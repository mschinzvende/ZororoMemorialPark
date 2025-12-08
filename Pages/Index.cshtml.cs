using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using static ZororoMemorialPark.Pages.Graves.GravesModel;

namespace ZororoMemorialPark.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Summary Metrics
        public int TotalGraves { get; set; }
        public int AvailableGraves { get; set; }
        public int ReservedGraves { get; set; }
        public int AllocatedGraves { get; set; }
        public int UnderConstructionGraves { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal RevenueMonth { get; set; }
        public decimal Outstanding { get; set; }
        public decimal PaymentsMonth { get; set; }

        // Chart JSON – existing
        public string SectionsJson { get; set; }
        public string GravesPerSectionJson { get; set; }
        public string StagesJson { get; set; }
        public string GravesPerStageJson { get; set; }
        public string RevenueMonthsJson { get; set; }
        public string RevenueValuesJson { get; set; }
        public string PaymentMethodNamesJson { get; set; }
        public string PaymentMethodTotalsJson { get; set; }

        // NEW – Development Stage by Section matrix
        public string SectionsForMatrixJson { get; set; }
        public string StagesForMatrixJson { get; set; }
        public string StageBySectionDataJson { get; set; }

        // NEW – Under Construction grouped by Section
        public string UnderConstructionSectionNamesJson { get; set; }
        public string UnderConstructionSectionCountsJson { get; set; }

        // Today's Activity
        public List<TodaysBurialVM> TodaysBurials { get; set; } = new();
        public List<TodaysPaymentVM> TodaysPayments { get; set; } = new();
        public List<UpcomingJobVM> UpcomingJobs { get; set; } = new();
        public class TodaysBurialVM
        {
            public string PlotNumber { get; set; }
            public string Section { get; set; }
            public string DeceasedName { get; set; }
            public string NextOfKin { get; set; }
            public DateOnly BurialDate { get; set; }
        }

        public class TodaysPaymentVM
        {
            public string Payer { get; set; }
            public decimal Amount { get; set; }
            public string Method { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public class UpcomingJobVM
        {
            public string PlotNumber { get; set; }
            public string JobType { get; set; }
            public string Supplier { get; set; }
            public DateOnly EndDate { get; set; }
            public bool IsOverdue => EndDate < DateOnly.FromDateTime(DateTime.Today);
        }


        public async Task OnGet()
        {
            // ========= BASE VIEWMODEL QUERIES (reuse from Graves page) =========

            // Get all sections and stages once
            var sectionList = await _context.tbl_Sections
                .OrderBy(s => s.sys_sectionName)
                .ToListAsync();

            var stageList = await _context.tbl_DevelopmentStages
                .OrderBy(s => s.sys_stageOrder)
                .ToListAsync();

            // Occupied graves base query
            var occupiedBaseQuery = from g in _context.tbl_Graves
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
                                        sys_saleStatus = g.sys_graveSaleStatus,
                                        sys_nextOfKinName = dc.sys_deceasedNextOfKinName,
                                        sys_nextOfKinContact = dc.sys_deceasedNextOfKinPhone,
                                        sys_sectionId = g.sys_sectionId,
                                        SectionName = s.sys_sectionName,
                                        sys_latitude = g.sys_latitude,
                                        sys_longitude = g.sys_longitude,
                                        sys_jobReminder = s.sys_jobReminder,
                                        sys_reminderPeriod = s.sys_reminderPeriod,
                                        topBuildStatus = DateOnly.FromDateTime(DateTime.Now).DayNumber - o.sys_allocatedOn.DayNumber >= 365 * s.sys_reminderPeriod
                                            ? $"Ready for {s.sys_jobReminder}"
                                            : "N/A",
                                        sys_graveTombStatus = DateOnly.FromDateTime(DateTime.Now).DayNumber - o.sys_allocatedOn.DayNumber >= 365
                                            ? "Ready for Tombstone"
                                            : al.sys_tombstoneStatus
                                    };

            // Under Construction base query
            var maxStageOrder = await _context.tbl_DevelopmentStages
                .MaxAsync(s => s.sys_stageOrder);

            var underConstructionBaseQuery = from g in _context.tbl_Graves
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
                                                 sys_price = g.sys_price,
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

            // Base grave view
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

            // ========= SUMMARY METRICS =========

            UnderConstructionGraves = await underConstructionBaseQuery
                .Where(g => g.sys_jobStatus == "In Progress" || g.sys_jobStatus == "Pending")
                .CountAsync();

            AvailableGraves = await baseQuery
                .Where(g => g.sys_graveSaleStatus == "Unsold")
                .CountAsync();

            ReservedGraves = await baseQuery
                .Where(g => g.sys_graveSaleStatus == "Reserved")
                .CountAsync();

            AllocatedGraves = await occupiedBaseQuery
                .Where(g => g.sys_saleStatus == "Occupied")
                .CountAsync();

            TotalGraves = await _context.tbl_Graves.CountAsync();

            // Revenue
            TotalRevenue = await _context.tbl_IncomePayments
                .SumAsync(x => (decimal?)x.sys_amount) ?? 0;

            PaymentsMonth = await _context.tbl_IncomePayments
                .Where(x => x.sys_date.Month == DateTime.Now.Month && x.sys_date.Year == DateTime.Now.Year)
                .SumAsync(x => (decimal?)x.sys_amount) ?? 0;

            RevenueMonth = PaymentsMonth;

            // ========= OUTSTANDING (Invoice total - payments) =========
            // Assumptions:
            // - tbl_InvoiceItems.sys_itemCost holds line totals (or unit price, but summed per invoice)
            // - tbl_IncomePayments has sys_invoiceId (FK) and sys_amount (paid amount)

            var invoiceTotals = await _context.tbl_InvoiceItems
                .GroupBy(ii => ii.sys_invoiceId)
                .Select(g => new
                {
                    sys_invoiceId = g.Key,
                    Total = g.Sum(ii => ii.sys_itemCost)
                })
                .ToListAsync();

            var invoicePayments = await _context.tbl_IncomePayments
                .GroupBy(p => p.sys_invoiceId)
                .Select(g => new
                {
                    sys_invoiceId = g.Key,
                    Paid = g.Sum(p => p.sys_amount)
                })
                .ToListAsync();

            var outstandingPerInvoice = from inv in invoiceTotals
                                        join pay in invoicePayments
                                            on inv.sys_invoiceId equals pay.sys_invoiceId
                                            into gj
                                        from pay in gj.DefaultIfEmpty()
                                        select (decimal)inv.Total - (decimal)(pay?.Paid ?? 0);

            Outstanding = outstandingPerInvoice.Sum();

            // ========= GRAVES PER SECTION (chart) =========

            var secSummary = await _context.tbl_Sections
                .Select(s => new
                {
                    s.sys_sectionName,
                    Count = _context.tbl_Graves.Count(g => g.sys_sectionId == s.sys_sectionId)
                })
                .ToListAsync();

            SectionsJson = JsonSerializer.Serialize(secSummary.Select(x => x.sys_sectionName));
            GravesPerSectionJson = JsonSerializer.Serialize(secSummary.Select(x => x.Count));

            // ========= GRAVES PER DEVELOPMENT STAGE (chart) =========

            var stageSummary = await _context.tbl_DevelopmentStages
                .Select(s => new
                {
                    s.sys_stageName,
                    Count = _context.tbl_Graves.Count(g => g.sys_stageId == s.sys_stageId)
                })
                .ToListAsync();

            StagesJson = JsonSerializer.Serialize(stageSummary.Select(x => x.sys_stageName));
            GravesPerStageJson = JsonSerializer.Serialize(stageSummary.Select(x => x.Count));

            // ========= MONTHLY REVENUE (LAST 12 MONTHS) =========

            var revRaw = await _context.tbl_IncomePayments
                .GroupBy(p => new { p.sys_date.Year, p.sys_date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(x => x.sys_amount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .Take(12)
                .ToListAsync();

            var rev = revRaw
                .Select(x => new
                {
                    Month = $"{x.Year}-{x.Month:00}",
                    x.Amount
                })
                .ToList();

            RevenueMonthsJson = JsonSerializer.Serialize(rev.Select(x => x.Month));
            RevenueValuesJson = JsonSerializer.Serialize(rev.Select(x => x.Amount));

            // ========= PAYMENT METHODS BREAKDOWN =========

            var methods = await _context.tbl_PaymentMethods
                .Select(m => new
                {
                    m.sys_paymentMethodName,
                    Total = _context.tbl_IncomePayments
                        .Where(p => p.sys_paymentMethod == m.sys_paymentMethodName)
                        .Sum(p => (decimal?)p.sys_amount) ?? 0
                })
                .ToListAsync();

            PaymentMethodNamesJson = JsonSerializer.Serialize(methods.Select(x => x.sys_paymentMethodName));
            PaymentMethodTotalsJson = JsonSerializer.Serialize(methods.Select(x => x.Total));

            // ========= DEVELOPMENT STAGE BY SECTION MATRIX =========

            var graveStageSection = await _context.tbl_Graves
                .GroupBy(g => new { g.sys_sectionId, g.sys_stageId })
                .Select(g => new
                {
                    g.Key.sys_sectionId,
                    g.Key.sys_stageId,
                    Count = g.Count()
                })
                .ToListAsync();

            var stageBySection = stageList.ToDictionary(
                stage => stage.sys_stageName,
                stage => sectionList
                    .Select(section =>
                        graveStageSection
                            .FirstOrDefault(x =>
                                x.sys_sectionId == section.sys_sectionId &&
                                x.sys_stageId == stage.sys_stageId)?.Count ?? 0
                    ).ToList()
            );

            SectionsForMatrixJson = JsonSerializer.Serialize(sectionList.Select(s => s.sys_sectionName));
            StagesForMatrixJson = JsonSerializer.Serialize(stageList.Select(s => s.sys_stageName));
            StageBySectionDataJson = JsonSerializer.Serialize(stageBySection);

            // ========= UNDER CONSTRUCTION BY SECTION =========

            var underConstructionGrouped = await _context.tbl_Graves
                .Where(g => g.sys_graveSaleStatus == "Under Construction")
                .GroupBy(g => g.sys_sectionId)
                .Select(g => new
                {
                    sys_sectionId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var sectionNameById = sectionList.ToDictionary(s => s.sys_sectionId, s => s.sys_sectionName);

            UnderConstructionSectionNamesJson = JsonSerializer.Serialize(
                underConstructionGrouped.Select(x =>
                    sectionNameById.ContainsKey(x.sys_sectionId)
                        ? sectionNameById[x.sys_sectionId]
                        : "Unknown")
            );

            UnderConstructionSectionCountsJson = JsonSerializer.Serialize(
                underConstructionGrouped.Select(x => x.Count)
            );


            // ==================== TODAY'S BURIALS ====================
            TodaysBurials = await (
                from alloc in _context.tbl_GraveAllocations
                join grave in _context.tbl_Graves on alloc.sys_graveId equals grave.sys_graveId
                join sec in _context.tbl_Sections on grave.sys_sectionId equals sec.sys_sectionId
                join dec in _context.tbl_Deceased on alloc.sys_deceasedId equals dec.sys_deceasedId
                where alloc.sys_allocatedOn == DateOnly.FromDateTime(DateTime.Today)
                select new TodaysBurialVM
                {
                    PlotNumber = grave.sys_plotNumber,
                    Section = sec.sys_sectionName,
                    DeceasedName = dec.sys_deceasedFullName,
                    NextOfKin = dec.sys_deceasedNextOfKinName,
                    BurialDate = alloc.sys_allocatedOn
                }
            ).ToListAsync();

            // ==================== TODAY'S PAYMENTS ====================
            TodaysPayments = await (
    from pay in _context.tbl_IncomePayments
    join inv in _context.tbl_Invoices on pay.sys_invoiceId equals inv.sys_invoiceId
    join cus in _context.tbl_Customers on inv.sys_customerId equals cus.sys_customerId
    where pay.sys_date == DateOnly.FromDateTime(DateTime.Today)
    orderby pay.sys_receiptNumber descending
    select new TodaysPaymentVM
    {
        Payer = cus.sys_paymentMadeBy,
        Amount = (decimal)pay.sys_amount,
        Method = pay.sys_paymentMethod,
        Timestamp = DateTime.Today // since no timestamp exists
    }
).ToListAsync();


            // ==================== UPCOMING JOB DEADLINES (Next 7 days) ====================
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            DateOnly nextWeek = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

            UpcomingJobs = await (
                from j in _context.tbl_Jobs
                join g in _context.tbl_Graves on j.sys_graveId equals g.sys_graveId
                join sec in _context.tbl_Sections on g.sys_sectionId equals sec.sys_sectionId
                join sup in _context.tbl_Suppliers on j.sys_supplierId equals sup.sys_supplierId
                where j.sys_endDate >= today && j.sys_endDate <= nextWeek
                      && (j.sys_jobStatus == "Pending" || j.sys_jobStatus == "In Progress")
                orderby j.sys_endDate
                select new UpcomingJobVM
                {
                    PlotNumber = g.sys_plotNumber,
                    JobType = sec.sys_jobReminder,
                    Supplier = sup.sys_supplierName,
                    EndDate = j.sys_endDate
                }
            ).ToListAsync();

        }
    }
}
