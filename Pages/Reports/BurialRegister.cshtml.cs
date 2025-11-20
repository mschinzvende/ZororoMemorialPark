using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Reports
{
    public class BurialRegisterModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly BurialRegisterPdfService _pdfService;

        public BurialRegisterModel(ApplicationDbContext context, BurialRegisterPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Filters (for display back into the form)
        [BindProperty(SupportsGet = true)]
        public DateOnly? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SectionId { get; set; }

        // Data for dropdown
        public List<tbl_Sections> Sections { get; set; } = new();

        // Report rows
        public List<BurialRegisterRow> Rows { get; set; } = new();

        // Summary
        public int TotalBurials => Rows.Count;

        public async Task OnGetAsync()
        {
            Sections = await _context.tbl_Sections
                .OrderBy(s => s.sys_sectionName)
                .AsNoTracking()
                .ToListAsync();

            // Build base query
            var query =
                from alloc in _context.tbl_GraveAllocations
                join grave in _context.tbl_Graves on alloc.sys_graveId equals grave.sys_graveId
                join section in _context.tbl_Sections on grave.sys_sectionId equals section.sys_sectionId
                join dec in _context.tbl_Deceased on alloc.sys_deceasedId equals dec.sys_deceasedId
                join cust in _context.tbl_Customers on alloc.sys_customerId equals cust.sys_customerId
                select new
                {
                    alloc.sys_allocationId,
                    alloc.sys_dateTimeOfBurial,
                    alloc.sys_burialOrder,
                    alloc.sys_repatriationSource,
                    GraveId = grave.sys_graveId,
                    grave.sys_plotNumber,
                    SectionName = section.sys_sectionName,
                    DeceasedName = dec.sys_deceasedFullName,
                    dec.sys_deceasedDOB,
                    dec.sys_deceasedDOD,
                    dec.sys_deceasedGender,
                    dec.sys_deceasedNationality,
                    CustomerName = cust.sys_paymentMadeBy,
                    CustomerPhone = cust.sys_customerPhone
                };

            // Apply filters
            if (FromDate.HasValue)
            {
                var fromDt = FromDate.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => x.sys_dateTimeOfBurial >= fromDt);
            }

            if (ToDate.HasValue)
            {
                var toDt = ToDate.Value.ToDateTime(new TimeOnly(23, 59, 59));
                query = query.Where(x => x.sys_dateTimeOfBurial <= toDt);
            }

            if (SectionId.HasValue && SectionId.Value > 0)
            {
                query = query.Where(x => x.SectionName != null && x.SectionName != "" &&
                                         _context.tbl_Sections
                                             .Any(s => s.sys_sectionId == SectionId &&
                                                       s.sys_sectionName == x.SectionName));
            }

            var data = await query
                .OrderBy(x => x.sys_dateTimeOfBurial)
                .ThenBy(x => x.sys_plotNumber)
                .AsNoTracking()
                .ToListAsync();

            Rows = data.Select(x =>
            {
                int? age = null;
                if (x.sys_deceasedDOB != default && x.sys_deceasedDOD != default)
                {
                    age = x.sys_deceasedDOD.Year - x.sys_deceasedDOB.Year;
                    if (x.sys_deceasedDOD < x.sys_deceasedDOB.AddYears(age.Value))
                        age--;
                }

                return new BurialRegisterRow
                {
                    BurialDateTime = x.sys_dateTimeOfBurial,
                    SectionName = x.SectionName,
                    PlotNumber = x.sys_plotNumber,
                    DeceasedName = x.DeceasedName,
                    DeceasedGender = x.sys_deceasedGender,
                    DeceasedNationality = x.sys_deceasedNationality,
                    AgeAtDeath = age,
                    CustomerName = x.CustomerName,
                    CustomerPhone = x.CustomerPhone,
                    RepatriationSource = x.sys_repatriationSource,
                    BurialOrder = x.sys_burialOrder
                };
            }).ToList();
        }

        public async Task<IActionResult> OnGetPdfAsync(DateOnly? fromDate, DateOnly? toDate, int? sectionId)
        {
            // Rebuild the same dataset as OnGetAsync, but without setting properties
            var query =
                from alloc in _context.tbl_GraveAllocations
                join grave in _context.tbl_Graves on alloc.sys_graveId equals grave.sys_graveId
                join section in _context.tbl_Sections on grave.sys_sectionId equals section.sys_sectionId
                join dec in _context.tbl_Deceased on alloc.sys_deceasedId equals dec.sys_deceasedId
                join cust in _context.tbl_Customers on alloc.sys_customerId equals cust.sys_customerId
                select new
                {
                    alloc.sys_allocationId,
                    alloc.sys_dateTimeOfBurial,
                    alloc.sys_burialOrder,
                    alloc.sys_repatriationSource,
                    GraveId = grave.sys_graveId,
                    grave.sys_plotNumber,
                    SectionName = section.sys_sectionName,
                    DeceasedName = dec.sys_deceasedFullName,
                    dec.sys_deceasedDOB,
                    dec.sys_deceasedDOD,
                    dec.sys_deceasedGender,
                    dec.sys_deceasedNationality,
                    CustomerName = cust.sys_paymentMadeBy,
                    CustomerPhone = cust.sys_customerPhone
                };

            if (fromDate.HasValue)
            {
                var fromDt = fromDate.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => x.sys_dateTimeOfBurial >= fromDt);
            }

            if (toDate.HasValue)
            {
                var toDt = toDate.Value.ToDateTime(new TimeOnly(23, 59, 59));
                query = query.Where(x => x.sys_dateTimeOfBurial <= toDt);
            }

            if (sectionId.HasValue && sectionId.Value > 0)
            {
                var sectionName = await _context.tbl_Sections
                    .Where(s => s.sys_sectionId == sectionId.Value)
                    .Select(s => s.sys_sectionName)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(sectionName))
                {
                    query = query.Where(x => x.SectionName == sectionName);
                }
            }

            var data = await query
                .OrderBy(x => x.sys_dateTimeOfBurial)
                .ThenBy(x => x.sys_plotNumber)
                .AsNoTracking()
                .ToListAsync();

            var rows = data.Select(x =>
            {
                int? age = null;
                if (x.sys_deceasedDOB != default && x.sys_deceasedDOD != default)
                {
                    age = x.sys_deceasedDOD.Year - x.sys_deceasedDOB.Year;
                    if (x.sys_deceasedDOD < x.sys_deceasedDOB.AddYears(age.Value))
                        age--;
                }

                return new BurialRegisterRow
                {
                    BurialDateTime = x.sys_dateTimeOfBurial,
                    SectionName = x.SectionName,
                    PlotNumber = x.sys_plotNumber,
                    DeceasedName = x.DeceasedName,
                    DeceasedGender = x.sys_deceasedGender,
                    DeceasedNationality = x.sys_deceasedNationality,
                    AgeAtDeath = age,
                    CustomerName = x.CustomerName,
                    CustomerPhone = x.CustomerPhone,
                    RepatriationSource = x.sys_repatriationSource,
                    BurialOrder = x.sys_burialOrder
                };
            }).ToList();

            // Company info – you can later pull this from settings table
            var companyName = "Zororo Memorial Park";
            var companyAddress = "Address Line 1, Harare, Zimbabwe";
            var companyPhone = "+263 000 000 000";
            var companyEmail = "info@zororo.co.zw";
            string logoPath = null; // e.g. Path.Combine(_env.WebRootPath, "images", "logo.png");

            var pdfBytes = _pdfService.GenerateBurialRegisterPdf(
                rows,
                companyName,
                companyAddress,
                companyPhone,
                companyEmail,
                logoPath,
                fromDate,
                toDate,
                sectionId.HasValue
                    ? await _context.tbl_Sections.Where(s => s.sys_sectionId == sectionId.Value)
                        .Select(s => s.sys_sectionName).FirstOrDefaultAsync()
                    : null
            );

            var fileName = $"BurialRegister_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }

    public class BurialRegisterRow
    {
        public DateTime BurialDateTime { get; set; }
        public string SectionName { get; set; }
        public string PlotNumber { get; set; }

        public string DeceasedName { get; set; }
        public string DeceasedGender { get; set; }
        public string DeceasedNationality { get; set; }
        public int? AgeAtDeath { get; set; }

        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        public string RepatriationSource { get; set; }
        public string BurialOrder { get; set; }
    }
}
