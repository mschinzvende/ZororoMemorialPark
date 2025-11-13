using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Finance
{
    public class CreateInvoiceModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public CreateInvoiceModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<tbl_Customers> Customers { get; set; }
        public List<tbl_ProductsAndServices> ProductServices { get; set; }
        public List<tbl_Tombstones> Tombstones { get; set; }

        public void OnGet()
        {
            Customers = _context.tbl_Customers.ToList();
            ProductServices = _context.tbl_ProductsAndServices
                .Where(p => p.sys_itemType == "Product" || p.sys_itemType == "Service")
                .Where(p => p.sys_isActive)
                .ToList();
            Tombstones = _context.tbl_Tombstones
                .Where(t => t.sys_status == "Available")
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                var form = Request.Form;

                // Invoice
                var invoice = new tbl_Invoices
                {
                    sys_invoiceNumber = form["InvoiceNumber"],
                    sys_customerId = int.Parse(form["CustomerId"]),
                    sys_invoiceDate = DateOnly.FromDateTime(DateTime.Now),
                    sys_invoiceDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30))
                };
                _context.tbl_Invoices.Add(invoice);
                await _context.SaveChangesAsync();

                // Products/Services
                if (form.ContainsKey("ProductIds"))
                {
                    foreach (var pid in form["ProductIds"])
                    {
                        var qty = double.Parse(form[$"ProductQty_{pid}"]);
                        var product = await _context.tbl_ProductsAndServices.FindAsync(int.Parse(pid));

                        if (product != null)
                        {
                            // Save invoice item
                            _context.tbl_InvoiceItems.Add(new tbl_InvoiceItems
                            {
                                sys_invoiceId = invoice.sys_invoiceId,
                                sys_itemName = product.sys_itemName,
                                sys_itemCost = product.sys_sellingPrice * qty
                            });

                            // Decrease stock for products
                            if (product.sys_itemType == "Product")
                            {
                                product.sys_stockQuantity -= (int)qty;
                                if (product.sys_stockQuantity < 0) product.sys_stockQuantity = 0;

                                // Record stock transaction
                                _context.tbl_StockTransactions.Add(new tbl_StockTransactions
                                {
                                    sys_itemId = product.sys_itemId,
                                    sys_quantity = qty,
                                    sys_transactionType = "Sale",
                                    sys_unitCost = product.sys_unitCost,
                                    sys_referenceNumber = invoice.sys_invoiceNumber
                                });
                            }
                        }
                    }
                }

                // Tombstones
                if (form.ContainsKey("TombstoneIds"))
                {
                    foreach (var tid in form["TombstoneIds"])
                    {
                        var qty = double.Parse(form[$"TombstoneQty_{tid}"]);
                        var tombstone = await _context.tbl_Tombstones.FindAsync(int.Parse(tid));

                        if (tombstone != null)
                        {
                            _context.tbl_InvoiceItems.Add(new tbl_InvoiceItems
                            {
                                sys_invoiceId = invoice.sys_invoiceId,
                                sys_itemName = tombstone.sys_tombstoneName,
                                sys_itemCost = tombstone.sys_price * qty
                            });

                            // If needed, also mark tombstone as sold (optional)
                            tombstone.sys_status = "Sold";
                        }
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Invoice created successfully!";
                return RedirectToPage("InvoicesList"); // adjust to your list page
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error saving invoice: {ex.Message}";
                return RedirectToPage();
            }
        }
    }
}
