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
    public class OutstandingBalancesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly OutstandingBalancesPdfService _pdfService;

        public OutstandingBalancesModel(ApplicationDbContext context, OutstandingBalancesPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Filters
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string SelectedCategory { get; set; } = "All"; // All | GraveSales | Reservations | Tombstones | Other

        // Summary
        public double TotalInvoiced { get; set; }
        public double TotalPaid { get; set; }
        public double TotalOutstanding { get; set; }

        // Detailed rows
        public List<OutstandingRow> Rows { get; set; } = new();

        // For charts
        public double GraveSalesOutstanding { get; set; }
        public double ReservationsOutstanding { get; set; }
        public double TombstonesOutstanding { get; set; }
        public double OtherOutstanding { get; set; }

        public async Task OnGetAsync()
        {
            ParseFiltersFromQuery();
            await LoadDataAsync();
        }

        // GET /Reports/OutstandingBalances?handler=Pdf
        public async Task<IActionResult> OnGetPdfAsync()
        {
            ParseFiltersFromQuery();
            await LoadDataAsync();

            // Company metadata – adjust as needed or pull from config
            string companyName = "Zororo Memorial Park";
            string companyAddress = "Harare, Zimbabwe";
            string companyPhone = "+263 000 000 000";
            string companyEmail = "info@zororo.co.zw";
            string logoPath = null; // set to physical path if you have a logo file

            var pdfBytes = _pdfService.GenerateOutstandingBalancesPdf(
                rows: Rows,
                fromDate: FromDate,
                toDate: ToDate,
                selectedCategoryKey: SelectedCategory,
                totalInvoiced: TotalInvoiced,
                totalPaid: TotalPaid,
                totalOutstanding: TotalOutstanding,
                graveSalesOutstanding: GraveSalesOutstanding,
                reservationsOutstanding: ReservationsOutstanding,
                tombstonesOutstanding: TombstonesOutstanding,
                otherOutstanding: OtherOutstanding,
                companyName: companyName,
                companyAddress: companyAddress,
                companyPhone: companyPhone,
                companyEmail: companyEmail,
                logoPath: logoPath
            );

            string filename = $"OutstandingBalances_{SelectedCategory}_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }

        private void ParseFiltersFromQuery()
        {
            // Category
            var category = Request.Query["category"].ToString();
            if (!string.IsNullOrWhiteSpace(category))
                SelectedCategory = category;
            else
                SelectedCategory = "All";

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

            // Default to current month if nothing provided
            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var now = DateTime.Today;
                FromDate = new DateOnly(now.Year, now.Month, 1);
                ToDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            }
        }

        private async Task LoadDataAsync()
        {
            // 1) Pull invoices in date range
            var invQuery = _context.tbl_Invoices.AsQueryable();

            if (FromDate.HasValue)
                invQuery = invQuery.Where(i => i.sys_invoiceDate >= FromDate.Value);
            if (ToDate.HasValue)
                invQuery = invQuery.Where(i => i.sys_invoiceDate <= ToDate.Value);

            var invoices = await invQuery.AsNoTracking().ToListAsync();
            if (!invoices.Any())
            {
                Rows = new List<OutstandingRow>();
                TotalInvoiced = TotalPaid = TotalOutstanding = 0;
                return;
            }

            var invoiceIds = invoices.Select(i => i.sys_invoiceId).ToList();

            // 2) Totals per invoice from items
            var invoiceTotals = await _context.tbl_InvoiceItems
                .Where(it => invoiceIds.Contains(it.sys_invoiceId))
                .GroupBy(it => it.sys_invoiceId)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    Total = g.Sum(x => x.sys_itemCost)
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Total);

            // 3) Payments per invoice
            var invoicePayments = await _context.tbl_IncomePayments
                .Where(p => invoiceIds.Contains(p.sys_invoiceId))
                .GroupBy(p => p.sys_invoiceId)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    Paid = g.Sum(x => x.sys_amount)
                })
                .ToDictionaryAsync(x => x.InvoiceId, x => x.Paid);

            // 4) Category mappings
            var allocInvoiceIds = new HashSet<int>(
                await _context.tbl_GraveAllocationInvoices
                    .Where(x => invoiceIds.Contains(x.sys_invoiceId))
                    .Select(x => x.sys_invoiceId)
                    .Distinct()
                    .ToListAsync()
            );

            var reservationInvoiceIds = new HashSet<int>(
                await _context.tbl_GraveReservationInvoices
                    .Where(x => invoiceIds.Contains(x.sys_invoiceId))
                    .Select(x => x.sys_invoiceId)
                    .Distinct()
                    .ToListAsync()
            );

            var tombstoneInvoiceIds = new HashSet<int>(
                await _context.tbl_TombstoneSales
                    .Where(t => invoiceIds.Contains(t.sys_invoiceId))
                    .Select(t => t.sys_invoiceId)
                    .Distinct()
                    .ToListAsync()
            );

            // 5) Customers
            var customerDict = await _context.tbl_Customers
                .AsNoTracking()
                .ToDictionaryAsync(c => c.sys_customerId, c => new
                {
                    c.sys_paymentMadeBy,
                    c.sys_customerPhone
                });

            // 6) Build rows
            var rows = new List<OutstandingRow>();

            foreach (var inv in invoices)
            {
                // compute totals
                double total = invoiceTotals.TryGetValue(inv.sys_invoiceId, out var t) ? t : 0;
                double paid = invoicePayments.TryGetValue(inv.sys_invoiceId, out var p) ? p : 0;
                double outstanding = total - paid;

                // nothing invoiced and nothing outstanding? skip
                if (total <= 0 && outstanding <= 0)
                    continue;

                // derive category key
                string categoryKey;
                if (allocInvoiceIds.Contains(inv.sys_invoiceId))
                    categoryKey = "GraveSales";
                else if (reservationInvoiceIds.Contains(inv.sys_invoiceId))
                    categoryKey = "Reservations";
                else if (tombstoneInvoiceIds.Contains(inv.sys_invoiceId))
                    categoryKey = "Tombstones";
                else
                    categoryKey = "Other";

                // filter by selected category
                if (SelectedCategory != "All" && categoryKey != SelectedCategory)
                    continue;

                // only show truly outstanding invoices
                if (outstanding <= 0)
                    continue;

                // map category label
                string categoryLabel = categoryKey switch
                {
                    "GraveSales" => "Grave Sale",
                    "Reservations" => "Grave Reservation",
                    "Tombstones" => "Tombstone Sale",
                    "Other" => "Other Revenue",
                    _ => "Other"
                };

                // customer info
                string clientName = "N/A";
                string clientPhone = "N/A";
                if (customerDict.TryGetValue(inv.sys_customerId, out var cust))
                {
                    clientName = string.IsNullOrWhiteSpace(cust.sys_paymentMadeBy)
                        ? "N/A"
                        : cust.sys_paymentMadeBy;
                    clientPhone = string.IsNullOrWhiteSpace(cust.sys_customerPhone)
                        ? "N/A"
                        : cust.sys_customerPhone;
                }

                rows.Add(new OutstandingRow
                {
                    InvoiceId = inv.sys_invoiceId,
                    Date = inv.sys_invoiceDate,
                    CategoryKey = categoryKey,
                    CategoryLabel = categoryLabel,
                    ClientName = clientName,
                    ClientPhone = clientPhone,
                    Total = total,
                    Paid = paid,
                    Outstanding = outstanding
                });
            }

            Rows = rows
                .OrderByDescending(r => r.Outstanding)
                .ThenBy(r => r.Date)
                .ToList();

            // 7) Summary numbers are based on rows currently shown
            TotalInvoiced = Rows.Sum(r => r.Total);
            TotalPaid = Rows.Sum(r => r.Paid);
            TotalOutstanding = Rows.Sum(r => r.Outstanding);

            // 8) Category breakdown for charts
            GraveSalesOutstanding = Rows.Where(r => r.CategoryKey == "GraveSales").Sum(r => r.Outstanding);
            ReservationsOutstanding = Rows.Where(r => r.CategoryKey == "Reservations").Sum(r => r.Outstanding);
            TombstonesOutstanding = Rows.Where(r => r.CategoryKey == "Tombstones").Sum(r => r.Outstanding);
            OtherOutstanding = Rows.Where(r => r.CategoryKey == "Other").Sum(r => r.Outstanding);
        }
    }

    public class OutstandingRow
    {
        public int InvoiceId { get; set; }
        public DateOnly Date { get; set; }

        // category key for logic (GraveSales / Reservations / Tombstones / Other)
        public string CategoryKey { get; set; }
        // friendly label for UI / PDF
        public string CategoryLabel { get; set; }

        public string ClientName { get; set; }
        public string ClientPhone { get; set; }

        public double Total { get; set; }
        public double Paid { get; set; }
        public double Outstanding { get; set; }
    }
}
