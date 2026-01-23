using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // Top month metrics (cards)
        // =========================
        public string CurrentMonthLabel { get; set; } = "";
        public decimal RevenueThisMonth { get; set; }
        public decimal OutstandingThisMonth { get; set; }
        public int GravesSoldThisMonth { get; set; }
        public int NewReservationsThisMonth { get; set; }

        // =========================
        // Charts JSON
        // =========================
        public string UnderConstructionMonthSectionNamesJson { get; set; } = "[]";
        public string UnderConstructionMonthSectionCountsJson { get; set; } = "[]";

        public string BurialsMonthSectionNamesJson { get; set; } = "[]";
        public string BurialsMonthSectionCountsJson { get; set; } = "[]";

        // Stacked matrix: { "StageName": [counts per section] }
        public string StageMonthMatrixSectionLabelsJson { get; set; } = "[]";
        public string StageMonthMatrixDataJson { get; set; } = "{}";

        public string Revenue6MonthsLabelsJson { get; set; } = "[]";
        public string Revenue6MonthsValuesJson { get; set; } = "[]";

        // Available Graves for Sale per Section
        public string AvailableSaleSectionNamesJson { get; set; } = "[]";
        public string AvailableSaleSectionCountsJson { get; set; } = "[]";


        public async Task OnGet()
        {
            // =========================
            // Date boundaries (current month)
            // =========================
            var now = DateTime.Today;
            var monthStart = new DateOnly(now.Year, now.Month, 1);
            var monthEndExclusive = monthStart.AddMonths(1);

            CurrentMonthLabel = now.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

            // =========================
            // Revenue (This Month)
            // =========================
            RevenueThisMonth = await _context.tbl_IncomePayments
                .Where(p => p.sys_date >= monthStart && p.sys_date < monthEndExclusive)
                .SumAsync(p => (decimal?)p.sys_amount) ?? 0m;

            // =========================
            // Outstanding (This Month)
            // Definition used (no invoice date dependency):
            // - Take invoices that had at least one payment in the month
            // - Outstanding = invoice total - total paid (to date)
            // =========================
            var invoiceIdsTouchedThisMonth = await _context.tbl_IncomePayments
                .Where(p => p.sys_date >= monthStart && p.sys_date < monthEndExclusive)
                .Select(p => p.sys_invoiceId)
                .Distinct()
                .ToListAsync();

            if (invoiceIdsTouchedThisMonth.Count == 0)
            {
                OutstandingThisMonth = 0m;
            }
            else
            {
                var invoiceTotals = await _context.tbl_InvoiceItems
                    .Where(ii => invoiceIdsTouchedThisMonth.Contains(ii.sys_invoiceId))
                    .GroupBy(ii => ii.sys_invoiceId)
                    .Select(g => new
                    {
                        sys_invoiceId = g.Key,
                        Total = g.Sum(x => x.sys_itemCost)
                    })
                    .ToListAsync();

                var invoicePaymentsAll = await _context.tbl_IncomePayments
                    .Where(p => invoiceIdsTouchedThisMonth.Contains(p.sys_invoiceId))
                    .GroupBy(p => p.sys_invoiceId)
                    .Select(g => new
                    {
                        sys_invoiceId = g.Key,
                        Paid = g.Sum(x => x.sys_amount)
                    })
                    .ToListAsync();

                var paidMap = invoicePaymentsAll.ToDictionary(x => x.sys_invoiceId, x => (decimal)x.Paid);

                OutstandingThisMonth = invoiceTotals.Sum(inv =>
                {
                    paidMap.TryGetValue(inv.sys_invoiceId, out var paid);
                    return (decimal)inv.Total - paid;
                });

                if (OutstandingThisMonth < 0) OutstandingThisMonth = 0; // safety
            }

            // =========================
            // Graves Sold (This Month) = allocations in month
            // =========================
            GravesSoldThisMonth = await _context.tbl_GraveAllocations
                .Where(a => a.sys_allocatedOn >= monthStart && a.sys_allocatedOn < monthEndExclusive)
                .CountAsync();

            // =========================
            // New Reservations (This Month)
            // Requires tbl_GraveReservations (see section 1)
            // =========================
            NewReservationsThisMonth = await _context.tbl_GraveReservations
                .Where(r => r.sys_reservedOn >= monthStart && r.sys_reservedOn < monthEndExclusive
                            )
                .CountAsync();

            // =========================
            // Lookup lists (sections + stages)
            // =========================
            var sectionList = await _context.tbl_Sections
                .OrderBy(s => s.sys_sectionName)
                .ToListAsync();

            var stageList = await _context.tbl_DevelopmentStages
                .OrderBy(s => s.sys_stageOrder)
                .ToListAsync();

            var sectionNameById = sectionList.ToDictionary(x => x.sys_sectionId, x => x.sys_sectionName);

            // =========================
            // CHART 1: Under Construction per Section (Current Month)
            // Interpretation used:
            // - Graves with jobs ENDING this month and job is Pending/In Progress
            // =========================
            var underConstructionMonth = await (
                from j in _context.tbl_Jobs
                join g in _context.tbl_Graves on j.sys_graveId equals g.sys_graveId
                join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                where j.sys_endDate >= monthStart && j.sys_endDate < monthEndExclusive
                      && (j.sys_jobStatus == "Pending" || j.sys_jobStatus == "In Progress")
                group j by s.sys_sectionName into grp
                select new
                {
                    SectionName = grp.Key,
                    Count = grp.Select(x => x.sys_graveId).Distinct().Count()
                }
            ).ToListAsync();

            UnderConstructionMonthSectionNamesJson = JsonSerializer.Serialize(underConstructionMonth.Select(x => x.SectionName));
            UnderConstructionMonthSectionCountsJson = JsonSerializer.Serialize(underConstructionMonth.Select(x => x.Count));

            // =========================
            // CHART 2: Burials (Allocations) per Section (Current Month)
            // =========================
            var burialsMonth = await (
                from a in _context.tbl_GraveAllocations
                join g in _context.tbl_Graves on a.sys_graveId equals g.sys_graveId
                join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                where a.sys_allocatedOn >= monthStart && a.sys_allocatedOn < monthEndExclusive
                group a by s.sys_sectionName into grp
                select new
                {
                    SectionName = grp.Key,
                    Count = grp.Count()
                }
            ).ToListAsync();

            BurialsMonthSectionNamesJson = JsonSerializer.Serialize(burialsMonth.Select(x => x.SectionName));
            BurialsMonthSectionCountsJson = JsonSerializer.Serialize(burialsMonth.Select(x => x.Count));



            // =========================
            // Available Graves for Sale per Section
            // Definition:
            // - sys_graveSaleStatus == "Unsold"
            // =========================
            var availableBySection = await (
                from g in _context.tbl_Graves
                join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                where g.sys_graveSaleStatus == "Unsold"
                group g by s.sys_sectionName into grp
                select new
                {
                    SectionName = grp.Key,
                    Count = grp.Count()
                }
            ).ToListAsync();

            AvailableSaleSectionNamesJson = JsonSerializer.Serialize(
                availableBySection.Select(x => x.SectionName)
            );

            AvailableSaleSectionCountsJson = JsonSerializer.Serialize(
                availableBySection.Select(x => x.Count)
            );


            // =========================
            // CHART 3: Development Stage per Section (Current Month) - stacked
            // Interpretation used:
            // - Look at graves in active construction jobs ending this month
            // - Bucket them by section + current stage
            // =========================
            var stageSectionCounts = await (
                from j in _context.tbl_Jobs
                join g in _context.tbl_Graves on j.sys_graveId equals g.sys_graveId
                where j.sys_endDate >= monthStart && j.sys_endDate < monthEndExclusive
                      && (j.sys_jobStatus == "Pending" || j.sys_jobStatus == "In Progress")
                group g by new { g.sys_sectionId, g.sys_stageId } into grp
                select new
                {
                    grp.Key.sys_sectionId,
                    grp.Key.sys_stageId,
                    Count = grp.Select(x => x.sys_graveId).Distinct().Count()
                }
            ).ToListAsync();

            // Always show all sections (consistent X-axis)
            var sectionNames = sectionList.Select(s => s.sys_sectionName).ToList();
            StageMonthMatrixSectionLabelsJson = JsonSerializer.Serialize(sectionNames);

            // Build datasets per stage (consistent legend)
            var stageMatrix = stageList.ToDictionary(
                st => st.sys_stageName,
                st => sectionList.Select(sec =>
                    stageSectionCounts.FirstOrDefault(x =>
                        x.sys_sectionId == sec.sys_sectionId &&
                        x.sys_stageId == st.sys_stageId
                    )?.Count ?? 0
                ).ToList()
            );

            StageMonthMatrixDataJson = JsonSerializer.Serialize(stageMatrix);

            // =========================
            // CHART 4: Revenue Trend (Last 6 Months)
            // =========================
            var firstMonth = monthStart.AddMonths(-5); // includes current month => 6 months total
            var monthKeys = new List<(int Year, int Month)>();
            for (int i = 0; i < 6; i++)
            {
                var d = firstMonth.AddMonths(i);
                monthKeys.Add((d.Year, d.Month));
            }

            var revenueRaw = await _context.tbl_IncomePayments
                .Where(p => p.sys_date >= firstMonth && p.sys_date < monthEndExclusive)
                .GroupBy(p => new { p.sys_date.Year, p.sys_date.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Amount = g.Sum(x => x.sys_amount)
                })
                .ToListAsync();

            var revenueMap = revenueRaw.ToDictionary(x => (x.Year, x.Month), x => (decimal)x.Amount);

            var labels = monthKeys.Select(k => $"{k.Year}-{k.Month:00}").ToList();
            var values = monthKeys.Select(k =>
            {
                revenueMap.TryGetValue((k.Year, k.Month), out var amt);
                return amt;
            }).ToList();

            Revenue6MonthsLabelsJson = JsonSerializer.Serialize(labels);
            Revenue6MonthsValuesJson = JsonSerializer.Serialize(values);
        }
    }
}
