using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;
using System.Drawing.Printing;

namespace ZororoMemorialPark.Pages.Jobs
{
    public class JobsModel : PageModel
    {
        public readonly ApplicationDbContext _context;

        public JobsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Pagination
        public PaginatedList<tbl_Jobs> PaginatedJobs { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        private const int pageSize = 10;

        // Dropdowns
        public List<tbl_Graves> Graves { get; set; }
        public List<tbl_Suppliers> Suppliers { get; set; }
        public List<tbl_DevelopmentStages> Stages { get; set; }
        public List<tbl_PaymentMethods> PaymentMethods { get; set; }
        public List<tbl_jobEvaluationParameters> JobParameters { get; set; }

        public List<tbl_Jobs> Jobs { get; set; }

        // Form binders
        [BindProperty] public tbl_Jobs Job { get; set; }
        [BindProperty] public tbl_Expenses Expense { get; set; }
        [BindProperty] public tbl_JobEvaluations JobEvaluation { get; set; }

        public async Task<IActionResult> OnGetAsync(int? pageIndex)
        {
            PageIndex = pageIndex ?? 1;

            Graves = await _context.tbl_Graves.ToListAsync();
            Suppliers = await _context.tbl_Suppliers.ToListAsync();
            Stages = await _context.tbl_DevelopmentStages.ToListAsync();
            PaymentMethods = await _context.tbl_PaymentMethods.ToListAsync();
            JobParameters = await _context.tbl_jobEvaluationParameters.ToListAsync();
            Jobs = await _context.tbl_Jobs.ToListAsync();

            var jobsQuery = _context.tbl_Jobs
                .Include(j => j.tbl_Grave)
                .Include(j => j.tbl_Supplier)
                .Include(j => j.tbl_DevelopmentStage)
                .OrderByDescending(j => j.sys_jobId);

            PaginatedJobs = await PaginatedList<tbl_Jobs>.CreateAsync(jobsQuery.AsNoTracking(), PageIndex, 10);

            // fallback if query has no results
            if (PaginatedJobs == null)
            {
                
                PaginatedJobs = new PaginatedList<tbl_Jobs>([], 0, 1, pageSize);
            }

            return Page();
        }

        // --- CRUD ---
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.tbl_Jobs.Add(Job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job created successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.tbl_Jobs.Update(Job);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Job updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int sys_jobId)
        {
            var job = await _context.tbl_Jobs.FindAsync(sys_jobId);
            if (job != null)
            {
                _context.tbl_Jobs.Remove(job);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Job deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Job not found.";
            }
            return RedirectToPage();
        }

        // --- Payments ---
        public async Task<IActionResult> OnPostAddPaymentAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.tbl_Expenses.Add(Expense);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment recorded successfully.";
            return RedirectToPage();
        }

        // --- Evaluations ---
        public async Task<IActionResult> OnPostAddEvaluationAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.tbl_JobEvaluations.Add(JobEvaluation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Evaluation saved successfully.";
            return RedirectToPage();
        }
    }
}
