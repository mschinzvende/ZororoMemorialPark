using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Reports
{
    public class SupplierPaymentsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly SupplierPaymentsPdfService _pdfService;

        public SupplierPaymentsModel(ApplicationDbContext context, SupplierPaymentsPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Filters (read-only, filled from query)
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int? SelectedSupplierId { get; set; }
        public int? SelectedStageId { get; set; }
        public int? SelectedSectionId { get; set; }

        // Dropdown data
        public List<tbl_Suppliers> SupplierList { get; set; } = new();
        public List<tbl_DevelopmentStages> StageList { get; set; } = new();
        public List<tbl_Sections> SectionList { get; set; } = new();

        // Summary
        public double TotalAmountDue { get; set; }
        public double TotalAmountPaid { get; set; }
        public double TotalBalance { get; set; }

        // Rows for table
        public List<SupplierPaymentRow> Rows { get; set; } = new();

        // Chart data
        public List<string> SupplierChartLabels { get; set; } = new();
        public List<double> SupplierChartPaid { get; set; } = new();

        public List<string> StageChartLabels { get; set; } = new();
        public List<double> StageChartPaid { get; set; } = new();

        public List<string> SectionChartLabels { get; set; } = new();
        public List<double> SectionChartPaid { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadLookupsAsync();
            ParseFiltersFromQuery();
            await LoadDataAsync();
        }

        // GET /Reports/SupplierPayments?handler=Pdf
        public async Task<IActionResult> OnGetPdfAsync()
        {
            await LoadLookupsAsync();
            ParseFiltersFromQuery();
            await LoadDataAsync();

            string companyName = "Zororo Memorial Park";
            string companyAddress = "Harare, Zimbabwe";
            string companyPhone = "+263 000 000 000";
            string companyEmail = "info@zororo.co.zw";
            string logoPath = null; // set a logo path if you have one

            var pdfBytes = _pdfService.GenerateSupplierPaymentsPdf(
                rows: Rows,
                fromDate: FromDate,
                toDate: ToDate,
                selectedSupplierId: SelectedSupplierId,
                selectedStageId: SelectedStageId,
                selectedSectionId: SelectedSectionId,
                totalDue: TotalAmountDue,
                totalPaid: TotalAmountPaid,
                totalBalance: TotalBalance,
                companyName: companyName,
                companyAddress: companyAddress,
                companyPhone: companyPhone,
                companyEmail: companyEmail,
                logoPath: logoPath,
                supplierLookup: SupplierList,
                stageLookup: StageList,
                sectionLookup: SectionList
            );

            string filename = $"SupplierPayments_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }

        private async Task LoadLookupsAsync()
        {
            SupplierList = await _context.tbl_Suppliers
                .AsNoTracking()
                .OrderBy(s => s.sys_supplierName)
                .ToListAsync();

            StageList = await _context.tbl_DevelopmentStages
                .AsNoTracking()
                .OrderBy(s => s.sys_stageOrder)
                .ToListAsync();

            SectionList = await _context.tbl_Sections
                .AsNoTracking()
                .OrderBy(s => s.sys_sectionName)
                .ToListAsync();
        }

        private void ParseFiltersFromQuery()
        {
            // Dates
            var fromStr = Request.Query["from"].ToString();
            var toStr = Request.Query["to"].ToString();

            if (DateOnly.TryParse(fromStr, out var from))
                FromDate = from;
            if (DateOnly.TryParse(toStr, out var to))
                ToDate = to;

            // Default: current month
            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var today = DateTime.Today;
                FromDate = new DateOnly(today.Year, today.Month, 1);
                ToDate = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
            }

            // Supplier
            var supStr = Request.Query["supplierId"].ToString();
            if (int.TryParse(supStr, out var supId) && supId > 0)
                SelectedSupplierId = supId;
            else
                SelectedSupplierId = null;

            // Stage
            var stageStr = Request.Query["stageId"].ToString();
            if (int.TryParse(stageStr, out var stageId) && stageId > 0)
                SelectedStageId = stageId;
            else
                SelectedStageId = null;

            // Section
            var sectionStr = Request.Query["sectionId"].ToString();
            if (int.TryParse(sectionStr, out var secId) && secId > 0)
                SelectedSectionId = secId;
            else
                SelectedSectionId = null;
        }

        private async Task LoadDataAsync()
        {
            // Base query: Expenses joined to Jobs, Suppliers, Stages, Graves, Sections
            var query =
                from e in _context.tbl_Expenses
                where e.sys_supplierId.HasValue && e.sys_jobId.HasValue
                join sup in _context.tbl_Suppliers on e.sys_supplierId.Value equals sup.sys_supplierId
                join j in _context.tbl_Jobs on e.sys_jobId.Value equals j.sys_jobId
                join st in _context.tbl_DevelopmentStages on j.sys_stageId equals st.sys_stageId
                join g in _context.tbl_Graves on j.sys_graveId equals g.sys_graveId
                join sec in _context.tbl_Sections on g.sys_sectionId equals sec.sys_sectionId
                select new
                {
                    Expense = e,
                    Supplier = sup,
                    Job = j,
                    Stage = st,
                    Grave = g,
                    Section = sec
                };

            // Date filters
            if (FromDate.HasValue)
            {
                var from = FromDate.Value;
                query = query.Where(x => x.Expense.sys_date >= from);
            }

            if (ToDate.HasValue)
            {
                var to = ToDate.Value;
                query = query.Where(x => x.Expense.sys_date <= to);
            }

            // Supplier filter
            if (SelectedSupplierId.HasValue)
            {
                int id = SelectedSupplierId.Value;
                query = query.Where(x => x.Supplier.sys_supplierId == id);
            }

            // Stage filter
            if (SelectedStageId.HasValue)
            {
                int id = SelectedStageId.Value;
                query = query.Where(x => x.Stage.sys_stageId == id);
            }

            // Section filter (from tbl_Graves.sys_sectionId)
            if (SelectedSectionId.HasValue)
            {
                int id = SelectedSectionId.Value;
                query = query.Where(x => x.Grave.sys_sectionId == id);
            }

            var data = await query
                .AsNoTracking()
                .OrderBy(x => x.Expense.sys_date)
                .ThenBy(x => x.Supplier.sys_supplierName)
                .ToListAsync();

            Rows = data.Select(x => new SupplierPaymentRow
            {
                ExpenseId = x.Expense.sys_expenseId,
                Date = x.Expense.sys_date,
                SupplierId = x.Supplier.sys_supplierId,
                SupplierName = x.Supplier.sys_supplierName,
                JobId = x.Job.sys_jobId,
                StageId = x.Stage.sys_stageId,
                StageName = x.Stage.sys_stageName,
                GraveId = x.Grave.sys_graveId,
                SectionId = x.Section.sys_sectionId,
                SectionName = x.Section.sys_sectionName,
                PlotNumber = x.Grave.sys_plotNumber,
                Notes = x.Expense.sys_notes,
                AmountDue = x.Expense.sys_amountDue,
                AmountPaid = x.Expense.sys_amountPaid,
                Balance = x.Expense.sys_balanceDue
            }).ToList();

            TotalAmountDue = Rows.Sum(r => r.AmountDue);
            TotalAmountPaid = Rows.Sum(r => r.AmountPaid);
            TotalBalance = Rows.Sum(r => r.Balance);

            // Chart: Paid per supplier
            SupplierChartLabels = Rows
                .GroupBy(r => r.SupplierName)
                .OrderByDescending(g => g.Sum(x => x.AmountPaid))
                .Select(g => g.Key)
                .ToList();

            SupplierChartPaid = Rows
                .GroupBy(r => r.SupplierName)
                .OrderByDescending(g => g.Sum(x => x.AmountPaid))
                .Select(g => g.Sum(x => x.AmountPaid))
                .ToList();

            // Chart: Paid per stage
            StageChartLabels = Rows
                .GroupBy(r => r.StageName)
                .OrderByDescending(g => g.Sum(x => x.AmountPaid))
                .Select(g => g.Key)
                .ToList();

            StageChartPaid = Rows
                .GroupBy(r => r.StageName)
                .OrderByDescending(g => g.Sum(x => x.AmountPaid))
                .Select(g => g.Sum(x => x.AmountPaid))
                .ToList();

            // Chart: Paid per section
            SectionChartLabels = Rows
                .GroupBy(r => r.SectionName)
                .OrderBy(g => g.Key)
                .Select(g => g.Key)
                .ToList();

            SectionChartPaid = Rows
                .GroupBy(r => r.SectionName)
                .OrderBy(g => g.Key)
                .Select(g => g.Sum(x => x.AmountPaid))
                .ToList();
        }
    }

    public class SupplierPaymentRow
    {
        public int ExpenseId { get; set; }
        public DateOnly Date { get; set; }

        public int SupplierId { get; set; }
        public string SupplierName { get; set; }

        public int JobId { get; set; }
        public int StageId { get; set; }
        public string StageName { get; set; }

        public int GraveId { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public string PlotNumber { get; set; }

        public string Notes { get; set; }
        public double AmountDue { get; set; }
        public double AmountPaid { get; set; }
        public double Balance { get; set; }
    }
}
