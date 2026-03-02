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
            WhatsAppNotificationService whatsApp
            )
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
                Justification = Request.Form["Justification"],
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

            var reviewer = await _userManager.FindByIdAsync(firstStep.AssignedToUserId);

            // =============================
            //  Send Whats App to Reviewer
            // =============================

            if (!string.IsNullOrWhiteSpace(reviewer.PhoneNumber))
            {
                var phone = reviewer.PhoneNumber;

               var appResponse =  await _whatsApp.SendRequisitionNotificationWithLink(
                    to: phone,
                    templateName: "requisition_review_notification",
                    reviewerName: reviewer.UserName,
                    requestNumber: requisition.RequestNumber,
                    requisitionId: requisition.Id.ToString()
                );
            }

            // ==============================
            // Email variables for reviewer
            // ==============================
            
            if (reviewer != null)
            {
                string baseUrl = $"{Request.Scheme}://{Request.Host}";
                string inboxUrl = $"{baseUrl}/Requisitions/Inbox";

                string emailBody =
                $@"
                            <p>Dear {reviewer.UserName},</p>

                            <p>
                                A new requisition (<strong>{requisition.RequestNumber}</strong>) has been submitted 
                                and is awaiting your review.
                            </p>

                            <p>Please click the button below to open your requisition inbox:</p>

                            <p>
                                <a href=""{inboxUrl}""
                                   style=""background-color:#006A46;
                                          color:white;
                                          padding:10px 18px;
                                          text-decoration:none;
                                          border-radius:5px;
                                          font-weight:bold;
                                          display:inline-block;"">
                                    Open Requisition Inbox
                                </a>
                            </p>

                            <p>
                                Regards,<br/>
                                Zororo Memorial Park Admin
                            </p>
                            ";


                // Call your email method here, e.g.
                await _notify.SendEmailAsync(
                    reviewer.Email,
                    subject: $"New Requisition {requisition.RequestNumber} Submitted",
                    body: emailBody);
            }

            StatusMessage = $"Requisition {requisition.RequestNumber} submitted successfully.";
            return RedirectToPage("Inbox");
        }
    }
}
