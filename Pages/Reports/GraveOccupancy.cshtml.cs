using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Reports
{
    public class GraveOccupancyModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GraveOccupancyModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Summary metrics
        public int TotalGraves { get; set; }
        public int OccupiedCount { get; set; }
        public int ReservedCount { get; set; }
        public int AvailableCount { get; set; }
        public int UnderConstructionCount { get; set; }

        // Detailed rows
        public List<GraveOccupancyRow> GraveRows { get; set; } = new();
        public List<UnderConstructionRow> UnderConstructionRows { get; set; } = new();

        // Chart data
        public OccupancySummaryDto OccupancySummary { get; set; } = new();
        public List<SectionOccupancyDto> SectionOccupancy { get; set; } = new();

        public async Task OnGetAsync()
        {

            var devStages = await _context.tbl_DevelopmentStages.AsNoTracking().ToListAsync();
            int maxStageOrder = devStages.Max(s => s.sys_stageOrder);


            var latestJobs =
    (await (
        from j in _context.tbl_Jobs
        join st in _context.tbl_DevelopmentStages on j.sys_stageId equals st.sys_stageId
        select new
        {
            j.sys_graveId,
            j.sys_jobStatus,
            st.sys_stageOrder
        }
    ).ToListAsync())
    .GroupBy(x => x.sys_graveId)
    .Select(grp =>
    {
        var last = grp.OrderByDescending(a => a.sys_stageOrder).First();
        return new
        {
            GraveId = grp.Key,
            StageOrder = last.sys_stageOrder,
            JobStatus = last.sys_jobStatus
        };
    })
    .ToDictionary(x => x.GraveId, x => x);



            var allocatedSet = await _context.tbl_GraveAllocations
    .Select(a => a.sys_graveId)
    .ToListAsync();

            var reservedSet = await _context.tbl_GraveReservations
                .Where(r => r.sys_graveId.HasValue)
                .Select(r => r.sys_graveId!.Value)
                .ToListAsync();


            var allGraves = await _context.tbl_Graves.AsNoTracking().ToListAsync();

            TotalGraves = allGraves.Count;

            OccupiedCount = allocatedSet.Count;

            ReservedCount = reservedSet
                .Where(id => !allocatedSet.Contains(id))
                .Count();

            UnderConstructionCount = latestJobs
                .Where(j => j.Value.StageOrder < maxStageOrder ||
                            j.Value.JobStatus != "Complete")
                .Count();

            AvailableCount = allGraves
                .Where(g =>
                    !allocatedSet.Contains(g.sys_graveId) &&
                    !reservedSet.Contains(g.sys_graveId) &&
                    latestJobs.ContainsKey(g.sys_graveId) &&
                    latestJobs[g.sys_graveId].StageOrder == maxStageOrder &&
                    latestJobs[g.sys_graveId].JobStatus == "Complete"
                )
                .Count();

            // ========= BASIC COUNTS & TABLE =========

            var gravesQuery =
                from g in _context.tbl_Graves
                join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                join a in _context.tbl_GraveAllocations on g.sys_graveId equals a.sys_graveId into ga
                from alloc in ga.DefaultIfEmpty()
                join r in _context.tbl_GraveReservations on g.sys_graveId equals r.sys_graveId into gr
                from res in gr.DefaultIfEmpty()
                join d in _context.tbl_Deceased on alloc.sys_deceasedId equals d.sys_deceasedId into gd
                from dec in gd.DefaultIfEmpty()
                join c in _context.tbl_Customers on alloc.sys_customerId equals c.sys_customerId into gc
                from cust in gc.DefaultIfEmpty()
                select new
                {
                    g.sys_graveId,
                    g.sys_plotNumber,
                    g.sys_graveSaleStatus,
                    SectionName = s.sys_sectionName,
                    Allocation = alloc,
                    Reservation = res,
                    DeceasedName = dec != null ? dec.sys_deceasedFullName : null,
                    CustomerName = cust != null ? cust.sys_paymentMadeBy : null
                };

            var graveData = await gravesQuery.AsNoTracking().ToListAsync();

            TotalGraves = graveData.Count;

            GraveRows = graveData.Select(x =>
            {
                string status;
                if (x.Allocation != null)
                    status = "Occupied";
                else if (x.Reservation != null)
                    status = "Reserved";
                else if (x.sys_graveSaleStatus == "Under Construction")
                    status = "Under Construction";
                else
                    status = "Available";

                return new GraveOccupancyRow
                {
                    GraveId = x.sys_graveId,
                    PlotNumber = x.sys_plotNumber,
                    SectionName = x.SectionName,
                    Status = status,
                    CustomerName = x.CustomerName,
                    DeceasedName = x.DeceasedName,
                    AllocationDate = x.Allocation?.sys_allocatedOn,
                    HasReservation = x.Reservation != null
                };
            }).ToList();

            OccupiedCount = GraveRows.Count(g => g.Status == "Occupied");
            ReservedCount = GraveRows.Count(g => g.Status == "Reserved");
            AvailableCount = GraveRows.Count(g => g.Status == "Available");

            // ========= UNDER-CONSTRUCTION =========



            var jobStageList =
                await (from j in _context.tbl_Jobs
                       join g in _context.tbl_Graves on j.sys_graveId equals g.sys_graveId
                       join s in _context.tbl_Sections on g.sys_sectionId equals s.sys_sectionId
                       join st in _context.tbl_DevelopmentStages on j.sys_stageId equals st.sys_stageId
                       select new
                       {
                           GraveId = g.sys_graveId,
                           g.sys_plotNumber,
                           SectionName = s.sys_sectionName,
                           j.sys_jobStatus,
                           j.sys_startDate,
                           j.sys_endDate,
                           StageName = st.sys_stageName,
                           StageOrder = st.sys_stageOrder
                       })
                      .AsNoTracking()
                      .ToListAsync();

            var underConstruction = jobStageList
                .GroupBy(x => new { x.GraveId, x.sys_plotNumber, x.SectionName })
                .Select(grp =>
                {
                    var latest = grp
                        .OrderByDescending(x => x.StageOrder)
                        .ThenByDescending(x => x.sys_endDate)
                        .First();

                    bool isFinalStageAndComplete = latest.StageOrder >= maxStageOrder &&
                                                   latest.sys_jobStatus == "Complete";

                    return new UnderConstructionRow
                    {
                        GraveId = grp.Key.GraveId,
                        PlotNumber = grp.Key.sys_plotNumber,
                        SectionName = grp.Key.SectionName,
                        CurrentStage = latest.StageName,
                        JobStatus = latest.sys_jobStatus,
                        StartDate = latest.sys_startDate,
                        EndDate = latest.sys_endDate,
                        IsCompletedAllStages = isFinalStageAndComplete
                    };
                })
                .Where(x => !x.IsCompletedAllStages)
                .ToList();

            UnderConstructionRows = underConstruction;
            UnderConstructionCount = UnderConstructionRows.Count;

            // ========= CHART DATA =========

            OccupancySummary = new OccupancySummaryDto
            {
                Occupied = OccupiedCount,
                Reserved = ReservedCount,
                Available = AvailableCount,
                UnderConstruction = UnderConstructionCount
            };

            var underConstructionBySection = await (
    from j in _context.tbl_Jobs
    join g in _context.tbl_Graves on j.sys_graveId equals g.sys_graveId
    where j.sys_jobStatus != "Complete"
    group g by g.sys_sectionId into grp
                select new
                {
                    SectionId = grp.Key,
                    Count = grp.Select(x => x.sys_graveId).Distinct().Count()
                }
                                ).ToListAsync();

            // ========= FINAL SECTION OCCUPANCY (OCCUPIED, RESERVED, AVAILABLE, UNDER CONSTRUCTION) =========

            var sectionRows = await (
                from s in _context.tbl_Sections
                join g in _context.tbl_Graves on s.sys_sectionId equals g.sys_sectionId
                select new
                {
                    s.sys_sectionId,
                    s.sys_sectionName,
                    g.sys_graveId
                }
            )
            .AsNoTracking()
            .ToListAsync();

            // Build final section DTOs
            SectionOccupancy = sectionRows
                .GroupBy(x => new { x.sys_sectionId, x.sys_sectionName })
                .Select(grp =>
                {
                    var graveIds = grp.Select(g => g.sys_graveId).ToList();

                    return new SectionOccupancyDto
                    {
                        SectionName = grp.Key.sys_sectionName,

                        // Already correct
                        Occupied = graveIds.Count(id => allocatedSet.Contains(id)),

                        Reserved = graveIds.Count(id =>
                            !allocatedSet.Contains(id) &&
                            reservedSet.Contains(id)
                        ),

                        Available = graveIds.Count(id =>
                            !allocatedSet.Contains(id) &&
                            !reservedSet.Contains(id) &&
                            latestJobs.ContainsKey(id) &&
                            latestJobs[id].StageOrder == maxStageOrder &&
                            latestJobs[id].JobStatus == "Complete"
                        ),

                        // FIXED: Under Construction per section
                        UnderConstruction = graveIds.Count(id =>
                            latestJobs.ContainsKey(id) &&
                            (
                                latestJobs[id].StageOrder < maxStageOrder ||
                                latestJobs[id].JobStatus != "Complete"
                            )
                        )
                    };
                })
                .OrderBy(s => s.SectionName)
                .ToList();




        }

        public class GraveOccupancyRow
        {
            public int GraveId { get; set; }
            public string PlotNumber { get; set; }
            public string SectionName { get; set; }
            public string Status { get; set; }
            public string CustomerName { get; set; }
            public string DeceasedName { get; set; }
            public DateOnly? AllocationDate { get; set; }
            public bool HasReservation { get; set; }
        }

        public class UnderConstructionRow
        {
            public int GraveId { get; set; }
            public string PlotNumber { get; set; }
            public string SectionName { get; set; }
            public string CurrentStage { get; set; }
            public string JobStatus { get; set; }
            public DateOnly StartDate { get; set; }
            public DateOnly EndDate { get; set; }
            public bool IsCompletedAllStages { get; set; }
        }

        public class OccupancySummaryDto
        {
            public int Occupied { get; set; }
            public int Reserved { get; set; }
            public int Available { get; set; }
            public int UnderConstruction { get; set; }
        }

        public class SectionOccupancyDto
        {
            public string SectionName { get; set; }
            public int Occupied { get; set; }

            public int Reserved { get; set; }
            public int Available { get; set; }

            public int UnderConstruction { get; set; }
        }
    }

}
