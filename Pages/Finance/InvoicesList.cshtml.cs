using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZororoMemorialPark.Pages.Finance
{
    public class InvoicesListModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public InvoicesListModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<tbl_PaymentMethods> PaymentMethods { get; set; } = new();

        public List<InvoiceRow> Invoices { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? ExpandedId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "All";

        public async Task OnGetAsync(int? expandedId, string? typeFilter, string? statusFilter)
        {
            ExpandedId = expandedId;
            TypeFilter = string.IsNullOrWhiteSpace(typeFilter) ? "All" : typeFilter;
            StatusFilter = string.IsNullOrWhiteSpace(statusFilter) ? "All" : statusFilter;

            PaymentMethods = await _context.tbl_PaymentMethods
                .OrderBy(x => x.sys_paymentMethodName)
                .ToListAsync();

            var invoices = await _context.tbl_Invoices
                .OrderByDescending(i => i.sys_invoiceDate)
                .ToListAsync();

            if (!invoices.Any())
                return;

            var invoiceIds = invoices.Select(i => i.sys_invoiceId).ToList();

            // ------------------------------------------------------------
            // 1?? GET GRAVE SALES (ALLOCATIONS ? INVOICE ? GRAVE)
            // ------------------------------------------------------------
            var saleLinks = await _context.tbl_GraveAllocationInvoices
                .Where(x => invoiceIds.Contains(x.sys_invoiceId))
                .ToListAsync();

            var allocationIds = saleLinks.Select(x => x.sys_allocationId).ToList();

            var allocations = await _context.tbl_GraveAllocations
                .Where(a => allocationIds.Contains(a.sys_allocationId))
                .ToListAsync();

            var allocationDict = allocations
                .ToDictionary(a => a.sys_allocationId, a => a.sys_graveId);

            var graveSalesDict = saleLinks
                .ToDictionary(
                    x => x.sys_invoiceId,
                    x => allocationDict.ContainsKey(x.sys_allocationId)
                            ? (int?)allocationDict[x.sys_allocationId]
                            : null
                );


            // ------------------------------------------------------------
            // 2?? GET GRAVE RESERVATIONS (RESERVATIONS ? INVOICE ? GRAVE)
            // ------------------------------------------------------------
            var resLinks = await _context.tbl_GraveReservationInvoices
                .Where(x => invoiceIds.Contains(x.sys_invoiceId))
                .ToListAsync();

            var reservationIds = resLinks.Select(x => x.sys_reservationId).ToList();

            var reservations = await _context.tbl_GraveReservations
                .Where(r => reservationIds.Contains(r.sys_reservationId))
                .ToListAsync();

            var resDict = reservations
                .ToDictionary(r => r.sys_reservationId, r => r.sys_graveId);

            var graveResDict = resLinks
                .ToDictionary(
                    x => x.sys_invoiceId,
                    x => resDict.ContainsKey(x.sys_reservationId)
                            ? (int?)resDict[x.sys_reservationId]
                            : null
                );


            // ------------------------------------------------------------
            // 3?? BUILD INVOICE ROWS
            // ------------------------------------------------------------
            foreach (var inv in invoices)
            {
                string type = "ProductService";
                int? graveId = null;

                // Determine Type + GraveId from dictionaries
                if (graveSalesDict.ContainsKey(inv.sys_invoiceId))
                {
                    type = "GraveSale";
                    graveId = graveSalesDict[inv.sys_invoiceId];
                }
                else if (graveResDict.ContainsKey(inv.sys_invoiceId))
                {
                    type = "Reservation";
                    graveId = graveResDict[inv.sys_invoiceId];
                }

                // Type filter
                if (TypeFilter != "All" && !string.Equals(TypeFilter, type, StringComparison.OrdinalIgnoreCase))
                    continue;

                var items = await _context.tbl_InvoiceItems
                    .Where(it => it.sys_invoiceId == inv.sys_invoiceId)
                    .ToListAsync();

                var payments = await _context.tbl_IncomePayments
                    .Where(p => p.sys_invoiceId == inv.sys_invoiceId)
                    .OrderBy(p => p.sys_date)
                    .ToListAsync();

                var total = items.Sum(i => i.sys_itemCost);
                var paid = payments.Sum(p => p.sys_amount);
                var balance = total - paid;

                // Status filter
                if (StatusFilter == "Outstanding" && balance <= 0)
                    continue;

                if (StatusFilter == "Paid" && balance > 0)
                    continue;

                var customerName = await _context.tbl_Customers
                    .Where(c => c.sys_customerId == inv.sys_customerId)
                    .Select(c => c.sys_paymentMadeBy)
                    .FirstOrDefaultAsync() ?? "Customer";

                Invoices.Add(new InvoiceRow
                {
                    sys_invoiceId = inv.sys_invoiceId,
                    sys_invoiceDate = inv.sys_invoiceDate,
                    CustomerName = customerName,
                    InvoiceType = type,
                    GraveId = graveId,
                    TotalAmount = total,
                    PaidAmount = paid,
                    Balance = balance,
                    Payments = payments
                });
            }
        }


        public async Task<IActionResult> OnPostCapturePaymentAsync(
            int InvoiceId,
            double Amount,
            int PaymentMethodId,
            string Notes = "n/a")
        {
            var invoice = await _context.tbl_Invoices
                .FirstOrDefaultAsync(i => i.sys_invoiceId == InvoiceId);

            if (invoice == null)
                return NotFound();

            var items = await _context.tbl_InvoiceItems
                .Where(i => i.sys_invoiceId == InvoiceId)
                .ToListAsync();

            var payments = await _context.tbl_IncomePayments
                .Where(p => p.sys_invoiceId == InvoiceId)
                .ToListAsync();

            var total = items.Sum(i => i.sys_itemCost);
            var paid = payments.Sum(p => p.sys_amount);
            var balance = total - paid;

            if (Amount > balance)
            {
                TempData["ErrorMessage"] = "Payment exceeds invoice balance.";
                return RedirectToPage(new { id = InvoiceId });
            }

            var method = await _context.tbl_PaymentMethods.FindAsync(PaymentMethodId);
            if (method == null)
            {
                TempData["ErrorMessage"] = "Invalid payment method.";
                return RedirectToPage(new { id = InvoiceId });
            }

            var payment = new tbl_IncomePayments
            {
                sys_invoiceId = InvoiceId,
                sys_amount = Amount,
                sys_paymentMethod = method.sys_paymentMethodName,
                sys_notes = string.IsNullOrWhiteSpace(Notes) ? "n/a" : Notes,
                sys_date = DateOnly.FromDateTime(DateTime.Now),
                sys_receiptNumber = InvoiceId
            };

            _context.tbl_IncomePayments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment captured successfully.";
            return RedirectToPage("/Finance/InvoiceDetail", new { id = InvoiceId });
        }

        public class InvoiceRow
        {
            public int sys_invoiceId { get; set; }
            public DateOnly sys_invoiceDate { get; set; }
            public string CustomerName { get; set; }
            public string InvoiceType { get; set; }
            public double TotalAmount { get; set; }
            public double PaidAmount { get; set; }
            public double Balance { get; set; }
            public int? GraveId { get; set; }
            public List<tbl_IncomePayments> Payments { get; set; } = new();
        }
    }
}
