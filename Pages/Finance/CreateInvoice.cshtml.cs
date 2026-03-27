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
            public double Discount { get; set; }
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
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (Items == null || Items.Count == 0)
                {
                    TempData["ErrorMessage"] = "No invoice items selected.";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(customerName))
                {
                    TempData["ErrorMessage"] = "Customer name is required.";
                    return RedirectToPage();
                }

                //-----------------------------------------
                // CREATE CUSTOMER
                //-----------------------------------------

                var customer = new tbl_Customers
                {
                    sys_paymentMadeBy = customerName.Trim(),
                    sys_customerPhone = customerPhone?.Trim()
                };

                _context.tbl_Customers.Add(customer);
                await _context.SaveChangesAsync();


                //-----------------------------------------
                // CREATE INVOICE
                //-----------------------------------------

                var invoice = new tbl_Invoices
                {
                    sys_customerId = customer.sys_customerId,
                    sys_invoiceDate = DateOnly.FromDateTime(DateTime.Now),
                    sys_invoiceDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(7))
                };

                _context.tbl_Invoices.Add(invoice);
                await _context.SaveChangesAsync();


                //-----------------------------------------
                // CREATE INVOICE ITEMS
                //-----------------------------------------

                foreach (var item in Items)
                {
                    double lineTotal = (item.Price * item.Qty) - item.Discount;

                    var ii = new tbl_InvoiceItems
                    {
                        sys_invoiceId = invoice.sys_invoiceId,
                        sys_itemName = item.Name,
                        sys_itemCost = lineTotal
                    };

                    _context.tbl_InvoiceItems.Add(ii);


                    //-----------------------------------------
                    // STOCK MANAGEMENT
                    //-----------------------------------------

                    if (item.Type == "product")
                    {
                        var product = await _context.tbl_ProductsAndServices
                            .FirstOrDefaultAsync(p => p.sys_itemName == item.Name);

                        if (product != null)
                        {
                            product.sys_stockQuantity -= item.Qty;

                            if (product.sys_stockQuantity < 0)
                                product.sys_stockQuantity = 0;

                            if (product.sys_stockQuantity == 0)
                                product.sys_status = "Out of Stock";
                            else if (product.sys_stockQuantity <= 3)
                                product.sys_status = "Low Stock";

                            var transactionRecord = new tbl_StockTransactions
                            {
                                sys_itemId = product.sys_itemId,
                                sys_transactionDate = DateTime.Now,
                                sys_transactionType = "Sale",
                                sys_quantity = item.Qty,
                                sys_unitCost = item.Price,
                                sys_referenceNumber = $"INV-{invoice.sys_invoiceId}",
                                sys_notes = $"Sold via Invoice {invoice.sys_invoiceId}"
                            };

                            _context.tbl_StockTransactions.Add(transactionRecord);
                        }
                    }

                    //-----------------------------------------
                    // TOMBSTONE STOCK
                    //-----------------------------------------

                    if (item.Type == "tombstone")
                    {
                        var tombstone = await _context.tbl_Tombstones
                            .FirstOrDefaultAsync(t => t.sys_tombstoneName == item.Name);

                        if (tombstone != null)
                        {
                            tombstone.sys_quantityAvailable -= item.Qty;
                            tombstone.sys_status = "Sold";
                        }
                    }
                }


                //-----------------------------------------
                // SAVE EVERYTHING
                //-----------------------------------------

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();


                //-----------------------------------------
                // REDIRECT TO INVOICE DETAIL
                //-----------------------------------------

                TempData["SuccessMessage"] = "Invoice created successfully.";

                return RedirectToPage("/Finance/InvoiceDetail",
                    new { id = invoice.sys_invoiceId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

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
