using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using Microsoft.AspNetCore.Mvc;
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

        public List<tbl_PaymentMethods> PaymentMethods { get; set; }


        public List<InvoiceRow> Invoices { get; set; } = new();
        [BindProperty(SupportsGet = true)]
        public int? ExpandedId { get; set; }

        public async Task OnGetAsync(int? expandedId)
        {
            ExpandedId = expandedId;

            PaymentMethods = await _context.tbl_PaymentMethods
                            .OrderBy(x => x.sys_paymentMethodName)
                            .ToListAsync();


            var invoices = await _context.tbl_Invoices
                .OrderByDescending(i => i.sys_invoiceDate)
                .ToListAsync();

            foreach (var inv in invoices)
            {
                // determine invoice type
                var type = "ProductService";
                if (await _context.tbl_GraveAllocationInvoices.AnyAsync(x => x.sys_invoiceId == inv.sys_invoiceId))
                    type = "GraveSale";
                else if (await _context.tbl_GraveReservationInvoices.AnyAsync(x => x.sys_invoiceId == inv.sys_invoiceId))
                    type = "Reservation";

                var items = await _context.tbl_InvoiceItems
                    .Where(it => it.sys_invoiceId == inv.sys_invoiceId)
                    .ToListAsync();

                var payments = await _context.tbl_IncomePayments
                    .Where(p => p.sys_invoiceId == inv.sys_invoiceId)
                    .OrderBy(p => p.sys_date)
                    .ToListAsync();

                var total = items.Sum(i => i.sys_itemCost);
                var paid = payments.Sum(p => p.sys_amount);

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
                    TotalAmount = total,
                    PaidAmount = paid,
                    Payments = payments
                });
            }
        }


        public async Task<IActionResult> OnPostCapturePaymentAsync(
         int InvoiceId,
         double Amount,
         int PaymentMethodId,
         string Notes="n/a")
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
                ModelState.AddModelError(string.Empty, "Payment exceeds invoice balance.");
                return Page();
            }

            var method = await _context.tbl_PaymentMethods.FindAsync(PaymentMethodId);

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
            return RedirectToPage("/Finance/InvoiceDetail", new { id = InvoiceId });
        }



        public class InvoiceRow
        {
            public int sys_invoiceId { get; set; }
            public string sys_invoiceNumber { get; set; }
            public DateOnly sys_invoiceDate { get; set; }
            public string CustomerName { get; set; }
            public string InvoiceType { get; set; }
            public double TotalAmount { get; set; }
            public double PaidAmount { get; set; }
            public List<tbl_IncomePayments> Payments { get; set; } = new();
        }
    }
}
