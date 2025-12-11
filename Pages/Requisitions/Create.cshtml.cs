using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IRequisitionWorkflowService _workflowService;

        public CreateModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IRequisitionWorkflowService workflowService)
        {
            _context = context;
            _userManager = userManager;
            _workflowService = workflowService;
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

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            Requisition.RequesterUserId = user.Id;
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
                StatusMessage= "No requisition workflow is configured for your role.";
                return Page();
            }

            var firstStep = await _workflowService.GetFirstStepAsync(workflow.Id);
            if (firstStep == null)
            {
                ModelState.AddModelError(string.Empty, "Workflow has no steps defined.");
                StatusMessage = "Workflow has no steps defined";
                return Page();
            }

            var requestNumber = await _workflowService.GenerateRequestNumberAsync();

            var requisition = new tbl_Requisition
            {
                RequestNumber = requestNumber,
                RequesterUserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                Status = RequisitionStatus.InProgress,
                WorkflowDefinitionId = workflow.Id,
                CurrentStepId = firstStep.Id
            };

            foreach (var line in Lines.Where(l => !string.IsNullOrWhiteSpace(l.Description)))
            {
                var rl = new tbl_RequisitionLine
                {
                    Description = line.Description.Trim(),
                    OriginalQuantity = line.Quantity,
                    OriginalUnitCost = line.UnitCost,
                    CurrentQuantity = line.Quantity,
                    CurrentUnitCost = line.UnitCost
                };
                requisition.Lines.Add(rl);
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

            // ==============================
            // Email variables for reviewer
            // ==============================
            var reviewer = await _userManager.FindByIdAsync(firstStep.AssignedToUserId);
            if (reviewer != null)
            {
                string emailTo = reviewer.Email;
                string emailSubject = $"New Requisition {requisition.RequestNumber} awaiting your action";
                string emailBody = $"Dear {reviewer.UserName},\n\n" +
                    $"A new requisition ({requisition.RequestNumber}) has been submitted and is awaiting your review.\n\n" +
                    "Please log into the system to review the details.\n\n" +
                    "Thank you.";

                // Call your email method here, e.g.
                // await _emailService.SendAsync(emailTo, emailSubject, emailBody);
            }

            StatusMessage = $"Requisition {requisition.RequestNumber} submitted successfully.";
            return RedirectToPage("Inbox");
        }
    }
}
