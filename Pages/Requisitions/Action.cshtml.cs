using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class ActionModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRequisitionWorkflowService _workflowService;
        private readonly NotificationService _notify;
        private readonly WhatsAppNotificationService _whatsApp;
        
        public ActionModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IRequisitionWorkflowService workflowService,
            NotificationService notify,
            WhatsAppNotificationService whatsApp)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
            _notify = notify;
            _whatsApp = whatsApp;
        }

        public tbl_Requisition? Requisition { get; set; }
        public string Decision { get; set; } = "";
        public Guid Token { get; set; }

        // =============================
        //  STEP 1: SHOW CONFIRMATION PAGE
        // =============================
        public async Task<IActionResult> OnGetAsync(string decision, Guid token)
        {
            var actionToken = await _context.tbl_RequisitionActionToken
                .FirstOrDefaultAsync(t => t.Token == token);

            if (actionToken == null || actionToken.IsUsed || actionToken.ExpiryDate < DateTime.UtcNow)
                return Content("Invalid or expired link.");

            var requisition = await _context.tbl_Requisitions
                .Include(r => r.Lines)
                .Include(r => r.CurrentStep)
                .FirstOrDefaultAsync(r => r.Id == actionToken.RequisitionId);

            if (requisition == null)
                return Content("Requisition not found.");

            if (requisition.CurrentStepId != actionToken.WorkflowStepId)
                return Content("This approval link is no longer valid.");

            Requisition = requisition;
            Decision = decision.ToLower();
            Token = token;

            return Page(); // shows confirmation UI
        }

        // =============================
        //  STEP 2: EXECUTE AFTER CONFIRM
        // =============================
        public async Task<IActionResult> OnPostConfirmAsync(string decision, Guid token)
        {
            var actionToken = await _context.tbl_RequisitionActionToken
                .FirstOrDefaultAsync(t => t.Token == token);

            if (actionToken == null || actionToken.IsUsed || actionToken.ExpiryDate < DateTime.UtcNow)
                return Content("Invalid or expired link.");

            var requisition = await _context.tbl_Requisitions
                .Include(r => r.Lines)
                .Include(r => r.CurrentStep)
                .ThenInclude(s => s!.WorkflowDefinition)
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == actionToken.RequisitionId);

            if (requisition == null)
                return Content("Requisition not found.");

            bool approve = decision == "approve";

            requisition.History.Add(new tbl_RequisitionHistory
            {
                Action = approve ? "Approved (WhatsApp)" : "Rejected (WhatsApp)",
                WorkflowStepId = requisition.CurrentStepId,
                Notes = "Confirmed via WhatsApp link.",
                PerformedByUserId = actionToken.UserId,
                PerformedAt = DateTime.UtcNow
            });

            bool finalDecision = false;
            IdentityUser? nextUser = null;

            if (!approve)
            {
                requisition.Status = RequisitionStatus.Rejected;
                requisition.CurrentStepId = null;
                requisition.FinalizedAt = DateTime.UtcNow;
                requisition.FinalizedByUserId = actionToken.UserId;
                finalDecision = true;
            }
            else
            {
                var nextStep = await _workflowService.GetNextStepAsync(requisition.CurrentStep);

                if (nextStep == null)
                {
                    requisition.Status = RequisitionStatus.Approved;
                    requisition.CurrentStepId = null;
                    requisition.FinalizedAt = DateTime.UtcNow;
                    requisition.FinalizedByUserId = actionToken.UserId;
                    finalDecision = true;
                }
                else
                {
                    requisition.CurrentStepId = nextStep.Id;
                    nextUser = await _userManager.FindByIdAsync(nextStep.AssignedToUserId);
                }
            }

            actionToken.IsUsed = true;
            await _context.SaveChangesAsync();

            if (!finalDecision && nextUser != null)
            {
                await NotifyApproverAsync(requisition, nextUser);
            }

            return Content($"Requisition {requisition.RequestNumber} processed successfully.");
        }

        // =============================
        //  NOTIFICATION METHOD (UNCHANGED)
        // =============================
        private async Task NotifyApproverAsync(tbl_Requisition requisition, IdentityUser approver)
        {
            var claims = await _userManager.GetClaimsAsync(approver);
            string name = claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? approver.UserName;

            var lines = requisition.Lines.ToList();
            var sb = new StringBuilder();

            foreach (var l in lines)
                sb.Append($"{l.Description} {l.CurrentQuantity}@${l.CurrentUnitCost} - ${l.CurrentQuantity * l.CurrentUnitCost} || ");

            string totalCost = lines.Sum(x => x.CurrentQuantity * x.CurrentUnitCost).ToString("C");

            var token = new tbl_RequisitionActionToken
            {
                Token = Guid.NewGuid(),
                RequisitionId = requisition.Id,
                WorkflowStepId = requisition.CurrentStepId.Value,
                UserId = approver.Id,
                ExpiryDate = DateTime.UtcNow.AddHours(12),
                IsUsed = false
            };

            _context.tbl_RequisitionActionToken.Add(token);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(approver.PhoneNumber))
            {
                await _whatsApp.SendRequisitionNotificationWithLink(
                    approver.PhoneNumber,
                    "req_notifications",
                    name,
                    requisition.RequestNumber,
                    sb.ToString(),
                    totalCost,
                    requisition.Id.ToString(),
                    token.Token.ToString());
            }

            string inboxUrl = $"{Request.Scheme}://{Request.Host}/Requisitions/Inbox";

            await _notify.SendEmailAsync(
                approver.Email,
                $"Requisition {requisition.RequestNumber} Awaiting Your Action",
                $"Requisition {requisition.RequestNumber} requires your review.<br/><a href='{inboxUrl}'>Open Inbox</a>");
        }
    }
}