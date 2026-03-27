using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Finance
{
    public class InvoicesListModel : PageModel
    {

        private readonly ApplicationDbContext _context;

        public InvoicesListModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<InvoiceRow> Invoices { get; set; } = new();

        public List<tbl_PaymentMethods> PaymentMethods { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string TypeFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }

        public async Task OnGetAsync()
        {

            PaymentMethods = await _context.tbl_PaymentMethods.ToListAsync();

            var customers = await _context.tbl_Customers
            .ToDictionaryAsync(c => c.sys_customerId, c => c.sys_paymentMadeBy);

            var invoiceQuery = _context.tbl_Invoices
            .OrderByDescending(i => i.sys_invoiceDate)
            .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var matchingCustomers = customers
                .Where(c => c.Value.Contains(SearchTerm))
                .Select(c => c.Key)
                .ToList();

                invoiceQuery = invoiceQuery.Where(i =>
                i.sys_invoiceId.ToString().Contains(SearchTerm) ||
                matchingCustomers.Contains(i.sys_customerId));
            }

            var totalRecords = await invoiceQuery.CountAsync();

            TotalPages = (int)Math.Ceiling(totalRecords / (double)PageSize);

            var invoices = await invoiceQuery
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

            if (!invoices.Any()) return;

            var invoiceIds = invoices.Select(i => i.sys_invoiceId).ToList();

            var items = await _context.tbl_InvoiceItems
            .Where(i => invoiceIds.Contains(i.sys_invoiceId))
            .ToListAsync();

            var payments = await _context.tbl_IncomePayments
            .Where(p => invoiceIds.Contains(p.sys_invoiceId))
            .ToListAsync();

            var saleLinks = await _context.tbl_GraveAllocationInvoices
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .ToListAsync();

            var allocationIds = saleLinks.Select(x => x.sys_allocationId).ToList();

            var allocations = await _context.tbl_GraveAllocations
            .Where(a => allocationIds.Contains(a.sys_allocationId))
            .ToDictionaryAsync(a => a.sys_allocationId, a => a.sys_graveId);

            var graveSalesDict = saleLinks.ToDictionary(
            x => x.sys_invoiceId,
            x => allocations.ContainsKey(x.sys_allocationId ?? 0)
            ? (int?)allocations[x.sys_allocationId ?? 0]
            : null);

            var resLinks = await _context.tbl_GraveReservationInvoices
            .Where(x => invoiceIds.Contains(x.sys_invoiceId))
            .ToListAsync();

            var reservationIds = resLinks.Select(x => x.sys_reservationId).ToList();

            var reservations = await _context.tbl_GraveReservations
            .Where(r => reservationIds.Contains(r.sys_reservationId))
            .ToDictionaryAsync(r => r.sys_reservationId, r => r.sys_graveId);

            var graveResDict = resLinks.ToDictionary(
            x => x.sys_invoiceId,
            x => reservations.ContainsKey(x.sys_reservationId)
            ? (int?)reservations[x.sys_reservationId]
            : null);

            foreach (var inv in invoices)
            {

                string type = "ProductService";
                int? graveId = null;

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

                if (TypeFilter != "All" &&
                !string.Equals(TypeFilter, type, StringComparison.OrdinalIgnoreCase))
                    continue;

                var invoiceItems = items.Where(x => x.sys_invoiceId == inv.sys_invoiceId);
                var invoicePayments = payments.Where(x => x.sys_invoiceId == inv.sys_invoiceId);

                var total = invoiceItems.Sum(i => i.sys_itemCost);
                var paid = invoicePayments.Sum(p => p.sys_amount);
                var balance = total - paid;

                if (StatusFilter == "Outstanding" && balance <= 0) continue;
                if (StatusFilter == "Paid" && balance > 0) continue;

                Invoices.Add(new InvoiceRow
                {
                    sys_invoiceId = inv.sys_invoiceId,
                    sys_invoiceDate = inv.sys_invoiceDate,
                    CustomerName = customers.ContainsKey(inv.sys_customerId)
                ? customers[inv.sys_customerId]
                : "Customer",
                    InvoiceType = type,
                    GraveId = graveId,
                    TotalAmount = total,
                    PaidAmount = paid,
                    Balance = balance
                });

            }

        }

        public async Task<IActionResult> OnPostCapturePaymentAsync(
        int InvoiceId,
        double Amount,
        int PaymentMethodId,
        string Notes = "n/a")
        {

            var method = await _context.tbl_PaymentMethods.FindAsync(PaymentMethodId);

            if (method == null) return RedirectToPage();

            if (Notes == null)
            {
                Notes = "n/a";
            }

            var payment = new tbl_IncomePayments
            {
                sys_invoiceId = InvoiceId,
                sys_amount = Amount,
                sys_paymentMethod = method.sys_paymentMethodName,
                sys_notes = Notes,
                sys_date = DateOnly.FromDateTime(DateTime.Now),
                sys_receiptNumber = InvoiceId
            };

            _context.tbl_IncomePayments.Add(payment);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment captured successfully.";

            return RedirectToPage();

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
        }

    }
}
