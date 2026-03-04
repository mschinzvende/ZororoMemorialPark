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

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalPages { get; set; }

        private const int PageSize = 10;

        [TempData]
        public string? StatusMessage { get; set; }

        public class WorkflowListItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string RequesterRole { get; set; } = "";
            public int StepCount { get; set; }
            public bool IsActive { get; set; }
            public bool IsInUse { get; set; }
        }

        public async Task OnGetAsync()
        {
            var query = _context.tbl_RequisitionWorkflowDefinitions
                .Include(w => w.Steps)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                query = query.Where(w =>
                    w.Name.Contains(Search) ||
                    w.RequesterRole.Contains(Search));
            }

            var totalCount = await query.CountAsync();

            TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            Workflows = await query
                .OrderBy(w => w.Name)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(w => new WorkflowListItem
                {
                    Id = w.Id,
                    Name = w.Name,
                    RequesterRole = w.RequesterRole,
                    StepCount = w.Steps.Count(),
                    IsActive = w.IsActive,
                    IsInUse = _context.tbl_Requisitions
                        .Any(r => r.WorkflowDefinitionId == w.Id)
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int WorkflowId)
        {
            var workflow = await _context.tbl_RequisitionWorkflowDefinitions
                .Include(w => w.Steps)
                .FirstOrDefaultAsync(w => w.Id == WorkflowId);

            if (workflow == null)
            {
                StatusMessage = "Workflow does not exist.";
                return RedirectToPage();
            }

            var isInUse = await _context.tbl_Requisitions
                .AnyAsync(r => r.WorkflowDefinitionId == WorkflowId);

            if (isInUse)
            {
                StatusMessage = "Cannot delete workflow because it is linked to existing requisitions.";
                return RedirectToPage();
            }

            _context.tbl_RequisitionWorkflowSteps.RemoveRange(workflow.Steps);
            _context.tbl_RequisitionWorkflowDefinitions.Remove(workflow);

            await _context.SaveChangesAsync();

            StatusMessage = $"Workflow '{workflow.Name}' deleted successfully.";
            return RedirectToPage();
        }
    }
}