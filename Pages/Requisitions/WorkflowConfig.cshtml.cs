using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

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

        public bool IsLocked { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        // ===========================
        // GET
        // ===========================
        public async Task<IActionResult> OnGetAsync(int? id)
        {
            await LoadLookupsAsync();

            if (id.HasValue)
            {
                Workflow = await _context.tbl_RequisitionWorkflowDefinitions
                    .Include(w => w.Steps.OrderBy(s => s.StepOrder))
                    .FirstOrDefaultAsync(w => w.Id == id.Value)
                    ?? new tbl_RequisitionWorkflowDefinition();

                Steps = Workflow.Steps.OrderBy(s => s.StepOrder).ToList();

                IsLocked = await _context.tbl_Requisitions
                    .AnyAsync(r => r.WorkflowDefinitionId == Workflow.Id);
            }

            return Page();
        }

        // ===========================
        // SAVE
        // ===========================
        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadLookupsAsync();

            Steps = Steps ?? new List<tbl_RequisitionWorkflowStep>();

            // Remove empty rows
            Steps = Steps
                .Where(s => !string.IsNullOrEmpty(s.AssignedToUserId))
                .ToList();

            if (!Steps.Any())
            {
                ModelState.AddModelError("", "At least one workflow step is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            bool isNew = Workflow.Id == 0;

            if (!isNew)
            {
                var isInUse = await _context.tbl_Requisitions
                    .AnyAsync(r => r.WorkflowDefinitionId == Workflow.Id);

                if (isInUse)
                {
                    StatusMessage = "This workflow is already in use and cannot be modified.";
                    IsLocked = true;
                    return Page();
                }
            }

            if (isNew)
            {
                _context.tbl_RequisitionWorkflowDefinitions.Add(Workflow);
                await _context.SaveChangesAsync();
            }
            else
            {
                var dbWorkflow = await _context.tbl_RequisitionWorkflowDefinitions
                    .Include(w => w.Steps)
                    .FirstOrDefaultAsync(w => w.Id == Workflow.Id);

                if (dbWorkflow == null)
                {
                    StatusMessage = "Workflow not found.";
                    return RedirectToPage("./Workflows");
                }

                dbWorkflow.Name = Workflow.Name;
                dbWorkflow.RequesterRole = Workflow.RequesterRole;
                dbWorkflow.IsActive = Workflow.IsActive;

                _context.tbl_RequisitionWorkflowSteps.RemoveRange(dbWorkflow.Steps);
                await _context.SaveChangesAsync();
            }

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

        // ===========================
        private async Task LoadLookupsAsync()
        {
            RoleOptions = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                })
                .ToListAsync();

            UserOptions = await _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName
                })
                .ToListAsync();
        }
    }
}