using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class WorkflowsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public WorkflowsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<WorkflowListItem> Workflows { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class WorkflowListItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string RequesterRole { get; set; } = "";
            public int StepCount { get; set; }
            public bool IsActive { get; set; }
        }

        public async Task OnGetAsync()
        {
            Workflows = await _context.tbl_RequisitionWorkflowDefinitions
                .Select(w => new WorkflowListItem
                {
                    Id = w.Id,
                    Name = w.Name,
                    RequesterRole = w.RequesterRole,
                    StepCount = w.Steps.Count(),
                    IsActive = w.IsActive
                })
                .OrderBy(w => w.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int WorkflowId)
        {
            var workflow = await _context.tbl_RequisitionWorkflowDefinitions
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == WorkflowId);

            if (workflow == null)
            {
                StatusMessage = "Error: Workflow does not exist.";
                return RedirectToPage();
            }

            // Delete steps first
            _context.tbl_RequisitionWorkflowSteps.RemoveRange(workflow.Steps);

            // Delete workflow
            _context.tbl_RequisitionWorkflowDefinitions.Remove(workflow);

            await _context.SaveChangesAsync();

            StatusMessage = $"Workflow '{workflow.Name}' deleted successfully.";
            return RedirectToPage();
        }
    }
}
