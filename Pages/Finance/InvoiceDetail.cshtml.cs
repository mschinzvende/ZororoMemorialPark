using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Finance
{
    public class InvoiceDetailModel : PageModel
    {
        private readonly ApplicationDbContext _context;
       // private readonly InvoicePdfService _pdfService;

        public InvoiceDetailModel(ApplicationDbContext context) //InvoicePdfService pdfService)
        {
            _context = context;
            //_pdfService = pdfService;
        }

        public tbl_Invoices Invoice { get; set; }
        public List<tbl_InvoiceItems> Items { get; set; } = new();
        public List<tbl_IncomePayments> Payments { get; set; } = new();
        public string CustomerName { get; set; }
        public double Total { get; set; }
        public double Paid { get; set; }
        public double Balance => Total - Paid;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Invoice = await _context.tbl_Invoices.FindAsync(id);
            if (Invoice == null) return Page();

            Items = await _context.tbl_InvoiceItems.Where(i => i.sys_invoiceId == id).ToListAsync();
            Payments = await _context.tbl_IncomePayments.Where(p => p.sys_invoiceId == id).OrderBy(p => p.sys_date).ToListAsync();

            Total = Items.Sum(i => i.sys_itemCost);
            Paid = Payments.Sum(p => p.sys_amount);

            CustomerName = await _context.tbl_Customers
                .Where(c => c.sys_customerId == Invoice.sys_customerId)
                .Select(c => c.sys_paymentMadeBy)
                .FirstOrDefaultAsync() ?? "Customer";

            return Page();
        }

        // GET handler to return PDF
      /*  public async Task<IActionResult> OnGetPdfAsync(int id)
        {
            var invoice = await _context.tbl_Invoices.FindAsync(id);
            if (invoice == null) return NotFound();

            var items = await _context.tbl_InvoiceItems.Where(i => i.sys_invoiceId == id).ToListAsync();
            var payments = await _context.tbl_IncomePayments.Where(p => p.sys_invoiceId == id).ToListAsync();
            var customerName = await _context.tbl_Customers
                .Where(c => c.sys_customerId == invoice.sys_customerId)
                .Select(c => c.sys_paymentMadeBy)
                .FirstOrDefaultAsync() ?? "Customer";

            // company placeholders — replace with actual from config/site settings
            string companyName = "Zororo Memorial Park";
            string companyAddress = "40 Livingstone Avenue, Harare, Zimbabwe";
            string companyPhone = "+263 782 352 083";
            string companyEmail = "info@zororomemorial.co.zw";
            string logoPath = null; // optional absolute or relative path to logo file on server

            var pdfBytes = _pdfService.GenerateInvoicePdf(invoice,
                companyName,
                companyAddress,
                companyPhone,
                companyEmail,
                logoPath,
                customerName,
                items,
                payments);

            return File(pdfBytes, "application/pdf", $"{invoice.sys_invoiceNumber}.pdf");
        }*/
    }
}
