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
    public class CreateInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateInvoiceModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ---------------------------
        // DROPDOWN DATA
        // ---------------------------
        public List<tbl_Customers> Customers { get; set; }
        public List<tbl_ProductsAndServices> Products { get; set; }
        public List<tbl_ProductsAndServices> Services { get; set; }
        public List<tbl_Tombstones> Tombstones { get; set; }

        // ---------------------------
        // FORM INPUT
        // ---------------------------
        [BindProperty]
        public string customerName { get; set; }

        [BindProperty]
        public string customerPhone { get; set; }

        [BindProperty]
        public List<InvoiceItemInput> Items { get; set; } = new();


        public class InvoiceItemInput
        {
            public string Name { get; set; }
            public double Price { get; set; }
            public int Qty { get; set; }

            public string Type { get; set; }
        }

        // ---------------------------
        // LOAD PAGE
        // ---------------------------
        public async Task<IActionResult> OnGetAsync()
        {
            /*Customers = await _context.tbl_Customers
                .OrderBy(x => x.sys_paymentMadeBy)
                .ToListAsync();*/

            Products = await _context.tbl_ProductsAndServices
                .Where(p => p.sys_itemType == "Product" && p.sys_category == "Sellable")
                .ToListAsync();

            Services = await _context.tbl_ProductsAndServices
                .Where(p => p.sys_itemType == "Service" && p.sys_category == "Sellable")
                .ToListAsync();

            Tombstones = await _context.tbl_Tombstones
                .Where(t => t.sys_status == "Available")
                .ToListAsync();

            return Page();
        }

        // ---------------------------
        // SAVE INVOICE ROUTINE
        // ---------------------------
        public async Task<IActionResult> OnPostAsync()
        {

            try
            {


                // VALIDATION


                if (Items == null || Items.Count == 0)
                    return BadRequest("No invoice items selected.");

                // CREATE INVOICE NUMBER
                //string invoiceNumber = "INV-" + DateTime.Now.ToString("yyyyMMddHHmmss");

                var customer = new tbl_Customers
                {
                    sys_paymentMadeBy = customerName,
                    sys_customerPhone = customerPhone
                };
                await _context.tbl_Customers.AddAsync(customer);
                await _context.SaveChangesAsync();


                var invoice = new tbl_Invoices
                {
                    sys_customerId = customer.sys_customerId,

                    sys_invoiceDate = DateOnly.Parse(DateTime.Now.ToString("dd-MM-yyyy")),
                    sys_invoiceDueDate = DateOnly.Parse(DateTime.Now.AddDays(7).ToString("dd-MM-yyyy"))

                };

                await _context.tbl_Invoices.AddAsync(invoice);
                await _context.SaveChangesAsync();

                // ---------------------------
                // SAVE ITEMS + UPDATE STOCK
                // ---------------------------
                foreach (var item in Items)
                {
                    // Save invoice item
                    var ii = new tbl_InvoiceItems
                    {
                        sys_invoiceId = invoice.sys_invoiceId,
                        sys_itemName = item.Name,
                        sys_itemCost = item.Price * item.Qty,

                    };
                    await _context.tbl_InvoiceItems.AddAsync(ii);


                    if (item.Type == "product")
                    {
                        //create product transaction record.

                        var transaction = new tbl_StockTransactions
                        {
                            sys_itemId = (await _context.tbl_ProductsAndServices
                                .Where(p => p.sys_itemName == item.Name)
                                .Select(p => p.sys_itemId)
                                .FirstOrDefaultAsync()),
                            sys_transactionDate = DateTime.Now,
                            sys_transactionType = "Sale",
                            sys_quantity = item.Qty,
                            sys_unitCost = item.Price,
                            sys_referenceNumber = $"INV-{invoice.sys_invoiceId}",
                            
                            sys_notes = $"Sold via Invoice {invoice.sys_invoiceId}"
                        };

                        await _context.tbl_StockTransactions.AddAsync(transaction);
                        await _context.SaveChangesAsync();
                        // STOCK MANAGEMENT
                        await DecreaseStockIfApplicable(item);

                    }

                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Invoice created successfully!";
                return RedirectToPage("InvoiceDetail", new { id = invoice.sys_invoiceId });
            }
            catch(Exception ex)
            {
                TempData["ErrorMessage"] = "Error creating invoice: " + ex.Message;
                return RedirectToPage();
            }
        }

        // ---------------------------
        // STOCK REDUCTION LOGIC
        // ---------------------------
        private async Task DecreaseStockIfApplicable(InvoiceItemInput item)
        {
            // ? CHECK IF ITEM MATCHES A PRODUCT
            var product = await _context.tbl_ProductsAndServices
                .FirstOrDefaultAsync(x => x.sys_itemName == item.Name);

            if (product != null)
            {
                product.sys_stockQuantity -= item.Qty;

                if (product.sys_stockQuantity < 0)
                    product.sys_stockQuantity = 0;

                // Update product stock status
                if (product.sys_stockQuantity == 0)
                    product.sys_status = "Out of Stock";
                else if (product.sys_stockQuantity <= 3)
                    product.sys_status = "Low Stock";

                return;
            }

            // ? CHECK IF ITEM MATCHES A TOMBSTONE
            var tombstone = await _context.tbl_Tombstones
                .FirstOrDefaultAsync(x => x.sys_tombstoneName == item.Name);

            if (tombstone != null)
            {
                tombstone.sys_quantityAvailable -= item.Qty; 
                tombstone.sys_status = "Sold";
            }
        }
    }
}
