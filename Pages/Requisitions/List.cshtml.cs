using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ZororoMemorialPark.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ZororoMemorialPark.Pages.Requisitions
{
    public class ListModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly UserHelper _theUser;

        public ListModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, UserHelper theUser)
        {
            _context = context;
            _userManager = userManager;
            _theUser = theUser;
        }

        // FILTERS
        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; } = "All";

        public List<RequisitionListItem> Requisitions { get; set; } = new();

        public List<SelectListItem> StatusOptions { get; set; } =
    new List<SelectListItem>
    {
        new SelectListItem { Value = "All",        Text = "All Statuses" },
        new SelectListItem { Value = "InProgress", Text = "In Progress" },
        new SelectListItem { Value = "Approved",   Text = "Approved" },
        new SelectListItem { Value = "Rejected",   Text = "Rejected" }
    };


        // TABLE PROJECTION
        public class RequisitionListItem
        {
            public int Id { get; set; }
            public string RequestNumber { get; set; } = "";
            public string Requester { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string WorkflowName { get; set; } = "";
            public string CurrentStepName { get; set; } = "";
            public string Status { get; set; } = "";
            public decimal TotalAmount { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            // Access control: Admin + Managers only
            if (!(User.IsInRole("Admin") || User.IsInRole("Manager")))
            {
                return Forbid();
            }

            var qry = _context.tbl_Requisitions
                .Include(r => r.WorkflowDefinition)
                .Include(r => r.CurrentStep)
                .Include(r => r.Lines)
                .AsQueryable();

            // APPLY SEARCH
            if (!string.IsNullOrWhiteSpace(Search))
            {
                qry = qry.Where(r =>
                       r.RequestNumber.Contains(Search)
                    || r.RequesterUserId.Contains(Search)
                    || r.Lines.Any(l => l.Description.Contains(Search))
                );
            }

            // APPLY STATUS FILTER
            if (!string.IsNullOrWhiteSpace(Status) && Status != "All")
            {
                qry = qry.Where(r => r.Status.ToString() == Status);
            }

                    var requisitions = await qry
                                .OrderByDescending(r => r.CreatedAt)
                                .ToListAsync();
            var result = new List<RequisitionListItem>();

            foreach (var r in requisitions)
            {
                result.Add(new RequisitionListItem
                {
                    Id = r.Id,
                    RequestNumber = r.RequestNumber,
                    Requester = await _theUser.GetFullNameByIdAsync(r.RequesterUserId), // NOW VALID
                    CreatedAt = r.CreatedAt,
                    WorkflowName = r.WorkflowDefinition?.Name,
                    CurrentStepName = r.CurrentStep != null ? r.CurrentStep.StepName : "Completed",
                    Status = r.Status.ToString(),
                    TotalAmount = r.Lines.Sum(x => x.CurrentQuantity * x.CurrentUnitCost)
                });
            }
           

            Requisitions = result;
            return Page();
        }
    }
}
