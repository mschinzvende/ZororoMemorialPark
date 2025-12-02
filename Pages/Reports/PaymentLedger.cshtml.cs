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
    public class PaymentLedgerModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly PaymentLedgerPdfService _pdfService;

        public PaymentLedgerModel(ApplicationDbContext context, PaymentLedgerPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Filters (read-only; filled from query string)
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string SelectedCategory { get; set; } = "All"; // All | GraveSales | Reservations | Tombstones | Other
        public int? SelectedPaymentMethodId { get; set; }

        // Payment methods for dropdown
        public List<tbl_PaymentMethods> PaymentMethods { get; set; } = new();

        // Summary totals for current filter
        public double TotalAmount { get; set; }
        public double GraveSalesAmount { get; set; }
        public double ReservationsAmount { get; set; }
        public double TombstoneAmount { get; set; }
        public double OtherAmount { get; set; }

        // Detailed rows
        public List<PaymentLedgerRow> Rows { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadFiltersAsync();
            await LoadDataAsync();
        }

        // GET /Reports/PaymentLedger?handler=Pdf
        public async Task<IActionResult> OnGetPdfAsync()
        {
            await LoadFiltersAsync();
            await LoadDataAsync();

            string companyName = "Zororo Memorial Park";
            string companyAddress = "Harare, Zimbabwe";
            string companyPhone = "+263 000 000 000";
            string companyEmail = "info@zororo.co.zw";
            string logoPath = null; // set to a physical path if you have one

            var pdfBytes = _pdfService.GeneratePaymentLedgerPdf(
                rows: Rows,
                fromDate: FromDate,
                toDate: ToDate,
                category: SelectedCategory,
                totalAmount: TotalAmount,
                graveSalesAmount: GraveSalesAmount,
                reservationsAmount: ReservationsAmount,
                tombstoneAmount: TombstoneAmount,
                otherAmount: OtherAmount,
                companyName: companyName,
                companyAddress: companyAddress,
                companyPhone: companyPhone,
                companyEmail: companyEmail,
                logoPath: logoPath
            );

            string filename = $"PaymentLedger_{SelectedCategory}_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }

        private async Task LoadFiltersAsync()
        {
            // Payment methods for dropdown
            PaymentMethods = await _context.tbl_PaymentMethods
                .AsNoTracking()
                .OrderBy(p => p.sys_paymentMethodName)
                .ToListAsync();

            // Category
            var category = Request.Query["category"].ToString();
            if (!string.IsNullOrWhiteSpace(category))
                SelectedCategory = category;
            else
                SelectedCategory = "All";

            // Payment Method
            var pmStr = Request.Query["paymentMethodId"].ToString();
            if (int.TryParse(pmStr, out var pmId))
                SelectedPaymentMethodId = pmId;
            else
                SelectedPaymentMethodId = null;

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

            // Default to current month if no dates provided
            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var now = DateTime.Today;
                FromDate = new DateOnly(now.Year, now.Month, 1);
                ToDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
            }
        }

        private async Task LoadDataAsync()
        {
            // --- Base Income Payments (general ledger's main source) ---
            var incomeQuery = _context.tbl_IncomePayments.AsQueryable();

            if (FromDate.HasValue)
                incomeQuery = incomeQuery.Where(p => p.sys_date >= FromDate.Value);
            if (ToDate.HasValue)
                incomeQuery = incomeQuery.Where(p => p.sys_date <= ToDate.Value);
            if (SelectedPaymentMethodId.HasValue)
            {
                // We only have method name in tbl_IncomePayments;
                // so we resolve the method name then filter by that
                var pmName = await _context.tbl_PaymentMethods
                    .Where(p => p.sys_paymentMethodId == SelectedPaymentMethodId.Value)
                    .Select(p => p.sys_paymentMethodName)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(pmName))
                    incomeQuery = incomeQuery.Where(p => p.sys_paymentMethod == pmName);
            }

            var incomeList = await incomeQuery.AsNoTracking().ToListAsync();

            // --- Tombstone payments (separate table) ---
            var tombstoneQuery = _context.tbl_TombstoneSales.AsQueryable();
            if (FromDate.HasValue)
                tombstoneQuery = tombstoneQuery.Where(t => t.sys_saleDate >= FromDate.Value);
            if (ToDate.HasValue)
                tombstoneQuery = tombstoneQuery.Where(t => t.sys_saleDate <= ToDate.Value);

            var tombstoneList = await tombstoneQuery.AsNoTracking().ToListAsync();

            // --- Invoice category mappings ---
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

            // Optional customer info
            var invoiceCustomers = await _context.tbl_Invoices
                .AsNoTracking()
                .Join(_context.tbl_Customers,
                    inv => inv.sys_customerId,
                    cust => cust.sys_customerId,
                    (inv, cust) => new { inv.sys_invoiceId, cust.sys_paymentMadeBy })
                .ToDictionaryAsync(x => x.sys_invoiceId, x => x.sys_paymentMadeBy);

            // --- Split income into categories ---
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

            // --- Totals ---
            GraveSalesAmount = graveSalesPayments.Sum(p => p.sys_amount);
            ReservationsAmount = reservationPayments.Sum(p => p.sys_amount);
            TombstoneAmount = tombstoneList.Sum(t => t.sys_amountPaid);
            OtherAmount = otherPayments.Sum(p => p.sys_amount);
            TotalAmount = GraveSalesAmount + ReservationsAmount + TombstoneAmount + OtherAmount;

            // --- Build ledger rows ---
            var rows = new List<PaymentLedgerRow>();

            // Grave Sales
            if (SelectedCategory == "All" || SelectedCategory == "GraveSales")
            {
                foreach (var p in graveSalesPayments)
                {
                    invoiceCustomers.TryGetValue(p.sys_invoiceId, out var customerName);

                    rows.Add(new PaymentLedgerRow
                    {
                        Date = p.sys_date,
                        Category = "Grave Sale",
                        InvoiceId = p.sys_invoiceId,
                        ReceiptNumber = p.sys_receiptNumber,
                        PaymentMethod = p.sys_paymentMethod,
                        Amount = p.sys_amount,
                        Payer = customerName ?? "Customer",
                        Notes = p.sys_notes
                    });
                }
            }

            // Reservations
            if (SelectedCategory == "All" || SelectedCategory == "Reservations")
            {
                foreach (var p in reservationPayments)
                {
                    invoiceCustomers.TryGetValue(p.sys_invoiceId, out var customerName);

                    rows.Add(new PaymentLedgerRow
                    {
                        Date = p.sys_date,
                        Category = "Grave Reservation",
                        InvoiceId = p.sys_invoiceId,
                        ReceiptNumber = p.sys_receiptNumber,
                        PaymentMethod = p.sys_paymentMethod,
                        Amount = p.sys_amount,
                        Payer = customerName ?? "Customer",
                        Notes = p.sys_notes
                    });
                }
            }

            // Tombstone payments
            if (SelectedCategory == "All" || SelectedCategory == "Tombstones")
            {
                foreach (var t in tombstoneList)
                {
                    rows.Add(new PaymentLedgerRow
                    {
                        Date = t.sys_saleDate,
                        Category = "Tombstone Sale",
                        InvoiceId = t.sys_invoiceId,
                        ReceiptNumber = null,
                        PaymentMethod = "N/A",
                        Amount = t.sys_amountPaid,
                        Payer = t.sys_buyerName,
                        Notes = t.sys_buyerContact
                    });
                }
            }

            // Other
            if (SelectedCategory == "All" || SelectedCategory == "Other")
            {
                foreach (var p in otherPayments)
                {
                    invoiceCustomers.TryGetValue(p.sys_invoiceId, out var customerName);

                    rows.Add(new PaymentLedgerRow
                    {
                        Date = p.sys_date,
                        Category = "Other Revenue",
                        InvoiceId = p.sys_invoiceId,
                        ReceiptNumber = p.sys_receiptNumber,
                        PaymentMethod = p.sys_paymentMethod,
                        Amount = p.sys_amount,
                        Payer = customerName ?? "Customer",
                        Notes = p.sys_notes
                    });
                }
            }

            Rows = rows
                .OrderBy(r => r.Date)
                .ThenBy(r => r.InvoiceId)
                .ToList();
        }
    }

    public class PaymentLedgerRow
    {
        public DateOnly Date { get; set; }
        public string Category { get; set; }
        public int InvoiceId { get; set; }
        public int? ReceiptNumber { get; set; }
        public string PaymentMethod { get; set; }
        public double Amount { get; set; }
        public string Payer { get; set; }
        public string Notes { get; set; }
    }
}
