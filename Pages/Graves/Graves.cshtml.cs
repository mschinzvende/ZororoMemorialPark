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
using System.Security.Policy;

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

        public List<GraveViewModel> AvailableGraves { get; set; } = new List<GraveViewModel>();
        public List<GraveViewModel> ReservedGraves { get; set; } = new List<GraveViewModel>();
        public List<OccupiedGraveViewModel> OccupiedGraves { get; set; } = new List<OccupiedGraveViewModel>();
        public List<UnderConstructionGraveViewModel> UnderConstructionGraves { get; set; } = new List<UnderConstructionGraveViewModel>();

        public IList<tbl_Tombstones> AvailableTombstones { get; set; } = new List<tbl_Tombstones>();
        public IList<tbl_Sections> Sections { get; set; } = new List<tbl_Sections>();
        public IList<tbl_DevelopmentStages> Stages { get; set; } = new List<tbl_DevelopmentStages>();

        // Paging + search
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        // Totals for each tab
        public int UnderConstructionTotalCount { get; set; }
        public int AvailableTotalCount { get; set; }
        public int ReservedTotalCount { get; set; }
        public int OccupiedTotalCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ActiveTab { get; set; } = "available";


        public async Task OnGetAsync(int? pageNumber, int? pageSize, string searchTerm)
        {
            try
            {
                PageNumber = pageNumber.GetValueOrDefault(1);
                if (PageNumber < 1) PageNumber = 1;

                PageSize = pageSize.GetValueOrDefault(10);
                if (PageSize <= 0) PageSize = 10;

                SearchTerm = searchTerm;

                // Load sections and stages for dropdowns
                Sections = await _context.tbl_Sections.AsNoTracking().ToListAsync();
                Stages = await _context.tbl_DevelopmentStages.AsNoTracking().ToListAsync();

                // Base query for graves with section and stage info
                var baseQuery =
                    from g in _context.tbl_Graves
                    join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                    join st in _context.tbl_DevelopmentStages on g.sys_stageId equals st.sys_stageId
                    
                    orderby g.sys_plotNumber
                    select new GraveViewModel
                    {
                        sys_graveId = g.sys_graveId,
                        sys_plotNumber = g.sys_plotNumber,
                        sys_graveSaleStatus = g.sys_graveSaleStatus,
                        
                        sys_price = s.sys_SecPrice,
                        sys_sectionId = g.sys_sectionId,
                        SectionName = s.sys_sectionName,
                        sys_stageId = g.sys_stageId,
                        StageName = st.sys_stageName,
                        sys_latitude = g.sys_latitude,
                        sys_longitude = g.sys_longitude
                    };

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    baseQuery = baseQuery.Where(g =>
                        g.sys_plotNumber.Contains(SearchTerm) ||
                        g.SectionName.Contains(SearchTerm));
                }

                var occupiedBaseQuery =
                    from g in _context.tbl_Graves
                    join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                    join o in _context.tbl_GraveAllocations on g.sys_graveId equals o.sys_graveId
                    join dc in _context.tbl_Deceased on o.sys_deceasedId equals dc.sys_deceasedId
                    join al in _context.tbl_GraveAllocations on g.sys_graveId equals al.sys_graveId
                    orderby g.sys_plotNumber
                    select new OccupiedGraveViewModel
                    {
                        sys_graveId = g.sys_graveId,
                        sys_plotNumber = g.sys_plotNumber,
                        sys_occupationDate = o.sys_allocatedOn,
                        sys_deceasedName = dc.sys_deceasedFullName,
                        sys_saleStatus = g.sys_graveSaleStatus,
                        sys_nextOfKinName = dc.sys_deceasedNextOfKinName,
                        sys_nextOfKinContact = dc.sys_deceasedNextOfKinPhone,
                        sys_sectionId = g.sys_sectionId,
                        SectionName = s.sys_sectionName,
                        sys_latitude = g.sys_latitude,
                        sys_longitude = g.sys_longitude,
                        sys_jobReminder = s.sys_jobReminder,
                        sys_reminderPeriod = s.sys_reminderPeriod,
                        topBuildStatus =
                            DateOnly.FromDateTime(DateTime.Now).DayNumber - o.sys_allocatedOn.DayNumber >= 365 * s.sys_reminderPeriod
                                ? $"Ready for {s.sys_jobReminder}"
                                : "N/A",
                        sys_graveTombStatus =
                            DateOnly.FromDateTime(DateTime.Now).DayNumber - o.sys_allocatedOn.DayNumber >= 365
                                ? "Ready for Tombstone"
                                : al.sys_tombstoneStatus
                    };

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    occupiedBaseQuery = occupiedBaseQuery.Where(g =>
                        g.sys_plotNumber.Contains(SearchTerm) ||
                        g.SectionName.Contains(SearchTerm) || 
                        g.sys_deceasedName.Contains(SearchTerm) ||
                        g.sys_nextOfKinName.Contains(SearchTerm));
                }

                // Under construction query
                var maxStageOrder = await _context.tbl_DevelopmentStages
                    .MaxAsync(s => s.sys_stageOrder);

                var underConstructionBaseQuery =
                    from g in _context.tbl_Graves
                    join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                    join st in _context.tbl_DevelopmentStages on g.sys_stageId equals st.sys_stageId
                    join j in _context.tbl_Jobs on g.sys_graveId equals j.sys_graveId
                    join sup in _context.tbl_Suppliers on j.sys_supplierId equals sup.sys_supplierId
                    where st.sys_stageOrder <= maxStageOrder
                    orderby g.sys_plotNumber
                    select new UnderConstructionGraveViewModel
                    {
                        sys_graveId = g.sys_graveId,
                        sys_plotNumber = g.sys_plotNumber,
                        sys_graveSaleStatus = g.sys_graveSaleStatus,
                        sys_price = s.sys_SecPrice,
                        sys_sectionId = g.sys_sectionId,
                        SectionName = s.sys_sectionName,
                        sys_stageId = g.sys_stageId,
                        StageName = st.sys_stageName,
                        sys_endDate = j.sys_endDate,
                        sys_jobId = j.sys_jobId,
                        sys_jobStatus = j.sys_jobStatus,
                        sys_supplierId = sup.sys_supplierId,
                        sys_supplierName = sup.sys_supplierName,
                        sys_latitude = g.sys_latitude,
                        sys_longitude = g.sys_longitude
                    };

                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    underConstructionBaseQuery = underConstructionBaseQuery.Where(g =>
                        g.sys_plotNumber.Contains(SearchTerm) ||
                        g.SectionName.Contains(SearchTerm));
                }

                // Apply status filters and paging

                // Under Construction
                var ucQuery = underConstructionBaseQuery
    .Where(g => g.sys_jobStatus == "In Progress" || g.sys_jobStatus == "Pending");

                if (ActiveTab == "underconstruction" && !string.IsNullOrWhiteSpace(SearchTerm))
                {
                    ucQuery = ucQuery.Where(g =>
                        g.sys_plotNumber.Contains(SearchTerm) ||
                        g.SectionName.Contains(SearchTerm));
                }


                UnderConstructionTotalCount = await ucQuery.CountAsync();
                UnderConstructionGraves = await ucQuery
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Available (Unsold)
                var availableQuery = baseQuery.Where(g => g.sys_graveSaleStatus == "Unsold");

                if (ActiveTab == "available" && !string.IsNullOrWhiteSpace(SearchTerm))
                {
                    availableQuery = availableQuery.Where(g =>
                        g.sys_plotNumber.Contains(SearchTerm) ||
                        g.SectionName.Contains(SearchTerm));
                }
                AvailableTotalCount = await availableQuery.CountAsync();
                AvailableGraves = await availableQuery
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Reserved
                var reservedQuery = baseQuery.Where(g => g.sys_graveSaleStatus == "Reserved");

                if (ActiveTab == "reserved" && !string.IsNullOrWhiteSpace(SearchTerm))
                {
                    reservedQuery = reservedQuery.Where(g =>
                        g.sys_plotNumber.Contains(SearchTerm) ||
                        g.SectionName.Contains(SearchTerm));
                }

                ReservedTotalCount = await reservedQuery.CountAsync();
                ReservedGraves = await reservedQuery
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Occupied
                var occQuery = occupiedBaseQuery.Where(g => g.sys_saleStatus == "Occupied");

                if (ActiveTab == "occupied" && !string.IsNullOrWhiteSpace(SearchTerm))
                {
                    occQuery = occQuery.Where(g =>
                        g.sys_plotNumber.Contains(SearchTerm) ||
                        g.SectionName.Contains(SearchTerm) || g.sys_deceasedName.Contains(SearchTerm) ||
                        g.sys_nextOfKinName.Contains(SearchTerm));
                }

                OccupiedTotalCount = await occQuery.CountAsync();
                OccupiedGraves = await occQuery
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                // Available tombstones
                AvailableTombstones = await _context.tbl_Tombstones
                    .Where(t => t.sys_status == "Available" && t.sys_quantityAvailable > 0)
                    .ToListAsync();

                // Log access
                var userId = _userManager.GetUserId(User);
                await _appLogger.LogActivityAsync(userId, "Access", "Accessed Graves page with server-side pagination");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading graves: " + ex.Message;
            }
        }

        //Get the next plot number
        public async Task<IActionResult> OnGetNextPlotNumber(int sectionId)
        {
            var section = await _context.tbl_Sections
                .Where(s => s.sys_sectionId == sectionId)
                .Select(s => new { s.sys_lastGraveNo })
                .FirstOrDefaultAsync();

            if (section == null)
                return new JsonResult(new { nextPlot = "" });

            return new JsonResult(new
            {
                nextPlot = section.sys_lastGraveNo + 1
            });
        }



        // Single grave creation
        public async Task<IActionResult> OnPostCreateAsync()
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int sectionId = int.Parse(Request.Form["sys_sectionId"]);
                int stageId = int.Parse(Request.Form["sys_stageId"]);
                string status = Request.Form["sys_graveSaleStatus"];
                double latitude = double.Parse(Request.Form["sys_Latitude"]);
                double longitude = double.Parse(Request.Form["sys_Longitude"]);
                int plotNumber = int.Parse(Request.Form["sys_plotNumber"]);

                var section = await _context.tbl_Sections
                    .FirstOrDefaultAsync(s => s.sys_sectionId == sectionId);

                if (section == null)
                {
                    TempData["ErrorMessage"] = "Invalid section.";
                    return RedirectToPage();
                }

                //int nextPlotNo = section.sys_lastGraveNo + 1;
                string plot = $"{plotNumber}-{section.sys_sectionName}";

                var exists = await _context.tbl_Graves
                    .AnyAsync(g => g.sys_plotNumber == plot && g.sys_sectionId == sectionId);

                if (exists)
                {
                    TempData["ErrorMessage"] = "Duplicate grave detected.";
                    return RedirectToPage();
                }

                var grave = new tbl_Graves
                {
                    sys_plotNumber = plot,
                    sys_sectionId = sectionId,
                    sys_stageId = stageId,
                    sys_graveSaleStatus = status,
                    sys_latitude = latitude,
                    sys_longitude = longitude
                };

                _context.tbl_Graves.Add(grave);

                //section.sys_lastGraveNo = nextPlotNo;
                //_context.tbl_Sections.Update(section);

                await _context.SaveChangesAsync();

                _context.tbl_Jobs.Add(new tbl_Jobs
                {
                    sys_graveId = grave.sys_graveId,
                    sys_stageId = stageId,
                    sys_jobStatus = "Pending",
                    sys_startDate = DateOnly.FromDateTime(DateTime.Now),
                    sys_endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                    sys_supplierId = 11
                });

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["SuccessMessage"] = "Grave created successfully.";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Create", $"Created grave {plot}");

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }


        // Bulk generate graves
        public async Task<IActionResult> OnPostBulkGenerateAsync()
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                int count = int.Parse(Request.Form["count"]);
                int sectionId = int.Parse(Request.Form["sys_sectionId"]);
                int stageId = int.Parse(Request.Form["sys_stageId"]);

                var section = await _context.tbl_Sections
                    .FirstOrDefaultAsync(s => s.sys_sectionId == sectionId);

                if (section == null)
                {
                    TempData["ErrorMessage"] = "Invalid section.";
                    return RedirectToPage();
                }

                int currentPlot = section.sys_lastGraveNo;
                var graves = new List<tbl_Graves>();

                for (int i = 1; i <= count; i++)
                {
                    currentPlot++;
                    graves.Add(new tbl_Graves
                    {
                        sys_plotNumber = $"{currentPlot}-{section.sys_sectionName}",
                        sys_sectionId = sectionId,
                        sys_stageId = stageId,
                        sys_graveSaleStatus = "Under Construction"
                    });
                }

                _context.tbl_Graves.AddRange(graves);

                section.sys_lastGraveNo = currentPlot;
                _context.tbl_Sections.Update(section);

                await _context.SaveChangesAsync();

                _context.tbl_Jobs.AddRange(
                    graves.Select(g => new tbl_Jobs
                    {
                        sys_graveId = g.sys_graveId,
                        sys_stageId = stageId,
                        sys_jobStatus = "Pending",
                        sys_startDate = DateOnly.FromDateTime(DateTime.Now),
                        sys_endDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7)),
                        sys_supplierId = 11
                    })
                );

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["SuccessMessage"] = $"{graves.Count} graves generated successfully.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["ErrorMessage"] = ex.Message;
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
                
                grave.sys_graveSaleStatus = Request.Form["sys_graveSaleStatus"];
                grave.sys_latitude = double.Parse(Request.Form["sys_Latitude"]);
                grave.sys_longitude = double.Parse(Request.Form["sys_Longitude"]);

                _context.tbl_Graves.Update(grave);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Grave updated successfully.";
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Edit",
                    $"Edited grave: {grave.sys_plotNumber}");
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
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Delete",
                    $"Deleted grave: {grave.sys_plotNumber}");
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
                    await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Bulk Delete",
                        $"Deleted {gravesToDelete.Count} graves.");
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

        public async Task<IActionResult> OnPostSaveTombstoneOrderAsync()
        {
            try
            {
                int graveId = int.Parse(Request.Form["sys_graveId"]);
                int tombstoneId = int.Parse(Request.Form["sys_tombstoneId"]);
                string buyerName = Request.Form["sys_buyerName"];
                string buyerContact = Request.Form["sys_buyerContact"];
                double amountPaid = double.Parse(Request.Form["sys_amountPaid"]);
                DateOnly installationDate = DateOnly.FromDateTime(DateTime.Parse(Request.Form["sys_installationDate"]));
                string installedBy = Request.Form["sys_installedBy"];
                string remarks = Request.Form["sys_remarks"];

                var tombstone = await _context.tbl_Tombstones.FindAsync(tombstoneId);

                var customer = new tbl_Customers
                {
                    sys_paymentMadeBy = buyerName + "(Tombstone Purchase)"
                };
                _context.tbl_Customers.Add(customer);
                await _context.SaveChangesAsync();

                var invoice = new tbl_Invoices
                {
                    sys_invoiceDate = DateOnly.FromDateTime(DateTime.Now),
                    sys_customerId = customer.sys_customerId,
                    sys_invoiceDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7))
                };
                _context.tbl_Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                var invoiceItem = new tbl_InvoiceItems
                {
                    sys_invoiceId = invoice.sys_invoiceId,
                    sys_itemName = $"Tombstone - {tombstone.sys_tombstoneName}",
                    sys_itemCost = tombstone.sys_price
                };
                _context.tbl_InvoiceItems.Add(invoiceItem);
                await _context.SaveChangesAsync();

                var sale = new tbl_TombstoneSales
                {
                    sys_tombstoneId = tombstoneId,
                    sys_buyerName = buyerName,
                    sys_buyerContact = buyerContact,
                    sys_amountPaid = amountPaid,
                    sys_paymentStatus = tombstone.sys_price <= amountPaid ? "Paid" : "Payment Due",
                    sys_installationStatus = "Not Installed",
                    sys_saleDate = DateOnly.FromDateTime(DateTime.Now),
                    sys_invoiceId = invoice.sys_invoiceId
                };
                _context.tbl_TombstoneSales.Add(sale);
                await _context.SaveChangesAsync();

                var installation = new tbl_TombstoneInstallations
                {
                    sys_saleId = sale.sys_saleId,
                    sys_graveId = graveId,
                    sys_installationDate = installationDate,
                    sys_installedBy = installedBy,
                    sys_remarks = remarks,
                    sys_status = "Pending"
                };
                _context.tbl_TombstoneInstallations.Add(installation);
                await _context.SaveChangesAsync();

                tombstone.sys_quantityAvailable -= 1;
                if (tombstone.sys_quantityAvailable <= 0)
                    tombstone.sys_status = "Out of Stock";

                var graveAllocation = await _context.tbl_GraveAllocations
                    .FirstOrDefaultAsync(t => t.sys_graveId == graveId);

                if (graveAllocation != null)
                {
                    graveAllocation.sys_tombstoneStatus = "Awaiting Installation";
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tombstone sale created successfully.";

                return RedirectToPage(new { id = graveId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error saving tombstone order: {ex.Message}";
                return RedirectToPage();
            }
        }

        // View models
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

        public class OccupiedGraveViewModel
        {
            public int sys_graveId { get; set; }
            public string sys_plotNumber { get; set; }
            public string sys_graveTombStatus { get; set; }

            public string sys_deceasedName { get; set; }

            
            public DateOnly sys_occupationDate { get; set; }
            public string sys_saleStatus { get; set; }
            public string sys_nextOfKinName { get; set; }
            public string sys_nextOfKinContact { get; set; }
            public int sys_sectionId { get; set; }
            public string SectionName { get; set; }
            public string sys_jobReminder { get; set; }
            public int sys_reminderPeriod { get; set; }
            public string topBuildStatus { get; set; }
            public double sys_latitude { get; set; }
            public double sys_longitude { get; set; }
        }

        public class UnderConstructionGraveViewModel
        {
            public int sys_graveId { get; set; }
            public string sys_plotNumber { get; set; }
            public string sys_graveSaleStatus { get; set; }
            public double sys_price { get; set; }
            public int sys_sectionId { get; set; }
            public string SectionName { get; set; }
            public int sys_stageId { get; set; }
            public string StageName { get; set; }
            public DateOnly sys_endDate { get; set; }
            public int sys_jobId { get; set; }
            public string sys_jobStatus { get; set; }
            public int sys_supplierId { get; set; }
            public string sys_supplierName { get; set; }
            public double sys_latitude { get; set; }
            public double sys_longitude { get; set; }
        }
    }
}
