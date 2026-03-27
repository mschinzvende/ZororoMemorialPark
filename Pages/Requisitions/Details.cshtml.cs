using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserHelper _theUser;

        public DetailsModel(ApplicationDbContext context, UserHelper theUser)
        {
            _context = context;
            _theUser = theUser;
        }

        public tbl_Requisition? Requisition { get; set; }

        public List<LineDisplay> Lines { get; set; } = new();
        public List<HistoryDisplay> History { get; set; } = new();
        public List<WorkflowStepDisplay> Steps { get; set; } = new();

        public string RequesterName { get; set; }
        public class LineDisplay
        {
            public tbl_RequisitionLine Line { get; set; } = new();
            public List<tbl_RequisitionLineChangeLog> Changes { get; set; } = new();
            public List<tbl_RequisitionLineComment> Comments { get; set; } = new();
        }

        public class HistoryDisplay
        {
            public string Action { get; set; } = "";
            public string PerformedBy { get; set; } = "";
            public string? Notes { get; set; }
            public DateTime Date { get; set; }
            public string StageName { get; set; } = "";
        }

        public class WorkflowStepDisplay
        {
            public int Order { get; set; }
            public string StepName { get; set; } = "";
            public string Type { get; set; } = "";
            public bool IsCurrent { get; set; }
            public bool IsCompleted { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Requisition = await _context.tbl_Requisitions
                .Include(r => r.WorkflowDefinition)
                .Include(r => r.CurrentStep)
                .Include(r => r.Lines)
                .Include(r => r.History)
                .FirstOrDefaultAsync(r => r.Id == id);

            

            if (Requisition == null)
                return NotFound();


            RequesterName = await _theUser.GetFullNameByIdAsync(Requisition.RequesterUserId);
            // FETCH ALL STEPS
            var steps = await _context.tbl_RequisitionWorkflowSteps
                .Where(s => s.WorkflowDefinitionId == Requisition.WorkflowDefinitionId)
                .OrderBy(s => s.StepOrder)
                .ToListAsync();

            int currentStepId = Requisition.CurrentStepId ?? 0;

            Steps = steps.Select(s => new WorkflowStepDisplay
            {
                Order = s.StepOrder,
                StepName = s.StepName ?? (s.StepType == WorkflowStepType.Review ? "Review" : "Approval"),
                Type = s.StepType.ToString(),
                IsCurrent = (s.Id == currentStepId),
                IsCompleted = (Requisition.History.Any(h => h.WorkflowStepId == s.Id))
            })
            .ToList();

            // LINES, COMMENTS, CHANGES
            foreach (var line in Requisition.Lines)
            {
                var item = new LineDisplay
                {
                    Line = line,
                    Comments = await _context.tbl_RequisitionLineComments
                        .Where(c => c.RequisitionLineId == line.Id)
                        .OrderByDescending(c => c.CreatedAt)
                        .ToListAsync(),
                    Changes = await _context.tbl_RequisitionLineChangeLogs
                        .Where(c => c.RequisitionLineId == line.Id)
                        .OrderByDescending(c => c.ChangedAt)
                        .ToListAsync()
                };

                Lines.Add(item);
            }

            // HISTORY (TIMELINE)
            History = Requisition.History
                .OrderBy(h => h.PerformedAt)
                .Select(h => new HistoryDisplay
                {
                    Action = h.Action,
                    Notes = h.Notes,
                    Date = h.PerformedAt,
                    StageName = steps.FirstOrDefault(s => s.Id == h.WorkflowStepId)?.StepName ?? "-",
                    PerformedBy = h.PerformedByUserId
                })
                .ToList();

            return Page();
        }
    }
}
