using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Jobs
{
    public class JobsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        private readonly IAppLogger _appLogger;
        private const int PageSize = 10;

        public JobsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAppLogger appLogger)
        {
            _context = context;
            _appLogger = appLogger;
            _userManager = userManager;
        }

        // --- Pagination primitives (safe to use in Razor) ---
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public bool HasPreviousPage { get; set; } = false;
        public bool HasNextPage { get; set; } = false;
        public int TotalItems { get; set; } = 0;

        // Dropdown lists (always initialized)
        public List<tbl_Graves> Graves { get; set; } = new();
        public List<tbl_Suppliers> Suppliers { get; set; } = new();
        public List<tbl_DevelopmentStages> Stages { get; set; } = new();
        public List<tbl_PaymentMethods> PaymentMethods { get; set; } = new();
        public List<tbl_jobEvaluationParameters> JobParameters { get; set; } = new();

        public List<tbl_Sections> Sections { get; set; } = new();

        // The page of jobs (used only if needed)
        public List<tbl_Jobs> PageJobs { get; set; } = new();

        // Job list view model with balances (safe to enumerate in Razor)
        public List<JobViewModel> JobList { get; set; } = new();

        // Bind properties for forms
        //[BindProperty] public tbl_Jobs Job { get; set; }
        [BindProperty] public tbl_Expenses Expense { get; set; } = new();
        [BindProperty] public tbl_JobEvaluations JobEvaluation { get; set; } = new();


        // GET
        public async Task<IActionResult> OnGetAsync(int? pageIndex)
        {
            CurrentPage = Math.Max(1, pageIndex ?? 1);

            await LoadDropdownsAsync();

            // Build base query (no pagination yet)
            var baseQuery = _context.tbl_Jobs
                .Include(j => j.tbl_Grave)
                .Include(j => j.tbl_Supplier)
                .Include(j => j.tbl_DevelopmentStage)
                .OrderByDescending(j => j.sys_jobId)
                .AsNoTracking();

            // Count and paging calculations
            TotalItems = await baseQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
            if (TotalPages < 1) TotalPages = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;

            HasPreviousPage = CurrentPage > 1;
            HasNextPage = CurrentPage < TotalPages;

            // fetch only the page of jobs
            PageJobs = await baseQuery
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var graveIds = PageJobs
                .Where(j => j.sys_graveId > 0)
                .Select(j => j.sys_graveId)
                .Distinct()
                .ToList();

            var graveProgressLookup = new Dictionary<int, string>();

            if (graveIds.Any())
            {
                var graveJobStats = await _context.tbl_Jobs
                    .Where(j => graveIds.Contains(j.sys_graveId))
                    .GroupBy(j => j.sys_graveId)
                    .Select(g => new
                    {
                        GraveId = g.Key,
                        TotalJobs = g.Count(),
                        CompletedJobs = g.Count(x => x.sys_jobStatus == "Complete")
                    })
                    .ToListAsync();

                foreach (var g in graveJobStats)
                {
                    if (g.TotalJobs > 0 && g.CompletedJobs == g.TotalJobs)
                        graveProgressLookup[g.GraveId] = "Fully Developed";
                    else if (g.CompletedJobs >= 0)
                        graveProgressLookup[g.GraveId] = "In Progress";

                }



                // Efficiently compute balances for the jobs on this page in a single DB query
                var jobIds = PageJobs.Select(j => j.sys_jobId).ToList();
                var balancesDict = new Dictionary<int, double>();
                if (jobIds.Count > 0)
                {
                    balancesDict = await _context.tbl_Expenses
                        .Where(e => jobIds.Contains(e.sys_jobId))
                        .GroupBy(e => e.sys_jobId)
                        .Select(g => new
                        {
                            JobId = g.Key,
                            Balance = g.Sum(e => (double?)e.sys_amountDue - (double?)e.sys_amountPaid) ?? 0.0
                        })
                        .ToDictionaryAsync(x => x.JobId, x => x.Balance);
                }

                // build the view model list for the current page
                JobList = PageJobs.Select(j =>
                {
                    graveProgressLookup.TryGetValue(j.sys_graveId, out string graveStatus);

                    return new JobViewModel
                    {
                        JobId = j.sys_jobId,
                        GraveID = j.sys_graveId,
                        SupplierID = j.sys_supplierId,
                        StageID = j.sys_stageId,
                        SectionID = j.tbl_Grave?.sys_sectionId ?? 0,

                        GravePlot = j.tbl_Grave?.sys_plotNumber ?? string.Empty,
                        SupplierName = j.tbl_Supplier?.sys_supplierName ?? string.Empty,
                        StageName = j.tbl_DevelopmentStage?.sys_stageName ?? string.Empty,

                        StartDate = j.sys_startDate,
                        EndDate = j.sys_endDate,
                        JobStatus = j.sys_jobStatus ?? string.Empty,

                        Balance = _context.tbl_Expenses
                            .Where(e => e.sys_jobId == j.sys_jobId)
                            .OrderByDescending(e => e.sys_expenseId)
                            .Select(e => e.sys_balanceDue)
                            .FirstOrDefault(),

                        GraveProgressStatus = graveStatus ?? "Not Started"
                    };
                }).ToList();


                return Page();
            }

            return Page();
        }
        

        // Helper to load dropdown lists (always returns non-null lists)
        private async Task LoadDropdownsAsync()
        {
            Graves = await _context.tbl_Graves.AsNoTracking().ToListAsync() ?? new();
            Suppliers = await _context.tbl_Suppliers.AsNoTracking().ToListAsync() ?? new();
            Stages = await _context.tbl_DevelopmentStages.AsNoTracking().ToListAsync() ?? new();
            PaymentMethods = await _context.tbl_PaymentMethods.AsNoTracking().ToListAsync() ?? new();
            JobParameters = await _context.tbl_jobEvaluationParameters.AsNoTracking().ToListAsync() ?? new();
            Sections = await _context.tbl_Sections.AsNoTracking().ToListAsync() ?? new();
        }


        // AJAX endpoint
        public JsonResult OnGetGetGravesBySection(int sectionId)
        {
            var graves = _context.tbl_Graves
                .Where(g => g.sys_sectionId == sectionId)
                .Select(g => new { g.sys_graveId, g.sys_plotNumber })
                .ToList();

            return new JsonResult(graves);
        }

        // --- Create/Edit/Delete/Payment/Evaluation handlers ---
        // Note: On failure we reload page state so the view can render safely.
        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                
                var submittedJob = new tbl_Jobs
                {
                    sys_graveId = Request.Form["sys_graveId"].ToString() != "" ? int.Parse(Request.Form["sys_graveId"]) : 0,
                    sys_supplierId = Request.Form["sys_supplierId"].ToString() != "" ? int.Parse(Request.Form["sys_supplierId"]) : 0,
                    sys_stageId = Request.Form["sys_stageId"].ToString() != "" ? int.Parse(Request.Form["sys_stageId"]) : 0,
                    sys_startDate = DateOnly.ParseExact(Request.Form["sys_startDate"],"yyyy-MM-dd",CultureInfo.InvariantCulture),
                    sys_endDate = DateOnly.ParseExact(Request.Form["sys_endDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    sys_jobStatus = Request.Form["sys_status"].ToString()

                };

                var existingJob = await _context.tbl_Jobs
                .FirstOrDefaultAsync(j => j.sys_graveId == submittedJob.sys_graveId && j.sys_stageId == submittedJob.sys_stageId && j.sys_supplierId==submittedJob.sys_supplierId);

                if (existingJob != null)
                {
                    TempData["ErrorMessage"] = "A job with the same Grave, Stage, and Supplier already exists.";
                    await LoadDropdownsAsync();
                    return RedirectToPage();
                }

                // Basic validation
                if (submittedJob.sys_graveId == 0 || submittedJob.sys_supplierId == 0 || submittedJob.sys_stageId == 0)
                {
                    TempData["ErrorMessage"] = "Grave, Supplier, and Stage are required fields.";
                    await LoadDropdownsAsync();
                    return RedirectToPage();
                }

                // Validation rules
                if (submittedJob.sys_startDate > submittedJob.sys_endDate)
                {
                    TempData["ErrorMessage"] = "End Date must be greater than or equal to Start Date.";
                    return RedirectToPage();
                }

                //Validate Stage order
                // Get grave with stage info
                var grave = await _context.tbl_Graves
                    .FirstOrDefaultAsync(g => g.sys_graveId == submittedJob.sys_graveId);
                // Find stage order of current stage
                var currentStageOrder = await _context.tbl_DevelopmentStages
                    .Where(s => s.sys_stageId == grave.sys_stageId)
                    .Select(s => s.sys_stageOrder)
                    .FirstOrDefaultAsync();

                // Find stage order of the submitted job stage
                var newStageOrder = await _context.tbl_DevelopmentStages
                    .Where(s => s.sys_stageId == submittedJob.sys_stageId)
                    .Select(s => s.sys_stageOrder)
                    .FirstOrDefaultAsync();

                if (newStageOrder != currentStageOrder + 1 && newStageOrder != currentStageOrder)
                {
                    TempData["ErrorMessage"] = "Check grave stage. Cannot jump development stage sequence";
                    return RedirectToPage();
                }


                _context.tbl_Jobs.Add(submittedJob);
                await _context.SaveChangesAsync();

                //check if job status is "Completed" and if so, update the grave status to "Developed"
                if (submittedJob.sys_jobStatus == "Complete")
                {
                    var graveToUpdate = await _context.tbl_Graves.FindAsync(submittedJob.sys_graveId);
                    if (graveToUpdate != null)
                    {
                        graveToUpdate.sys_stageId = submittedJob.sys_stageId;
                        _context.tbl_Graves.Update(graveToUpdate);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Job created successfully.";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Create", $"Created Job ID {submittedJob.sys_jobId}");
                return RedirectToPage();
            }

            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                await LoadDropdownsAsync();
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in OnPostCreateAsync: {ex.Message}");
                return await OnGetAsync(CurrentPage);
            }

            
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                // Read values from form
                if (!int.TryParse(Request.Form["sys_jobId"], out int jobId) ||
                    !int.TryParse(Request.Form["sys_graveId"], out int graveId) ||
                    !int.TryParse(Request.Form["sys_supplierId"], out int supplierId) ||
                    !int.TryParse(Request.Form["sys_stageId"], out int stageId) ||
                    
                    !DateOnly.TryParse(Request.Form["sys_startDate"], out DateOnly startDate) ||
                    !DateOnly.TryParse(Request.Form["sys_endDate"], out DateOnly endDate))
                {
                    TempData["ErrorMessage"] = "Invalid form input. Please check your values.";
                    return RedirectToPage();
                }

                // Fetch job
                var job = await _context.tbl_Jobs.FirstOrDefaultAsync(j => j.sys_jobId == jobId);
                if (job == null)
                {
                    TempData["ErrorMessage"] = "Job not found.";
                    return RedirectToPage();
                }

                // Validation rules
                if (startDate > endDate)
                {
                    TempData["ErrorMessage"] = "End Date must be greater than or equal to Start Date.";
                    return RedirectToPage();
                }

                var grave = await _context.tbl_Graves.FirstOrDefaultAsync(g => g.sys_graveId == graveId);
                if (grave == null)
                {
                    TempData["ErrorMessage"] = "Selected grave does not exist.";
                    return RedirectToPage();
                }

                // Optional: ensure grave belongs to a section if your design requires
                var sectionId = grave.sys_sectionId;
                if (sectionId <= 0)
                {
                    TempData["ErrorMessage"] = "Selected grave is not assigned to a valid section.";
                    return RedirectToPage();
                }

                var supplierExists = await _context.tbl_Suppliers.AnyAsync(s => s.sys_supplierId == supplierId);
                if (!supplierExists)
                {
                    TempData["ErrorMessage"] = "Selected supplier does not exist.";
                    return RedirectToPage();
                }

                var stageExists = await _context.tbl_DevelopmentStages.AnyAsync(s => s.sys_stageId == stageId);
                if (!stageExists)
                {
                    TempData["ErrorMessage"] = "Selected stage does not exist.";
                    return RedirectToPage();
                }

                //Validate Stage order
                
                
                // Get the existing job from DB
                var existingJob = await _context.tbl_Jobs
                    .FirstOrDefaultAsync(j => j.sys_jobId == job.sys_jobId);

                // Get the order of the existing job’s stage
                var existingStageOrder = await _context.tbl_DevelopmentStages
                    .Where(s => s.sys_stageId == existingJob.sys_stageId)
                    .Select(s => s.sys_stageOrder)
                    .FirstOrDefaultAsync();

                // Update job
                job.sys_graveId = graveId;
                job.sys_supplierId = supplierId;
                job.sys_stageId = stageId;
                job.sys_startDate = DateOnly.ParseExact(Request.Form["sys_startDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                job.sys_endDate = DateOnly.ParseExact(Request.Form["sys_endDate"], "yyyy-MM-dd", CultureInfo.InvariantCulture);
                job.sys_jobStatus = Request.Form["sys_status"];


                // Get the order of the new stage being selected
                var editedStageOrder = await _context.tbl_DevelopmentStages
                    .Where(s => s.sys_stageId ==job.sys_stageId)
                    .Select(s => s.sys_stageOrder)
                    .FirstOrDefaultAsync();

                if (editedStageOrder < existingStageOrder)
                {
                    TempData["ErrorMessage"] = "Check grave stage. Cannot jump development stage sequence";
                    return RedirectToPage();
                }

                await _context.SaveChangesAsync();//check if job status is "Completed" and if so, update the grave status to "Developed"
                if (job.sys_jobStatus == "Complete")
                {
                    var nextStage = await _context.tbl_DevelopmentStages
                        .Where(s => s.sys_stageOrder == editedStageOrder + 1)
                        .FirstOrDefaultAsync();
                    var graveToUpdate = await _context.tbl_Graves.FindAsync(job.sys_graveId);
                    if (graveToUpdate != null)
                    {
                        if(nextStage != null)
                        {
                            graveToUpdate.sys_stageId = nextStage.sys_stageId;
                        }
                        else
                        {
                            graveToUpdate.sys_stageId = job.sys_stageId;
                        }
                           
                        _context.tbl_Graves.Update(graveToUpdate);
                        await _context.SaveChangesAsync();
                    }

                    //If status is complete, create a new job entry for that grave and asign next stage

                    
                    if (nextStage != null)
                    {
                        var newJob = new tbl_Jobs
                        {
                            sys_graveId = job.sys_graveId,
                            sys_supplierId = 11, //Default Supplier ID for system tasks
                            sys_stageId = nextStage.sys_stageId,
                            sys_startDate = DateOnly.ParseExact(DateTime.Now.ToString("yyyy-MM-dd"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                            sys_endDate = DateOnly.ParseExact(DateTime.Now.ToString("yyyy-MM-dd"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                            sys_jobStatus = "Pending"
                        };
                        _context.tbl_Jobs.Add(newJob);
                        await _context.SaveChangesAsync();
                    }

                    //check if next stage is null meaning the grave has reached last stage and set sale status to unsold

                    if (nextStage == null)
                    {
                        var graveToMarkUnsold = await _context.tbl_Graves.FindAsync(job.sys_graveId);
                        if (graveToMarkUnsold != null)
                        {
                            graveToMarkUnsold.sys_graveSaleStatus = "Unsold";
                            _context.tbl_Graves.Update(graveToMarkUnsold);
                            await _context.SaveChangesAsync();
                        }
                    }
                }


                TempData["SuccessMessage"] = "Job updated successfully!";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Edit", $"Edited Job ID {job.sys_jobId}");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating job: {ex.Message}";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in OnPostEditAsync: {ex.Message}");
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                int jobid = Int32.Parse(Request.Form["deleteJobId"]);
                var job = await _context.tbl_Jobs.FindAsync(jobid);
                if (job != null)
                {
                    _context.tbl_Jobs.Remove(job);
                    await _context.SaveChangesAsync();

                    // Rollback product usage
                    var usageList = await _context.tbl_JobEvaluationProductUsage
                        .Where(u => u.sys_jobId == jobid)
                        .ToListAsync();

                    foreach (var u in usageList)
                    {
                        var stockItem = await _context.tbl_ProductsAndServices
                            .FirstOrDefaultAsync(i => i.sys_itemId == u.sys_itemId);

                        if (stockItem != null)
                        {
                            double before = stockItem.sys_stockQuantity;
                            stockItem.sys_stockQuantity += u.sys_quantityUsed;
                            double after = stockItem.sys_stockQuantity;

                            _context.tbl_InventoryAuditTrail.Add(new tbl_InventoryAuditTrail
                            {
                                sys_itemId = u.sys_itemId,
                                sys_jobId = jobid,
                                sys_action = "ROLLBACK_DELETE_JOB",
                                sys_beforeQty = before,
                                sys_afterQty = after,
                                sys_changeQty = u.sys_quantityUsed,
                                sys_date = DateTime.Now
                            });

                            _context.tbl_ProductsAndServices.Update(stockItem);
                        }
                    }

                    _context.tbl_JobEvaluationProductUsage.RemoveRange(usageList);




                    TempData["SuccessMessage"] = "Job deleted successfully.";

                    _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Delete", $"Deleted Job ID {job.sys_jobId}");
                }
                else
                {
                    TempData["ErrorMessage"] = "Job not found.";
                }
                return RedirectToPage();
            }

            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting job: {ex.Message}";
                _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error in OnPostDeleteAsync: {ex.Message}");
                return RedirectToPage();
            }


        }

        public async Task<JsonResult> OnGetPaymentsAsync(int jobId)
        {
            var payments = await _context.tbl_Expenses
                .Where(p => p.sys_jobId == jobId)
                .OrderBy(p => p.sys_date)
                .Select(p => new PaymentDto
                {
                    PaymentDate = p.sys_date,
                    AmountPaid = p.sys_amountPaid,
                    AmountDue = p.sys_amountDue,
                    Balance = p.sys_balanceDue
                })
                .ToListAsync();

            return new JsonResult(payments);
        }

        public async Task<IActionResult> OnPostAddPaymentAsync(int sys_jobId, int sys_supplierId, 
            double AmountDue, double AmountPaid, DateOnly PaymentDate, int sys_paymentMethodId, 
            string PaymentNotes)
        {
            var previousPayments = await _context.tbl_Expenses
                .Where(p => p.sys_jobId == sys_jobId)
                .OrderBy(p => p.sys_date)
                .ToListAsync();

            double totalPaid = previousPayments.Sum(p => p.sys_amountPaid);
            double balance = 0;

            if (previousPayments.Count == 0)
            {
                // First payment: take user-entered AmountDue
                balance = AmountDue - AmountPaid;
            }
            else
            {
                // Subsequent payments: AmountDue always 0
                AmountDue = 0;
                var lastBalance = previousPayments.Last().sys_balanceDue;
                balance = lastBalance - AmountPaid;

                if (balance < 0)
                {
                    TempData["ErrorMessage"] = "Payment exceeds the remaining balance.";
                    await LoadDropdownsAsync();
                    return RedirectToPage();
                }
            }

            if(PaymentNotes == null)
            {
                PaymentNotes = "n/a";
            }

            var payment = new tbl_Expenses
            {
                sys_jobId = sys_jobId,
                sys_amountDue = AmountDue,
                sys_amountPaid = AmountPaid,
                sys_balanceDue = balance,
                sys_date = PaymentDate,
                sys_paymentMethodId = sys_paymentMethodId,
                sys_supplierId = sys_supplierId,
                sys_notes = PaymentNotes
            };

            _context.tbl_Expenses.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment saved successfully!";
            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetEvaluationParametersAsync(int jobId)
        {
            var parameters = await _context.tbl_jobEvaluationParameters.ToListAsync();

            var existing = await _context.tbl_JobEvaluations
                .Where(e => e.sys_jobId == jobId)
                .ToListAsync();

            var links = await _context.tbl_EvaluationParameterProducts
                .Include(l => l.tbl_ProductsAndServices)
                .ToListAsync();

            var usage = await _context.tbl_JobEvaluationProductUsage
                .Where(u => u.sys_jobId == jobId)
                .ToListAsync();

            var result = parameters.Select(p => new
            {
                p.sys_jobEvaluationParameterId,
                p.sys_parameterName,
                sys_jobEvaluation = existing
                    .FirstOrDefault(e => e.sys_jobEvaluationParameterId == p.sys_jobEvaluationParameterId)
                    ?.sys_jobEvaluation,

                linkedProducts = links
                    .Where(l => l.sys_jobEvaluationParameterId == p.sys_jobEvaluationParameterId)
                    .Select(lp => new
                    {
                        lp.tbl_ProductsAndServices.sys_itemId,
                        lp.tbl_ProductsAndServices.sys_itemName,
                        lp.sys_defaultQty,
                        lp.tbl_ProductsAndServices.sys_unitOfMeasure,
                        sys_quantityUsed = usage
                            .FirstOrDefault(u =>
                                u.sys_itemId == lp.tbl_ProductsAndServices.sys_itemId &&
                                u.sys_jobEvaluationParameterId == p.sys_jobEvaluationParameterId)
                            ?.sys_quantityUsed ?? 0
                    }).ToList()
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAddEvaluationAsync()
        {
           // try
           // {
                var jobIdStr = Request.Form["JobId"];
                if (string.IsNullOrEmpty(jobIdStr) || !int.TryParse(jobIdStr, out int jobId))
                {
                    TempData["ErrorMessage"] = "Invalid Job ID.";
                    return RedirectToPage();
                }

                // ===============================
                // 1) SAVE TEXT EVALUATIONS
                // ===============================

                foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("evaluation_")))
                {
                    var paramIdStr = key.Replace("evaluation_", "");
                    if (!int.TryParse(paramIdStr, out int paramId))
                        continue;

                    var evaluationText = Request.Form[key];
                    if (string.IsNullOrWhiteSpace(evaluationText))
                        continue;

                    var existingEval = await _context.tbl_JobEvaluations
                        .FirstOrDefaultAsync(e =>
                            e.sys_jobId == jobId &&
                            e.sys_jobEvaluationParameterId == paramId);

                    if (existingEval != null)
                    {
                        // DO NOT update (readonly once saved)
                        continue;
                    }
                    else
                    {
                        _context.tbl_JobEvaluations.Add(new tbl_JobEvaluations
                        {
                            sys_jobId = jobId,
                            sys_jobEvaluationParameterId = paramId,
                            sys_jobEvaluation = evaluationText
                        });
                    }
                }

                // ===============================
                // 2) SAVE PRODUCT USAGE (ONCE ONLY)
                // ===============================

                foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("product_")))
                {
                    var parts = key.Split('_');
                    if (parts.Length != 3) continue;

                    int.TryParse(parts[1], out int paramId);
                    int.TryParse(parts[2], out int productId);

                    double.TryParse(Request.Form[key], out double qtyUsed);
                    if (qtyUsed <= 0) continue;

                    // Check if usage exists
                    var existingUsage = await _context.tbl_JobEvaluationProductUsage
                        .FirstOrDefaultAsync(u =>
                            u.sys_jobId == jobId &&
                            u.sys_jobEvaluationParameterId == paramId &&
                            u.sys_itemId == productId);

                    if (existingUsage != null)
                    {
                        // Do NOT deduct stock again
                        continue;
                    }

                    // FIRST TIME PRODUCT USAGE ? Save & deduct stock
                    var usage = new tbl_JobEvaluationProductUsage
                    {
                        sys_jobId = jobId,
                        sys_jobEvaluationParameterId = paramId,
                        sys_itemId = productId,
                        sys_quantityUsed = qtyUsed,
                        sys_dateUsed = DateTime.Now
                    };

                    _context.tbl_JobEvaluationProductUsage.Add(usage);

                    // ===============================
                    // 3) DEDUCT STOCK (ONCE ONLY)
                    // ===============================

                    var stockItem = await _context.tbl_ProductsAndServices
                        .FirstOrDefaultAsync(i => i.sys_itemId == productId);

                    if (stockItem != null)
                    {
                        double before = stockItem.sys_stockQuantity;
                        stockItem.sys_stockQuantity -= qtyUsed;
                        double after = stockItem.sys_stockQuantity;

                        _context.tbl_ProductsAndServices.Update(stockItem);

                        // ===============================
                        // 4) STOCK AUDIT LOG
                        // ===============================

                        _context.tbl_InventoryAuditTrail.Add(new tbl_InventoryAuditTrail
                        {
                            sys_itemId = productId,
                            sys_jobId = jobId,
                            sys_action = "USED_IN_EVALUATION",
                            sys_beforeQty = before,
                            sys_afterQty = after,
                            sys_changeQty = -qtyUsed,
                            sys_date = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Evaluations & product usage saved successfully.";
           // }
           // catch (Exception ex)
           // {
            //    TempData["ErrorMessage"] = $"Error saving evaluations: {ex.Message}";
            //}

            return RedirectToPage();
        }



        public async Task<IActionResult> OnPostBulkDeleteAsync(string jobIds)
        {
            if (!string.IsNullOrEmpty(jobIds))
            {
                var ids = jobIds.Split(',').Select(int.Parse).ToList();
                var jobsToDelete = await _context.tbl_Jobs.Where(j => ids.Contains(j.sys_jobId)).ToListAsync();

                if (jobsToDelete.Any())
                {
                    _context.tbl_Jobs.RemoveRange(jobsToDelete);
                    await _context.SaveChangesAsync();

                    foreach(var job in jobsToDelete)
                    {
                        // Rollback product usage
                        var usageList = await _context.tbl_JobEvaluationProductUsage
                            .Where(u => u.sys_jobId == job.sys_jobId)
                            .ToListAsync();

                        foreach (var u in usageList)
                        {
                            var stockItem = await _context.tbl_ProductsAndServices
                                .FirstOrDefaultAsync(i => i.sys_itemId == u.sys_itemId);

                            if (stockItem != null)
                            {
                                double before = stockItem.sys_stockQuantity;
                                stockItem.sys_stockQuantity += u.sys_quantityUsed;
                                double after = stockItem.sys_stockQuantity;

                                _context.tbl_InventoryAuditTrail.Add(new tbl_InventoryAuditTrail
                                {
                                    sys_itemId = u.sys_itemId,
                                    sys_jobId = job.sys_jobId,
                                    sys_action = "ROLLBACK_DELETE_JOB",
                                    sys_beforeQty = before,
                                    sys_afterQty = after,
                                    sys_changeQty = u.sys_quantityUsed,
                                    sys_date = DateTime.Now
                                });

                                _context.tbl_ProductsAndServices.Update(stockItem);
                            }
                        }

                        _context.tbl_JobEvaluationProductUsage.RemoveRange(usageList);

                    }

                    TempData["SuccessMessage"] = $"{jobsToDelete.Count} jobs deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No jobs found to delete.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "No jobs selected for deletion.";
            }

            return RedirectToPage();
        }

    }


    // DTO for returning payments in JSON
    public class PaymentDto
    {
        public DateOnly PaymentDate { get; set; }
        public double AmountPaid { get; set; }
        public double AmountDue { get; set; }
        public double Balance { get; set; }
    }
    // -- lightweight view model used by the razor page --
    public class JobViewModel
    {
        public int JobId { get; set; }
        public int GraveID { get; set; }
        public int SupplierID { get; set; }
        public int SectionID { get; set; }
        public int StageID { get; set; }

        public string GravePlot { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string JobStatus { get; set; } = string.Empty;

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        public double Balance { get; set; }

        // NEW — derived grave-level status
        public string GraveProgressStatus { get; set; } = "Not Started";
    }
}
