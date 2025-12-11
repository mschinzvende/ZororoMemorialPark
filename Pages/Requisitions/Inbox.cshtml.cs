using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class InboxModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRequisitionWorkflowService _workflowService;

        public InboxModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IRequisitionWorkflowService workflowService)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
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
                    string emailBody =
                        $"Dear {requester.UserName},\n\nYour requisition ({requisition.RequestNumber}) has been {requisition.Status}.\n\nRemarks: {OverallComment}\n";

                    // call email service here
                }
            }

            // Notify next approver
            if (!finalDecision && nextUser != null)
            {
                string emailTo = nextUser.Email;
                string emailSubject = $"Requisition {requisition.RequestNumber} awaiting your action";
                string emailBody =
                    $"Dear {nextUser.UserName},\n\nRequisition ({requisition.RequestNumber}) is now assigned to you.\n";

                // call email service here
            }

            StatusMessage = $"Requisition {requisition.RequestNumber} {action} successfully.";
            return RedirectToPage(new { id = requisition.Id });
        }

    }
}
