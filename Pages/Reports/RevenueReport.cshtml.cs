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
    public class RevenueReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly RevenueReportPdfService _pdfService;

        public RevenueReportModel(ApplicationDbContext context, RevenueReportPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Filters (read-only, filled from query)
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string SelectedCategory { get; set; } = "Total"; // Total | GraveSales | Reservations | Tombstones | Other

        // Summary totals for the selected period
        public double TotalRevenue { get; set; }
        public double GraveSalesRevenue { get; set; }
        public double ReservationsRevenue { get; set; }
        public double TombstoneRevenue { get; set; }
        public double OtherRevenue { get; set; }

        // Detailed rows for the currently selected category
        public List<RevenueRow> Rows { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Parse filters from query
            ParseFiltersFromQuery();

            await LoadDataAsync();
        }

        // GET /Reports/RevenueReport?handler=Pdf
        public async Task<IActionResult> OnGetPdfAsync()
        {
            ParseFiltersFromQuery();
            await LoadDataAsync();

            // Company metadata – adjust or pull from config
            string companyName = "Zororo Memorial Park";
            string companyAddress = "Harare, Zimbabwe";
            string companyPhone = "+263 000 000 000";
            string companyEmail = "info@zororo.co.zw";
            string logoPath = null; // set to a file path if you have one

            var pdfBytes = _pdfService.GenerateRevenueReportPdf(
                rows: Rows,
                fromDate: FromDate,
                toDate: ToDate,
                category: SelectedCategory,
                totalRevenue: TotalRevenue,
                graveSalesRevenue: GraveSalesRevenue,
                reservationsRevenue: ReservationsRevenue,
                tombstoneRevenue: TombstoneRevenue,
                otherRevenue: OtherRevenue,
                companyName: companyName,
                companyAddress: companyAddress,
                companyPhone: companyPhone,
                companyEmail: companyEmail,
                logoPath: logoPath
            );

            string filename = $"RevenueReport_{SelectedCategory}_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }

        private void ParseFiltersFromQuery()
        {
            // Category
            var category = Request.Query["category"].ToString();
            if (!string.IsNullOrWhiteSpace(category))
                SelectedCategory = category;
            else
                SelectedCategory = "Total";

            // From date
            var fromStr = Request.Query["from"].ToString();
            if (DateOnly.TryParse(fromStr, out var from))
                FromDate = from;
            else
                FromDate = null;

            // To date
            var toStr = Request.Query["to"].ToString();
            if (DateOnly.TryParse(toStr, out var to))
                ToDate = to;
            else
                ToDate = null;

            // Default: current month if no dates provided
            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var now = DateTime.Today;
                FromDate = new DateOnly(now.Year, now.Month, 1);
                ToDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            }
        }

        private async Task LoadDataAsync()
        {
            // Base income payments
            var incomeQuery = _context.tbl_IncomePayments.AsQueryable();
            if (FromDate.HasValue)
                incomeQuery = incomeQuery.Where(p => p.sys_date >= FromDate.Value);
            if (ToDate.HasValue)
                incomeQuery = incomeQuery.Where(p => p.sys_date <= ToDate.Value);

            var incomeList = await incomeQuery.AsNoTracking().ToListAsync();

            // Tombstone payments
            var tombstoneQuery = _context.tbl_TombstoneSales.AsQueryable();
            if (FromDate.HasValue)
                tombstoneQuery = tombstoneQuery.Where(t => t.sys_saleDate >= FromDate.Value);
            if (ToDate.HasValue)
                tombstoneQuery = tombstoneQuery.Where(t => t.sys_saleDate <= ToDate.Value);

            var tombstoneList = await tombstoneQuery.AsNoTracking().ToListAsync();

            // Mapping invoice IDs
            var allocationInvoiceIds = new HashSet<int>(
                await _context.tbl_GraveAllocationInvoices
                    .Select(x => x.sys_invoiceId)
                    .ToListAsync()
            );

            var reservationInvoiceIds = new HashSet<int>(
                await _context.tbl_GraveReservationInvoices
                    .Select(x => x.sys_invoiceId)
                    .ToListAsync()
            );

            var tombstoneInvoiceIds = new HashSet<int>(tombstoneList.Select(t => t.sys_invoiceId));

            // Optional invoice numbers for display
            var invoiceNumbers = await _context.tbl_Invoices
                .AsNoTracking()
                .ToDictionaryAsync(i => i.sys_invoiceId, i => i.sys_invoiceId);

            // Split income into categories
            var graveSalesPayments = incomeList
                .Where(p => allocationInvoiceIds.Contains(p.sys_invoiceId))
                .ToList();

            var reservationPayments = incomeList
                .Where(p => reservationInvoiceIds.Contains(p.sys_invoiceId))
                .ToList();

            var otherPayments = incomeList
                .Where(p =>
                    !allocationInvoiceIds.Contains(p.sys_invoiceId) &&
                    !reservationInvoiceIds.Contains(p.sys_invoiceId) &&
                    !tombstoneInvoiceIds.Contains(p.sys_invoiceId)
                )
                .ToList();

            // Totals
            GraveSalesRevenue = graveSalesPayments.Sum(p => p.sys_amount);
            ReservationsRevenue = reservationPayments.Sum(p => p.sys_amount);
            TombstoneRevenue = tombstoneList.Sum(t => t.sys_amountPaid);
            OtherRevenue = otherPayments.Sum(p => p.sys_amount);
            TotalRevenue = GraveSalesRevenue + ReservationsRevenue + TombstoneRevenue + OtherRevenue;

            // Build detailed rows based on selected category
            var rows = new List<RevenueRow>();

            if (SelectedCategory == "GraveSales" || SelectedCategory == "Total")
            {
                rows.AddRange(graveSalesPayments.Select(p => new RevenueRow
                {
                    Date = p.sys_date,
                    Amount = p.sys_amount,
                    Type = "Grave Sale",
                    InvoiceId = p.sys_invoiceId,
                    //InvoiceNumber = invoiceNumbers.TryGetValue(p.sys_invoiceId, out var nr) ? nr : "",
                    Notes = $"Receipt #{p.sys_receiptNumber} - {p.sys_notes}"
                }));
            }

            if (SelectedCategory == "Reservations" || SelectedCategory == "Total")
            {
                rows.AddRange(reservationPayments.Select(p => new RevenueRow
                {
                    Date = p.sys_date,
                    Amount = p.sys_amount,
                    Type = "Grave Reservation",
                    InvoiceId = p.sys_invoiceId,
                   // InvoiceNumber = invoiceNumbers.TryGetValue(p.sys_invoiceId, out var nr) ? nr : "",
                    Notes = $"Receipt #{p.sys_receiptNumber} - {p.sys_notes}"
                }));
            }

            if (SelectedCategory == "Tombstones" || SelectedCategory == "Total")
            {
                rows.AddRange(tombstoneList.Select(t => new RevenueRow
                {
                    Date = t.sys_saleDate,
                    Amount = t.sys_amountPaid,
                    Type = "Tombstone Sale",
                    InvoiceId = t.sys_invoiceId,
                   // InvoiceNumber = invoiceNumbers.TryGetValue(t.sys_invoiceId, out var nr) ? nr : "",
                    Notes = $"Buyer: {t.sys_buyerName} ({t.sys_buyerContact})"
                }));
            }

            if (SelectedCategory == "Other" || SelectedCategory == "Total")
            {
                rows.AddRange(otherPayments.Select(p => new RevenueRow
                {
                    Date = p.sys_date,
                    Amount = p.sys_amount,
                    Type = "Other Revenue",
                    InvoiceId = p.sys_invoiceId,
                   // InvoiceNumber = invoiceNumbers.TryGetValue(p.sys_invoiceId, out var nr) ? nr : "",
                    Notes = $"Receipt #{p.sys_receiptNumber} - {p.sys_notes}"
                }));
            }

            // If user selected a specific category (not Total), filter rows strictly
            if (SelectedCategory != "Total")
            {
                rows = rows.Where(r => r.Type switch
                {
                    "Grave Sale" => SelectedCategory == "GraveSales",
                    "Grave Reservation" => SelectedCategory == "Reservations",
                    "Tombstone Sale" => SelectedCategory == "Tombstones",
                    "Other Revenue" => SelectedCategory == "Other",
                    _ => false
                }).ToList();
            }

            Rows = rows
                .OrderBy(r => r.Date)
                .ThenBy(r => r.InvoiceNumber)
                .ToList();
        }
    }

    public class RevenueRow
    {
        public DateOnly Date { get; set; }
        public double Amount { get; set; }
        public string Type { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; }
        public string Notes { get; set; }
    }
}
