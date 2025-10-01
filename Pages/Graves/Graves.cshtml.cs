using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZororoMemorialPark.Pages.Graves
{
    public class GravesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAppLogger _appLogger;

        [BindProperty]
        public string SelectedIds { get; set; }
        public GravesModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAppLogger appLogger)
        {
            _context = context;
            _userManager = userManager;
            _appLogger = appLogger;
        }

        public IList<tbl_Graves> Graves { get; set; } = new List<tbl_Graves>();
        //public PaginatedList<GraveViewModel> PaginatedGraves { get; set; } = new PaginatedList<GraveViewModel>(new List<GraveViewModel>(), 0, 1, 10);
        public List<GraveViewModel> AvailableGraves { get; set; } = new List<GraveViewModel>();
        public List<GraveViewModel> ReservedGraves { get; set; } = new List<GraveViewModel>();
        public List<GraveViewModel> OccupiedGraves { get; set; } = new List<GraveViewModel>();
        public IList<tbl_Sections> Sections { get; set; } = new List<tbl_Sections>();
        public IList<tbl_DevelopmentStages> Stages { get; set; } = new List<tbl_DevelopmentStages>();

        public async Task OnGetAsync()
        {
            try
            {
                // Load sections and stages for dropdowns
                Sections = await _context.tbl_Sections.AsNoTracking().ToListAsync();
                Stages = await _context.tbl_DevelopmentStages.AsNoTracking().ToListAsync();

                // Base query for graves with section and stage info
                var baseQuery = from g in _context.tbl_Graves
                                join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                                join st in _context.tbl_DevelopmentStages on g.sys_stageId equals st.sys_stageId
                                orderby g.sys_plotNumber
                                select new GraveViewModel
                                {
                                    sys_graveId = g.sys_graveId,
                                    sys_plotNumber = g.sys_plotNumber,
                                    sys_graveSaleStatus = g.sys_graveSaleStatus,
                                    sys_price = g.sys_price,
                                    sys_sectionId = g.sys_sectionId,
                                    SectionName = s.sys_sectionName,
                                    sys_stageId = g.sys_stageId,
                                    StageName = st.sys_stageName,
                                    sys_latitude = g.sys_latitude,
                                    sys_longitude = g.sys_longitude
                                };

                // Load all graves by status
                AvailableGraves = await baseQuery.Where(g => g.sys_graveSaleStatus == "Unsold").ToListAsync();
                ReservedGraves = await baseQuery.Where(g => g.sys_graveSaleStatus == "Reserved").ToListAsync();
                OccupiedGraves = await baseQuery.Where(g => g.sys_graveSaleStatus == "Sold" || g.sys_graveSaleStatus == "Occupied").ToListAsync();

                // Log access
                var userId = _userManager.GetUserId(User);
                await _appLogger.LogActivityAsync(userId, "Access", "Accessed Graves page with tabs (no pagination)");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading graves: " + ex.Message;
            }
        }



        // Single grave creation
        public async Task<IActionResult> OnPostCreateAsync()
        {
            try
            {
                
                int sectionId = int.Parse(Request.Form["sys_sectionId"]);
                int stageId = int.Parse(Request.Form["sys_stageId"]);
                string status = Request.Form["sys_graveSaleStatus"];
                double price = double.Parse(Request.Form["sys_price"]);
                double latitude = double.Parse(Request.Form["sys_Latitude"]);
                double longitude = double.Parse(Request.Form["sys_Longitude"]);

                string plot = Request.Form["sys_plotNumber"];

                var sectionName = _context.tbl_Sections
                    .Where(s => s.sys_sectionId == sectionId )
                    .Select(s => s.sys_sectionName)
                    .FirstOrDefault();

                plot = plot + "-" + sectionName;

                if (string.IsNullOrWhiteSpace(plot))
                {
                    TempData["ErrorMessage"] = "Plot number is required.";
                    return RedirectToPage();
                }

                // Check duplicates
                var exists = await _context.tbl_Graves.AnyAsync(g => g.sys_plotNumber == plot && g.sys_sectionId == sectionId);
                if (exists)
                {
                    TempData["ErrorMessage"] = "A grave with this plot number already exists in this section.";
                    return RedirectToPage();
                }

                var grave = new tbl_Graves
                {
                    sys_plotNumber = plot,
                    sys_sectionId = sectionId,
                    sys_stageId = stageId,
                    sys_graveSaleStatus = status,
                    sys_price = price,
                    sys_latitude = latitude,
                    sys_longitude = longitude
                };

                _context.tbl_Graves.Add(grave);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Grave created successfully.";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Create", $"Created grave: {plot}");

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating grave: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", ex.Message);
                return RedirectToPage();
            }
        }

        // Bulk generate graves
        public async Task<IActionResult> OnPostBulkGenerateAsync()
        {
            try
            {
                int count = int.Parse(Request.Form["count"]);
                int sectionId = int.Parse(Request.Form["sys_sectionId"]);
                int stageId = int.Parse(Request.Form["sys_stageId"]);
                string seed = Request.Form["plotSeed"];
                double price = double.Parse(Request.Form["sys_price"]);

                var sectionName = _context.tbl_Sections
                    .Where(s => s.sys_sectionId == sectionId)
                    .Select(s => s.sys_sectionName)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(seed))
                {
                    TempData["ErrorMessage"] = "Plot number seed is required.";
                    return RedirectToPage();
                }

                var NewSeed = seed + "-" + sectionName;

                var gravesToAdd = new List<tbl_Graves>();
                string currentSeed = seed;

                for (int i = 0; i < count; i++)
                {
                    // Check duplicates
                    bool exists = await _context.tbl_Graves.AnyAsync(g => g.sys_plotNumber == NewSeed && g.sys_sectionId == sectionId);
                    if (exists)
                    {
                        TempData["ErrorMessage"] = $"Duplicate found: Plot {NewSeed} already exists in the section.";
                        _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Bulk Generate Error", $"Duplicate plot {NewSeed} found during bulk generation.");
                        if (int.TryParse(currentSeed, out int seedNumr))
                        {
                            currentSeed = (seedNumr + 1).ToString();
                            NewSeed = currentSeed + "-" + sectionName;
                        }
                        continue;
                    }

                    gravesToAdd.Add(new tbl_Graves
                    {
                        sys_plotNumber = currentSeed+"-"+sectionName,
                        sys_sectionId = sectionId,
                        sys_stageId = stageId,
                        sys_graveSaleStatus = "Unsold",
                        sys_price = price
                    });

                    // Increment plot number (assumes numeric seed)
                    if (int.TryParse(currentSeed, out int seedNum))
                    {
                        currentSeed = (seedNum + 1).ToString();
                    }
                    else
                    {
                        // If seed is not numeric, just append i
                        currentSeed = seed + (i + 1);
                    }
                }

                _context.tbl_Graves.AddRange(gravesToAdd);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{count} graves generated successfully.";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Bulk Generate", $"Generated {count} graves starting at {seed}");

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error bulk generating graves: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", ex.Message);
                return RedirectToPage();
            }
        }

        // Edit grave
        public async Task<IActionResult> OnPostEditAsync()
        {
            try
            {
                int id = int.Parse(Request.Form["sys_graveId"]);
                var grave = await _context.tbl_Graves.FindAsync(id);
                if (grave == null)
                {
                    TempData["ErrorMessage"] = "Grave not found.";
                    return RedirectToPage();
                }

                grave.sys_plotNumber = Request.Form["sys_plotNumber"];
                grave.sys_sectionId = int.Parse(Request.Form["sys_sectionId"]);
                grave.sys_stageId = int.Parse(Request.Form["sys_stageId"]);
                grave.sys_price = double.Parse(Request.Form["sys_price"]);
                grave.sys_graveSaleStatus = Request.Form["sys_graveSaleStatus"];
                grave.sys_latitude = double.Parse(Request.Form["sys_Latitude"]);
                grave.sys_longitude = double.Parse(Request.Form["sys_Longitude"]);

                _context.tbl_Graves.Update(grave);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Grave updated successfully.";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Edit", $"Edited grave: {grave.sys_plotNumber}");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error editing grave: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", ex.Message);
                return RedirectToPage();
            }
        }

        // Delete grave
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                int id = int.Parse(Request.Form["sys_graveId"]);
                var grave = await _context.tbl_Graves.FindAsync(id);
                if (grave == null)
                {
                    TempData["ErrorMessage"] = "Grave not found.";
                    return RedirectToPage();
                }

                _context.tbl_Graves.Remove(grave);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Grave deleted successfully.";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Delete", $"Deleted grave: {grave.sys_plotNumber}");
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting grave: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", ex.Message);
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteSelectedAsync()
        {
            try
            {
                //var selectedIds = Request.Form["SelectedIds"].ToString();

                if (string.IsNullOrEmpty(SelectedIds))
                {
                    TempData["ErrorMessage"] = "No graves selected for deletion.";
                    return RedirectToPage();
                }

                var ids = SelectedIds.Split(',').Select(int.Parse).ToList();

                var gravesToDelete = await _context.tbl_Graves
                    .Where(g => ids.Contains(g.sys_graveId))
                    .ToListAsync();

                if (gravesToDelete.Any())
                {
                    _context.tbl_Graves.RemoveRange(gravesToDelete);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{gravesToDelete.Count} grave(s) deleted successfully.";
                    await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Bulk Delete", $"Deleted {gravesToDelete.Count} graves.");
                }
                else
                {
                    TempData["ErrorMessage"] = "No matching graves found.";
                }

                return RedirectToPage();
            }

            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting selected graves: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", ex.Message);
                return RedirectToPage();
            }


        }




        // ViewModel to include Section and Stage names for display
        public class GraveViewModel
        {
            public int sys_graveId { get; set; }
            public string sys_plotNumber { get; set; }
            public string sys_graveSaleStatus { get; set; }
            public double sys_price { get; set; }
            public int sys_sectionId { get; set; }
            public string SectionName { get; set; }
            public int sys_stageId { get; set; }
            public string StageName { get; set; }
            public double sys_latitude { get; set; }
            public double sys_longitude { get; set; }
        }
    }
}
