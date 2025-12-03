using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Reports
{
    // --- ViewModels for the report ---

    public class GraveStageSummaryVm
    {
        public string StageName { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SectionGraveSummaryVm
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;

        public int TotalGraves { get; set; }

        // Available (not allocated before the period start)
        public int OpeningAvailableGraves { get; set; }

        public int GravesAllocatedInPeriod { get; set; }
        public int BurialsInPeriod { get; set; }
        public int ReservationsCount { get; set; }

        public List<GraveStageSummaryVm> StageBreakdown { get; set; } = new();
    }

    public class ProductUsageSummaryVm
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;

        // Approx opening = current stock + used in period (assumes only consumption)
        public double OpeningQty { get; set; }
        public double UsedQty { get; set; }
        public double ClosingQty { get; set; }
    }

    public class GraveProductUsageVm
    {
        public string SectionName { get; set; } = string.Empty;
        public string PlotNumber { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;
        public double QuantityUsed { get; set; }
    }

    public class BurialRecordVm
    {
        public DateOnly BurialDate { get; set; }
        public string DeceasedName { get; set; } = string.Empty;
        public string GravePlot { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
    }

    public class ReservationRecordVm
    {
        public int SectionId { get; set; }
        public string SectionName { get; set; } = string.Empty;
        public string GravePlot { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
    }

    public class GraveActivityReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GraveActivityReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ====== FILTERS ======
        [BindProperty(SupportsGet = true)]
        public DateOnly FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SectionId { get; set; }

        public string ErrorMessage { get; set; } = string.Empty;

        // ====== DATA TO RENDER ======
        public List<tbl_Sections> AllSections { get; set; } = new();
        public List<SelectListItem> SectionSelectList { get; set; } = new();

        public List<SectionGraveSummaryVm> SectionSummaries { get; set; } = new();
        public List<ProductUsageSummaryVm> ProductSummaries { get; set; } = new();
        public List<GraveProductUsageVm> GraveProductUsage { get; set; } = new();
        public List<BurialRecordVm> Burials { get; set; } = new();
        public List<ReservationRecordVm> Reservations { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(DateOnly? fromDate, DateOnly? toDate, int? sectionId)
        {
            try
            {
                AllSections = await _context.tbl_Sections
                    .AsNoTracking()
                    .OrderBy(s => s.sys_sectionName)
                    .ToListAsync();

                FromDate = fromDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
                ToDate = toDate ?? DateOnly.FromDateTime(DateTime.Today);
                SectionId = sectionId;

                BuildSectionSelectList();

                await LoadReportDataAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading report: {ex.Message}";
            }

            return Page();
        }

        // PDF export endpoint (QuestPDF)
        public async Task<IActionResult> OnGetExportPdfAsync(DateOnly? fromDate, DateOnly? toDate, int? sectionId)
        {
            try
            {
                AllSections = await _context.tbl_Sections
                    .AsNoTracking()
                    .OrderBy(s => s.sys_sectionName)
                    .ToListAsync();

                FromDate = fromDate ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
                ToDate = toDate ?? DateOnly.FromDateTime(DateTime.Today);
                SectionId = sectionId;

                BuildSectionSelectList();

                await LoadReportDataAsync();



                var pdfBytes = BuildPdf();
                var fileName = $"GraveActivity_{FromDate:yyyyMMdd}_{ToDate:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting PDF: {ex.Message}";
                return Page();
            }
        }

        private void BuildSectionSelectList()
        {
            SectionSelectList = AllSections
                .Select(s => new SelectListItem
                {
                    Value = s.sys_sectionId.ToString(),
                    Text = s.sys_sectionName,
                    Selected = (SectionId.HasValue && SectionId.Value == s.sys_sectionId)
                })
                .ToList();
        }

        private async Task LoadReportDataAsync()
        {
            var fromDateTime = FromDate.ToDateTime(TimeOnly.MinValue);
            var toDateTime = ToDate.ToDateTime(TimeOnly.MaxValue);

            // ---- FILTERED SECTIONS ----
            var sectionQuery = _context.tbl_Sections.AsNoTracking().AsQueryable();
            if (SectionId.HasValue)
            {
                sectionQuery = sectionQuery.Where(s => s.sys_sectionId == SectionId.Value);
            }
            var sections = await sectionQuery.ToListAsync();

            // ---- STAGES ----
            var stages = await _context.tbl_DevelopmentStages
                .AsNoTracking()
                .ToListAsync();

            // ---- GRAVES (filtered by section if any) ----
            var graveQuery = _context.tbl_Graves.AsNoTracking().AsQueryable();
            if (SectionId.HasValue)
            {
                graveQuery = graveQuery.Where(g => g.sys_sectionId == SectionId.Value);
            }
            var graves = await graveQuery.ToListAsync();

            // ---- ALLOCATIONS (for opening stock, allocations, burials) ----
            var allocationQuery = from a in _context.tbl_GraveAllocations.AsNoTracking()
                                  join g in _context.tbl_Graves.AsNoTracking()
                                      on a.sys_graveId equals g.sys_graveId
                                  select new { a, g };

            if (SectionId.HasValue)
            {
                allocationQuery = allocationQuery
                    .Where(x => x.g.sys_sectionId == SectionId.Value);
            }

            var allocations = await allocationQuery.ToListAsync();

            // Allocations before period (for opening available graves)
            var allocationsBeforePeriod = allocations
                .Where(x => x.a.sys_allocatedOn < FromDate)
                .ToList();

            // Allocations in period
            var allocationsInPeriod = allocations
                .Where(x => x.a.sys_allocatedOn >= FromDate && x.a.sys_allocatedOn <= ToDate)
                .ToList();

            // Burials in period (using burial DateTime)
            var burialAllocationsInPeriod = allocations
                .Where(x => x.a.sys_dateTimeOfBurial >= fromDateTime &&
                            x.a.sys_dateTimeOfBurial <= toDateTime)
                .ToList();

            // ---- RESERVATIONS (current reservations only) ----
            var reservationQuery = from r in _context.tbl_GraveReservations.AsNoTracking()
                                   join g in _context.tbl_Graves.AsNoTracking()
                                       on r.sys_graveId equals g.sys_graveId
                                   join s in _context.tbl_Sections.AsNoTracking()
                                       on g.sys_sectionId equals s.sys_sectionId
                                   join c in _context.tbl_Customers.AsNoTracking()
                                       on r.sys_customerId equals c.sys_customerId
                                   select new { r, g, s, c };

            if (SectionId.HasValue)
            {
                reservationQuery = reservationQuery
                    .Where(x => x.g.sys_sectionId == SectionId.Value);
            }

            var reservationData = await reservationQuery.ToListAsync();

            Reservations = reservationData
                .Select(x => new ReservationRecordVm
                {
                    SectionId = x.s.sys_sectionId,
                    SectionName = x.s.sys_sectionName,
                    GravePlot = x.g.sys_plotNumber,
                    CustomerName = x.c.sys_paymentMadeBy,
                    CustomerPhone = x.c.sys_customerPhone
                })
                .OrderBy(r => r.SectionName)
                .ThenBy(r => r.GravePlot)
                .ToList();

            // ---- PRODUCT USAGE DURING PERIOD ----
            var usageQuery =
                from u in _context.tbl_JobEvaluationProductUsage.AsNoTracking()
                join j in _context.tbl_Jobs.AsNoTracking()
                    on u.sys_jobId equals j.sys_jobId
                join g in _context.tbl_Graves.AsNoTracking()
                    on j.sys_graveId equals g.sys_graveId
                join s in _context.tbl_Sections.AsNoTracking()
                    on g.sys_sectionId equals s.sys_sectionId
                join st in _context.tbl_DevelopmentStages.AsNoTracking()
                    on j.sys_stageId equals st.sys_stageId
                join p in _context.tbl_ProductsAndServices.AsNoTracking()
                    on u.sys_itemId equals p.sys_itemId
                where u.sys_dateUsed >= fromDateTime && u.sys_dateUsed <= toDateTime
                select new { u, j, g, s, st, p };

            if (SectionId.HasValue)
            {
                usageQuery = usageQuery
                    .Where(x => x.g.sys_sectionId == SectionId.Value);
            }

            var usageInPeriod = await usageQuery.ToListAsync();

            // Filter only consumable products for stock summary
            var consumableProducts = await _context.tbl_ProductsAndServices
                .AsNoTracking()
                .Where(p =>
                    p.sys_isActive &&
                    p.sys_itemType == "Product" &&
                    p.sys_category == "Consumable")
                .OrderBy(p => p.sys_itemName)
                .ToListAsync();

            // ---- BURIALS LIST (with deceased & section) ----
            var burialsQuery =
                from a in _context.tbl_GraveAllocations.AsNoTracking()
                join g in _context.tbl_Graves.AsNoTracking()
                    on a.sys_graveId equals g.sys_graveId
                join s in _context.tbl_Sections.AsNoTracking()
                    on g.sys_sectionId equals s.sys_sectionId
                join d in _context.tbl_Deceased.AsNoTracking()
                    on a.sys_deceasedId equals d.sys_deceasedId
                where a.sys_dateTimeOfBurial >= fromDateTime &&
                      a.sys_dateTimeOfBurial <= toDateTime
                select new { a, g, s, d };

            if (SectionId.HasValue)
            {
                burialsQuery = burialsQuery
                    .Where(x => x.g.sys_sectionId == SectionId.Value);
            }

            var burialEntities = await burialsQuery.ToListAsync();

            Burials = burialEntities
                .Select(x => new BurialRecordVm
                {
                    BurialDate = DateOnly.FromDateTime(x.a.sys_dateTimeOfBurial),
                    DeceasedName = x.d.sys_deceasedFullName,
                    GravePlot = x.g.sys_plotNumber,
                    SectionName = x.s.sys_sectionName
                })
                .OrderBy(b => b.BurialDate)
                .ThenBy(b => b.SectionName)
                .ThenBy(b => b.GravePlot)
                .ToList();

            // ---- SECTION SUMMARIES ----
            SectionSummaries = sections.Select(sec =>
            {
                var secGraves = graves
                    .Where(g => g.sys_sectionId == sec.sys_sectionId)
                    .ToList();

                int totalGraves = secGraves.Count;

                // Graves allocated before the period (no longer available at opening)
                var allocatedBeforeIds = allocationsBeforePeriod
                    .Where(x => x.g.sys_sectionId == sec.sys_sectionId)
                    .Select(x => x.g.sys_graveId)
                    .Distinct()
                    .ToHashSet();

                int openingAvailable = secGraves
                    .Count(g => !allocatedBeforeIds.Contains(g.sys_graveId));

                // Allocations in period
                int allocatedInPeriodCount = allocationsInPeriod
                    .Where(x => x.g.sys_sectionId == sec.sys_sectionId)
                    .Select(x => x.a.sys_graveId)
                    .Distinct()
                    .Count();

                // Burials in period
                int burialsInPeriodCount = burialAllocationsInPeriod
                    .Count(x => x.g.sys_sectionId == sec.sys_sectionId);

                // Reservations (current)
                int reservationsCount = Reservations
                    .Count(r => r.SectionId == sec.sys_sectionId);

                // Stage breakdown from current grave stage
                var stageBreakdown = secGraves
                    .GroupBy(g =>
                    {
                        var st = stages.FirstOrDefault(s => s.sys_stageId == g.sys_stageId);
                        return st != null ? st.sys_stageName : "Unknown";
                    })
                    .Select(g => new GraveStageSummaryVm
                    {
                        StageName = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return new SectionGraveSummaryVm
                {
                    SectionId = sec.sys_sectionId,
                    SectionName = sec.sys_sectionName,
                    TotalGraves = totalGraves,
                    OpeningAvailableGraves = openingAvailable,
                    GravesAllocatedInPeriod = allocatedInPeriodCount,
                    BurialsInPeriod = burialsInPeriodCount,
                    ReservationsCount = reservationsCount,
                    StageBreakdown = stageBreakdown
                };
            }).ToList();

            // ---- PRODUCT SUMMARY ----
            ProductSummaries = consumableProducts
                .Select(p =>
                {
                    var usedQty = usageInPeriod
                        .Where(x => x.p.sys_itemId == p.sys_itemId)
                        .Sum(x => x.u.sys_quantityUsed);

                    // Approximation: opening = current stock + usedQty, closing = current stock
                    double openingQty = p.sys_stockQuantity + usedQty;
                    double closingQty = p.sys_stockQuantity;

                    return new ProductUsageSummaryVm
                    {
                        ItemId = p.sys_itemId,
                        ItemName = p.sys_itemName,
                        OpeningQty = openingQty,
                        UsedQty = usedQty,
                        ClosingQty = closingQty
                    };
                })
                .Where(p => p.OpeningQty != 0 || p.UsedQty != 0 || p.ClosingQty != 0)
                .OrderBy(p => p.ItemName)
                .ToList();

            // ---- PER-GRAVE PRODUCT USAGE ----
            GraveProductUsage = usageInPeriod
                .Select(x => new GraveProductUsageVm
                {
                    SectionName = x.s.sys_sectionName,
                    PlotNumber = x.g.sys_plotNumber,
                    StageName = x.st.sys_stageName,
                    ProductName = x.p.sys_itemName,
                    QuantityUsed = x.u.sys_quantityUsed
                })
                .OrderBy(r => r.SectionName)
                .ThenBy(r => r.PlotNumber)
                .ThenBy(r => r.StageName)
                .ThenBy(r => r.ProductName)
                .ToList();
        }

        // ============================
        // QUESTPDF DOCUMENT BUILDER
        // ============================
        private byte[] BuildPdf()
        {
            var fromText = FromDate.ToString("yyyy-MM-dd");
            var toText = ToDate.ToString("yyyy-MM-dd");

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(25);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor("#000000"));

                    // ====================================================
                    // HEADER
                    // ====================================================
                    page.Header().Column(col =>
                    {
                        col.Item().Text("ZORORO MEMORIAL PARK")
                            .Bold().FontSize(18).FontColor("#006A46");

                        col.Item().Text("Grave Activity & Stock Report")
                            .SemiBold().FontSize(13);

                        col.Item().Text($"Period: {fromText}  to  {toText}")
                            .FontSize(10);

                        col.Item().PaddingBottom(5)
                            .BorderBottom(1).BorderColor("#CCCCCC");
                    });

                    // ====================================================
                    // CONTENT
                    // ====================================================
                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(15);

                        // ====================================================
                        // SUMMARY SECTION
                        // ====================================================
                        col.Item().Text("Summary")
                            .FontSize(13).SemiBold().FontColor("#006A46");

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Sections in scope: {SectionSummaries.Count}");
                                c.Item().Text($"Opening available graves: {SectionSummaries.Sum(s => s.OpeningAvailableGraves)}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Allocated in period: {SectionSummaries.Sum(s => s.GravesAllocatedInPeriod)}");
                                c.Item().Text($"Burials in period: {Burials.Count}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Current reservations: {Reservations.Count}");
                                c.Item().Text($"Total product qty used: {ProductSummaries.Sum(p => p.UsedQty)}");
                            });
                        });


                        // ====================================================
                        // SECTION ACTIVITY TABLE
                        // ====================================================
                        col.Item().Text("Section Activity & Stages")
                            .FontSize(12).SemiBold().FontColor("#006A46");

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2); // Section
                                cols.RelativeColumn();   // Total
                                cols.RelativeColumn();   // Opening
                                cols.RelativeColumn();   // Allocated
                                cols.RelativeColumn();   // Burials
                                cols.RelativeColumn();   // Reservations
                            });

                            // HEADER
                            t.Header(h =>
                            {
                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Section");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Total");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Opening");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Allocated");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Burials");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Resv");
                            });

                            // BODY
                            foreach (var s in SectionSummaries)
                            {
                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(s.SectionName);

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(s.TotalGraves.ToString());

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(s.OpeningAvailableGraves.ToString());

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(s.GravesAllocatedInPeriod.ToString());

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(s.BurialsInPeriod.ToString());

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(s.ReservationsCount.ToString());
                            }
                        });


                        // ====================================================
                        // PRODUCT USAGE SUMMARY
                        // ====================================================
                        col.Item().Text("Consumable Products - Stock & Usage")
                            .FontSize(12).SemiBold().FontColor("#006A46");

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Product
                                cols.RelativeColumn();   // Opening
                                cols.RelativeColumn();   // Used
                                cols.RelativeColumn();   // Closing
                            });

                            t.Header(h =>
                            {
                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Product");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Opening");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Used");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Closing");
                            });

                            foreach (var p in ProductSummaries)
                            {
                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(p.ItemName);

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(p.OpeningQty.ToString("0.##"));

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(p.UsedQty.ToString("0.##"));

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(p.ClosingQty.ToString("0.##"));
                            }
                        });


                        // ====================================================
                        // BURIALS SECTION
                        // ====================================================
                        col.Item().Text("Burials in Period")
                            .FontSize(12).SemiBold().FontColor("#006A46");

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();   // Date
                                cols.RelativeColumn(3);  // Deceased
                                cols.RelativeColumn();   // Grave
                                cols.RelativeColumn();   // Section
                            });

                            t.Header(h =>
                            {
                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Date");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Deceased");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Grave");

                                h.Cell().Background("#006A46").Padding(3)
                                    .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                    .Text("Section");
                            });

                            foreach (var b in Burials)
                            {
                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(b.BurialDate.ToString("yyyy-MM-dd"));

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(b.DeceasedName);

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(b.GravePlot);

                                t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                    .Text(b.SectionName);
                            }
                        });


                        // ====================================================
                        // RESERVATIONS (ONLY IF EXISTS)
                        // ====================================================
                        if (Reservations.Any())
                        {
                            col.Item().Text("Current Reservations")
                                .FontSize(12).SemiBold().FontColor("#006A46");

                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();   // Section
                                    cols.RelativeColumn();   // Grave
                                    cols.RelativeColumn(2);  // Customer
                                    cols.RelativeColumn();   // Phone
                                });

                                t.Header(h =>
                                {
                                    h.Cell().Background("#006A46").Padding(3)
                                        .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                        .Text("Section");

                                    h.Cell().Background("#006A46").Padding(3)
                                        .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                        .Text("Grave");

                                    h.Cell().Background("#006A46").Padding(3)
                                        .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                        .Text("Customer");

                                    h.Cell().Background("#006A46").Padding(3)
                                        .DefaultTextStyle(x => x.FontColor("#FFFFFF").SemiBold().FontSize(9))
                                        .Text("Phone");
                                });

                                foreach (var r in Reservations)
                                {
                                    t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                        .Text(r.SectionName);

                                    t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                        .Text(r.GravePlot);

                                    t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                        .Text(r.CustomerName);

                                    t.Cell().BorderBottom(0.5f).BorderColor("#DDDDDD").PaddingVertical(2)
                                        .Text(r.CustomerPhone);
                                }
                            });
                        }
                    });

                    // ====================================================
                    // FOOTER
                    // ====================================================
                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().AlignLeft().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(8);

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            });

            return doc.GeneratePdf();
        }

    }
}
