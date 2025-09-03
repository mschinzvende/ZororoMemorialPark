using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data; // Replace with your DbContext namespace
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Jobs
{
    public class JobParametersModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public JobParametersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Paginated list for server-side pagination
        public PaginatedList<tbl_jobEvaluationParameters> PaginatedParameters { get; set; }

       

        // Page index for pagination
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        // Page size
        private const int PageSize = 10;

        public async Task OnGetAsync()
        {
            IQueryable<tbl_jobEvaluationParameters> parametersIQ = _context.tbl_jobEvaluationParameters;

            

            parametersIQ = parametersIQ.OrderBy(p => p.sys_jobEvaluationParameterId);

            PaginatedParameters = await PaginatedList<tbl_jobEvaluationParameters>.CreateAsync(parametersIQ.AsNoTracking(), PageIndex, PageSize);
        }

        // Create
        [BindProperty]
        public tbl_jobEvaluationParameters NewParameter { get; set; }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"Validation failed: {errors}";
                return RedirectToPage();
            }

            _context.tbl_jobEvaluationParameters.Add(NewParameter);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Parameter added successfully.";
            return RedirectToPage();
        }

        // Edit
        [BindProperty]
        public tbl_jobEvaluationParameters EditParameter { get; set; }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input.";
                return RedirectToPage();
            }

            var parameter = await _context.tbl_jobEvaluationParameters.FindAsync(EditParameter.sys_jobEvaluationParameterId);
            if (parameter == null)
            {
                TempData["ErrorMessage"] = "Parameter not found.";
                return RedirectToPage();
            }

            parameter.sys_parameterName = EditParameter.sys_parameterName;

            _context.tbl_jobEvaluationParameters.Update(parameter);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parameter updated successfully.";
            return RedirectToPage();
        }

        // Delete single
        [BindProperty]
        public int sys_jobEvaluationParameterId { get; set; }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            var parameter = await _context.tbl_jobEvaluationParameters.FindAsync(sys_jobEvaluationParameterId);
            if (parameter == null)
            {
                TempData["ErrorMessage"] = "Parameter not found.";
                return RedirectToPage();
            }

            _context.tbl_jobEvaluationParameters.Remove(parameter);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Parameter deleted successfully.";
            return RedirectToPage();
        }

        

        public async Task<IActionResult> OnPostDeleteSelectedAsync([FromForm] string SelectedIds)
        {
            if (string.IsNullOrWhiteSpace(SelectedIds))
            {
                TempData["ErrorMessage"] = "No parameters selected.";
                return RedirectToPage();
            }

            var ids = SelectedIds.Split(',').Select(int.Parse).ToList();
            var parameters = await _context.tbl_jobEvaluationParameters
                                           .Where(p => ids.Contains(p.sys_jobEvaluationParameterId))
                                           .ToListAsync();

            if (parameters.Count == 0)
            {
                TempData["ErrorMessage"] = "Selected parameters not found.";
                return RedirectToPage();
            }

            _context.tbl_jobEvaluationParameters.RemoveRange(parameters);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{parameters.Count} parameter(s) deleted successfully.";
            return RedirectToPage();
        }
    }

    // ---------------- PaginatedList Helper ----------------
    
}
