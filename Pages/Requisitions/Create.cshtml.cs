using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Services;
using ZororoMemorialPark.Services.WhatsApp;
using ZororoMemorialPark.Data.Models;
using System.Text;

// KEEP YOUR USING STATEMENTS AS IS

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRequisitionWorkflowService _workflowService;
        private readonly NotificationService _notify;
        private readonly WhatsAppNotificationService _whatsApp;

        public CreateModel(
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

        [BindProperty]
        public tbl_Requisition Requisition { get; set; } = new();

        [BindProperty]
        public List<RequisitionLineInput> Lines { get; set; } = new() { new RequisitionLineInput() };

        [TempData]
        public string? StatusMessage { get; set; }

        public class RequisitionLineInput
        {
            public string Description { get; set; } = string.Empty;
            public decimal Quantity { get; set; }
            public decimal UnitCost { get; set; }
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (Lines == null || !Lines.Any(l => !string.IsNullOrWhiteSpace(l.Description)))
            {
                ModelState.AddModelError(string.Empty, "Please add at least one line item.");
            }

            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Please fix validation issues.";
                return Page();
            }

            var workflow = await _workflowService.GetWorkflowForRequesterAsync(user.Id);
            if (workflow == null)
            {
                StatusMessage = "No requisition workflow configured.";
                return Page();
            }

            var firstStep = await _workflowService.GetFirstStepAsync(workflow.Id);
            if (firstStep == null)
            {
                StatusMessage = "Workflow has no steps defined.";
                return Page();
            }

            var requestNumber = await _workflowService.GenerateRequestNumberAsync();

            var requisition = new tbl_Requisition
            {
                RequestNumber = requestNumber,
                Justification = Request.Form["Justification"],
                RequesterUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                Status = RequisitionStatus.InProgress,
                WorkflowDefinitionId = workflow.Id,
                CurrentStepId = firstStep.Id
            };

            foreach (var line in Lines.Where(l => !string.IsNullOrWhiteSpace(l.Description)))
            {
                requisition.Lines.Add(new tbl_RequisitionLine
                {
                    Description = line.Description.Trim(),
                    OriginalQuantity = line.Quantity,
                    OriginalUnitCost = line.UnitCost,
                    CurrentQuantity = line.Quantity,
                    CurrentUnitCost = line.UnitCost
                });
            }

            requisition.History.Add(new tbl_RequisitionHistory
            {
                Action = "Submitted",
                PerformedByUserId = user.Id,
                Notes = "Requisition submitted.",
                PerformedAt = DateTime.UtcNow
            });

            _context.tbl_Requisitions.Add(requisition);
            await _context.SaveChangesAsync();

            var reviewer = await _userManager.FindByIdAsync(firstStep.AssignedToUserId);

            if (reviewer != null)
                await NotifyApproverAsync(requisition, reviewer);

            StatusMessage = $"Requisition {requisition.RequestNumber} submitted successfully.";
            return RedirectToPage("Inbox");
        }

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

            string baseUrl = $"{Request.Scheme}://{Request.Host}";
            string inboxUrl = $"{baseUrl}/Requisitions/Inbox";

            string emailBody = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
</head>
<body style='font-family: Arial, sans-serif; background-color:#f4f6f8; padding:20px;'>

<table width='100%' cellpadding='0' cellspacing='0' style='max-width:600px; margin:auto; background-color:white; border-radius:8px; overflow:hidden;'>
    
    <tr>
        <td style='background-color:#006A46; padding:15px; text-align:center; color:white; font-size:18px; font-weight:bold;'>
            Zororo Memorial Park
        </td>
    </tr>

    <tr>
        <td style='padding:25px; color:#333; font-size:14px;'>

            <p>Dear <strong>{name}</strong>,</p>

            <p>
                A requisition (<strong>{requisition.RequestNumber}</strong>) has been 
                assigned to you and requires your review.
            </p>

            <p style='margin-top:25px; text-align:center;'>
                <a href='{inboxUrl}'
                   style='background-color:#006A46;
                          color:white;
                          padding:12px 22px;
                          text-decoration:none;
                          border-radius:5px;
                          font-weight:bold;
                          display:inline-block;'>
                    Open Requisition Inbox
                </a>
            </p>

            <p style='margin-top:30px; font-size:12px; color:#777;'>
                If the button above does not work, copy and paste the link below into your browser:
                <br/>
                {inboxUrl}
            </p>

            <p style='margin-top:30px;'>
                Regards,<br/>
                <strong>Zororo Memorial Park Admin</strong>
            </p>

        </td>
    </tr>

    <tr>
        <td style='background-color:#f4f6f8; padding:10px; text-align:center; font-size:11px; color:#888;'>
            © {DateTime.UtcNow.Year} Zororo Memorial Park. All rights reserved.
        </td>
    </tr>

</table>

</body>
</html>";






            await _notify.SendEmailAsync(
    approver.Email,
    $"Requisition {requisition.RequestNumber} Awaiting Your Action",
    emailBody
);
        }
    }
}