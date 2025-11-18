using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Pages.Graves;

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

        // Chart JSON
        public string SectionsJson { get; set; }
        public string GravesPerSectionJson { get; set; }

        public string StagesJson { get; set; }
        public string GravesPerStageJson { get; set; }

        public string RevenueMonthsJson { get; set; }
        public string RevenueValuesJson { get; set; }

        public string PaymentMethodNamesJson { get; set; }
        public string PaymentMethodTotalsJson { get; set; }

        public async Task OnGet()
        {
            // Get the maximum stage order value
            var maxStageOrder = await _context.tbl_DevelopmentStages
                .MaxAsync(s => s.sys_stageOrder);
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



                                        //Compute Top Build Status
                                        topBuildStatus = DateOnly.FromDateTime(DateTime.Now).DayNumber - o.sys_allocatedOn.DayNumber >= 365 * s.sys_reminderPeriod
                                            ? $"Ready for {s.sys_jobReminder}"
                                            : "N/A",

                                        // ? Automatically compute Tomb Status
                                        sys_graveTombStatus = DateOnly.FromDateTime(DateTime.Now).DayNumber - o.sys_allocatedOn.DayNumber >= 365
                                            ? "Ready for Tombstone"
                                            : al.sys_tombstoneStatus
                                    };


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




            UnderConstructionGraves = await underConstructionBaseQuery
                    .Where(g => g.sys_jobStatus == "In Progress" || g.sys_jobStatus == "Pending")
                    .CountAsync();

            // Load all graves by status
            AvailableGraves = await baseQuery.Where(g => g.sys_graveSaleStatus == "Unsold").CountAsync();
            ReservedGraves = await baseQuery.Where(g => g.sys_graveSaleStatus == "Reserved").CountAsync();
            AllocatedGraves = await occupiedBaseQuery.Where(g => g.sys_saleStatus == "Occupied").CountAsync();


            // Summary
            TotalGraves = await _context.tbl_Graves.CountAsync();
            
            TotalRevenue = await _context.tbl_IncomePayments.SumAsync(x => (decimal?)x.sys_amount) ?? 0;
            PaymentsMonth = await _context.tbl_IncomePayments
                .Where(x => x.sys_date.Month == DateTime.Now.Month)
                .SumAsync(x => (decimal?)x.sys_amount) ?? 0;

            RevenueMonth = PaymentsMonth;

            //Outstanding = await _context.tbl_Invoices
              ///  .SumAsync(x => (decimal?)(x.sys_invoiceTotal - x.sys_amountPaid)) ?? 0;

            // Graves per Section
            var sec = await _context.tbl_Sections
                .Select(s => new
                {
                    s.sys_sectionName,
                    Count = _context.tbl_Graves.Count(g => g.sys_sectionId == s.sys_sectionId)
                })
                .ToListAsync();

            SectionsJson = JsonSerializer.Serialize(sec.Select(x => x.sys_sectionName));
            GravesPerSectionJson = JsonSerializer.Serialize(sec.Select(x => x.Count));

            // Development Stage Chart
            var stages = await _context.tbl_DevelopmentStages
                .Select(s => new
                {
                    s.sys_stageName,
                    Count = _context.tbl_Graves.Count(g => g.sys_stageId == s.sys_stageId)
                })
                .ToListAsync();

            StagesJson = JsonSerializer.Serialize(stages.Select(x => x.sys_stageName));
            GravesPerStageJson = JsonSerializer.Serialize(stages.Select(x => x.Count));

            // Monthly Revenue (12 months)
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

            // Convert to "YYYY-MM" in memory
            var rev = revRaw
                .Select(x => new
                {
                    Month = $"{x.Year}-{x.Month:00}",
                    x.Amount
                })
                .ToList();

            RevenueMonthsJson = JsonSerializer.Serialize(rev.Select(x => x.Month));
            RevenueValuesJson = JsonSerializer.Serialize(rev.Select(x => x.Amount));


            // Payment Methods
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
        }
    }
}
