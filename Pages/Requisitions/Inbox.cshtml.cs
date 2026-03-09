using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Services;
using ZororoMemorialPark.Data.Models;
using System.Text;
using ZororoMemorialPark.Services.WhatsApp;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class InboxModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRequisitionWorkflowService _workflowService;
        private readonly NotificationService _notify;
        private readonly WhatsAppNotificationService _whatsApp;

        public InboxModel(WhatsAppNotificationService whatsApp, NotificationService notify, ApplicationDbContext context, UserManager<IdentityUser> userManager, IRequisitionWorkflowService workflowService)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
            _notify = notify;
            _whatsApp = whatsApp;
        }

        public List<tbl_Requisition> AssignedToMe { get; set; } = new();
        public List<tbl_Requisition> MySubmitted { get; set; } = new();

        public tbl_Requisition? SelectedRequisition { get; set; }

        [BindProperty]
        public List<LineEditModel> LineEdits { get; set; } = new();

        [BindProperty]
        public string? OverallComment { get; set; }

        [BindProperty]
        public bool Approve { get; set; }

        public bool CanAction { get; set; } = false;

        [TempData]
        public string? StatusMessage { get; set; }

        public class LineEditModel
        {
            public int LineId { get; set; }
            public string Description { get; set; } = "";
            public decimal CurrentQuantity { get; set; }
            public decimal CurrentUnitCost { get; set; }
            public string? LineComment { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var userId = user.Id;

            System.Diagnostics.Debug.WriteLine("INBOX LOADED WITH ID = " + id);

            // Load assigned requisitions
            AssignedToMe = await _context.tbl_Requisitions
                .Include(r => r.CurrentStep)
                .Include(r => r.Lines)
                .Where(r => r.Status == RequisitionStatus.InProgress &&
                            r.CurrentStep != null &&
                            r.CurrentStep.AssignedToUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Load user submitted requisitions
            MySubmitted = await _context.tbl_Requisitions
                .Include(r => r.CurrentStep)
                .Include(r => r.Lines)
                .Where(r => r.RequesterUserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Load selected requisition
            if (id.HasValue)
            {
                SelectedRequisition = await _context.tbl_Requisitions
                    .Include(r => r.Lines)
                    .Include(r => r.CurrentStep)
                    .FirstOrDefaultAsync(r => r.Id == id.Value);

                System.Diagnostics.Debug.WriteLine("SelectedReq Loaded = " + (SelectedRequisition != null));
            }

            if (SelectedRequisition != null)
            {
                // Determine if user can take action
                CanAction = SelectedRequisition.CurrentStep != null &&
                            SelectedRequisition.CurrentStep.AssignedToUserId == userId;

                // Populate line edits (read-only or editable)
                LineEdits = SelectedRequisition.Lines
                    .Select(l => new LineEditModel
                    {
                        LineId = l.Id,
                        Description = l.Description,
                        CurrentQuantity = l.CurrentQuantity,
                        CurrentUnitCost = l.CurrentUnitCost,
                        LineComment = l.LatestComment
                    })
                    .ToList();
            }

            return Page();
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
                    "memorial_request_notificaton",
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
        public async Task<IActionResult> OnPostActionAsync(int requisitionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userId = user.Id;

            var requisition = await _context.tbl_Requisitions
                .Include(r => r.Lines)
                .Include(r => r.CurrentStep)
                .ThenInclude(s => s!.WorkflowDefinition)
                .FirstOrDefaultAsync(r => r.Id == requisitionId);

            if (requisition == null)
            {
                StatusMessage = "Error: Requisition not found.";
                return RedirectToPage();
            }

            // Security check – user must be assigned to this step
            if (requisition.CurrentStep == null || requisition.CurrentStep.AssignedToUserId != userId)
            {
                StatusMessage = "You are not allowed to action this requisition.";
                return RedirectToPage();
            }

            // Process each line item modification
            foreach (var lineEdit in LineEdits)
            {
                var line = requisition.Lines.FirstOrDefault(l => l.Id == lineEdit.LineId);
                if (line == null) continue;

                // Track changes
                if (line.CurrentQuantity != lineEdit.CurrentQuantity ||
                    line.CurrentUnitCost != lineEdit.CurrentUnitCost)
                {
                    var change = new tbl_RequisitionLineChangeLog
                    {
                        RequisitionLineId = line.Id,
                        WorkflowStepId = requisition.CurrentStep.Id,
                        OldQuantity = line.CurrentQuantity,
                        NewQuantity = lineEdit.CurrentQuantity,
                        OldUnitCost = line.CurrentUnitCost,
                        NewUnitCost = lineEdit.CurrentUnitCost,
                        ChangedByUserId = userId
                    };
                    _context.tbl_RequisitionLineChangeLogs.Add(change);

                    line.CurrentQuantity = lineEdit.CurrentQuantity;
                    line.CurrentUnitCost = lineEdit.CurrentUnitCost;
                }

                // Line comment
                if (!string.IsNullOrWhiteSpace(lineEdit.LineComment))
                {
                    var comment = new tbl_RequisitionLineComment
                    {
                        RequisitionLineId = line.Id,
                        WorkflowStepId = requisition.CurrentStep.Id,
                        CommentText = lineEdit.LineComment,
                        CreatedByUserId = userId
                    };
                    _context.tbl_RequisitionLineComments.Add(comment);

                    line.LatestComment = lineEdit.LineComment;
                }
            }

            // Add history entry
            string action = Approve ? "Approved" : "Rejected";

            requisition.History.Add(new tbl_RequisitionHistory
            {
                Action = action,
                WorkflowStepId = requisition.CurrentStep.Id,
                Notes = OverallComment,
                PerformedByUserId = userId,
                PerformedAt = DateTime.UtcNow
            });

            bool finalDecision = false;
            IdentityUser? nextUser = null;

            // REJECT CASE
            if (!Approve)
            {
                requisition.Status = RequisitionStatus.Rejected;
                requisition.FinalizedAt = DateTime.UtcNow;
                requisition.FinalizedByUserId = userId;
                requisition.CurrentStepId = null;
                finalDecision = true;
            }
            else
            {
                // APPROVE CASE – move to next step or finalize
                var nextStep = await _workflowService.GetNextStepAsync(requisition.CurrentStep);

                if (nextStep == null)
                {
                    requisition.Status = RequisitionStatus.Approved;
                    requisition.FinalizedAt = DateTime.UtcNow;
                    requisition.FinalizedByUserId = userId;
                    requisition.CurrentStepId = null;
                    finalDecision = true;
                }
                else
                {
                    requisition.CurrentStepId = nextStep.Id;
                    nextUser = await _userManager.FindByIdAsync(nextStep.AssignedToUserId);
                }
            }

            await _context.SaveChangesAsync();

            // Notify requester if final
            if (finalDecision)
            {
                var requester = await _userManager.FindByIdAsync(requisition.RequesterUserId);
                if (requester != null)
                {
                    string emailTo = requester.Email;
                    string emailSubject = $"Requisition {requisition.RequestNumber} - Final Status: {requisition.Status}";

                    string baseUrl = $"{Request.Scheme}://{Request.Host}";
                    string detailsUrl = $"{baseUrl}/Requisitions/Details/{requisition.Id}";

                    string emailBody = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; background-color:#f4f6f8; padding:20px;'>

<table width='100%' cellpadding='0' cellspacing='0' style='max-width:600px; margin:auto; background-color:white; border-radius:8px; overflow:hidden;'>

<tr>
<td style='background-color:#006A46; padding:15px; text-align:center; color:white; font-size:18px; font-weight:bold;'>
    Requisition Update
</td>
</tr>

<tr>
<td style='padding:25px; font-size:14px; color:#333;'>

<p>Dear <strong>{requester.UserName}</strong>,</p>

<p>
Your requisition (<strong>{requisition.RequestNumber}</strong>) has been 
<strong>{requisition.Status}</strong>.
</p>

<p style='margin-top:25px; text-align:center;'>
<a href='{detailsUrl}'
   style='background-color:#006A46;
          color:white;
          padding:12px 22px;
          text-decoration:none;
          border-radius:5px;
          font-weight:bold;
          display:inline-block;'>
    View Requisition
</a>
</p>

<p style='margin-top:30px;'>
Regards,<br/>
<strong>Zororo Memorial Park Admin</strong>
</p>

</td>
</tr>

<tr>
<td style='background-color:#f4f6f8; padding:10px; text-align:center; font-size:11px; color:#888;'>
© {DateTime.UtcNow.Year} Zororo Memorial Park
</td>
</tr>

</table>

</body>
</html>";

                    await _notify.SendEmailAsync(emailTo, emailSubject, emailBody); 

                }
            }

            // Notify next approver
            if (!finalDecision && nextUser != null)
            {
                await NotifyApproverAsync(requisition, nextUser);
            }

            StatusMessage = $"Requisition {requisition.RequestNumber} {action} successfully.";
            return RedirectToPage(new { id = requisition.Id });
        }

    }
}
