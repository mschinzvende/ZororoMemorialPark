// Pages/Requisitions/WorkflowConfig.cshtml.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class WorkflowConfigModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public WorkflowConfigModel(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty]
        public tbl_RequisitionWorkflowDefinition Workflow { get; set; } = new();

        [BindProperty]
        public List<tbl_RequisitionWorkflowStep> Steps { get; set; } = new();

        public List<SelectListItem> RoleOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await LoadLookupsAsync();

            if (id.HasValue)
            {
                Workflow = await _context.tbl_RequisitionWorkflowDefinitions
                    .Include(w => w.Steps.OrderBy(s => s.StepOrder))
                    .FirstOrDefaultAsync(w => w.Id == id.Value) ?? new tbl_RequisitionWorkflowDefinition();

                Steps = Workflow.Steps.OrderBy(s => s.StepOrder).ToList();
            }
            else
            {
                Workflow = new tbl_RequisitionWorkflowDefinition();
                Steps = new List<tbl_RequisitionWorkflowStep>();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadLookupsAsync();

            if (!ModelState.IsValid)
            {
                StatusMessage = "Error: Fix validation issues.";
                return Page();
            }

            bool isNew = (Workflow.Id == 0);

            if (isNew)
            {
                // NEW WORKFLOW
                _context.tbl_RequisitionWorkflowDefinitions.Add(Workflow);
                await _context.SaveChangesAsync(); // Save first to get Workflow.Id
            }
            else
            {
                // EXISTING WORKFLOW
                var dbWorkflow = await _context.tbl_RequisitionWorkflowDefinitions
                    .Include(w => w.Steps)
                    .FirstOrDefaultAsync(w => w.Id == Workflow.Id);

                if (dbWorkflow == null)
                {
                    StatusMessage = "Error: Workflow not found.";
                    return RedirectToPage();
                }

                // Update fields
                dbWorkflow.Name = Workflow.Name;
                dbWorkflow.RequesterRole = Workflow.RequesterRole;
                dbWorkflow.IsActive = Workflow.IsActive;

                // Delete old steps
                _context.tbl_RequisitionWorkflowSteps.RemoveRange(dbWorkflow.Steps);

                await _context.SaveChangesAsync();
            }

            // Now insert steps fresh
            int order = 1;
            foreach (var step in Steps)
            {
                step.Id = 0;
                step.WorkflowDefinitionId = Workflow.Id;
                step.StepOrder = order++;

                _context.tbl_RequisitionWorkflowSteps.Add(step);
            }

            await _context.SaveChangesAsync();

            StatusMessage = "Workflow saved successfully.";

            return RedirectToPage("./Workflows");
        }


        private async Task LoadLookupsAsync()
        {
            RoleOptions = await _roleManager.Roles
                .Select(r => new SelectListItem { Value = r.Name, Text = r.Name })
                .ToListAsync();

            UserOptions = await _userManager.Users
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                })
                .ToListAsync();
        }
    }
}
