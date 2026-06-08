using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Inventory
{
    public class ProductsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<IdentityUser> _userManager;
        private readonly IAppLogger _appLogger;
        public ProductsModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, IAppLogger appLogger)
        {
            _context = context;
            _userManager = userManager;
            _appLogger = appLogger;
        }

        public List<tbl_ProductsAndServices> Products { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var allProducts = await _context.tbl_ProductsAndServices
                .Where(p => p.sys_itemType == "Product")
                .OrderBy(p => p.sys_itemName)
                .ToListAsync();

            bool changesMade = false;

            foreach (var product in allProducts)
            {
                string newStatus;

                

                UpdateStockLevel updateStock = new();
                newStatus = updateStock.GetStockStatus(product.sys_stockQuantity);
                

                if (product.sys_status != newStatus)
                {
                    product.sys_status = newStatus;
                    changesMade = true;
                }
            }

            if (changesMade)
            {
                await _context.SaveChangesAsync();
            }

            Products = allProducts
                .OrderBy(p => p.sys_itemName)
                .ToList();

            
            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Access", "Accessed Products page");
            return Page();
        }

        public async Task<IActionResult> OnPostAddProductAsync()
        {
            try
            {
                var costPrice = 0.0;
                var sellingPrice = 0.0;
                var status = "Out";

                if (Request.Form["sys_category"] == "Sellable")
                {
                    sellingPrice = double.Parse(Request.Form["sys_unitPrice"]);
                }
                else if(Request.Form["sys_category"] =="Consumable")
                {
                    costPrice = double.Parse(Request.Form["sys_unitPrice"]);
                }

               
                UpdateStockLevel updateStock = new();
                status = updateStock.GetStockStatus(double.Parse(Request.Form["sys_stockQuantity"]));

                
                
                var product = new tbl_ProductsAndServices
                {

                    sys_itemName = Request.Form["sys_itemName"],
                    sys_description = Request.Form["sys_description"],
                    sys_category = Request.Form["sys_category"],
                    sys_sellingPrice = sellingPrice,
                    sys_stockQuantity = Int32.Parse(Request.Form["sys_stockQuantity"]),
                    sys_unitOfMeasure = Request.Form["sys_measurementUnit"],
                    sys_isActive = true,
                    sys_itemType = "Product",
                    sys_status = status,
                    sys_unitCost = costPrice
                };

                _context.tbl_ProductsAndServices.Add(product);
                await _context.SaveChangesAsync();

                SuccessMessage = "Product added successfully.";

                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Create", $"Added new product: {product.sys_itemName}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error adding product: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error adding product: {ex.Message}");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditProductAsync()
        {
            try
            {
                var costPrice = 0.0;
                var sellingPrice = 0.0;
                var status = "Out";

                if (Request.Form["sys_category"] == "Sellable")
                {
                    sellingPrice = double.Parse(Request.Form["sys_unitPrice"]);
                }
                else if (Request.Form["sys_category"] == "Consumable")
                {
                    costPrice = double.Parse(Request.Form["sys_unitPrice"]);
                }

                if (Int32.Parse(Request.Form["sys_stockQuantity"]) <= 0)
                {
                    status = "Out";
                }



                else if (Int32.Parse(Request.Form["sys_stockQuantity"]) > 10)
                {
                    status = "InStock";
                }

                else
                {
                    status = "Low";
                }


                int id = int.Parse(Request.Form["sys_itemId"]);
                var product = await _context.tbl_ProductsAndServices.FindAsync(id);

                if (product == null)
                {
                    ErrorMessage = "Product not found.";
                    return RedirectToPage();
                }

                product.sys_itemName = Request.Form["sys_itemName"];
                product.sys_description = Request.Form["sys_description"];
                product.sys_category = Request.Form["sys_category"];
                product.sys_sellingPrice = sellingPrice;
                product.sys_stockQuantity = Int32.Parse(Request.Form["sys_stockQuantity"]);
                product.sys_unitOfMeasure = Request.Form["sys_measurementUnit"];
                product.sys_unitCost = costPrice;
                product.sys_status = status;

                _context.tbl_ProductsAndServices.Update(product);
                await _context.SaveChangesAsync();

                SuccessMessage = "Product updated successfully.";

                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Edit", $"Edited product: {product.sys_itemName}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error updating product: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error updating product: {ex.Message}");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteProductAsync()
        {
            try
            {
                int id = int.Parse(Request.Form["sys_itemId"]);
                var product = await _context.tbl_ProductsAndServices.FindAsync(id);

                if (product == null)
                {
                    ErrorMessage = "Product not found.";
                    return RedirectToPage();
                }

                _context.tbl_ProductsAndServices.Remove(product);
                await _context.SaveChangesAsync();

                SuccessMessage = "Product deleted successfully.";

                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Delete", $"Deleted product: {product.sys_itemName}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error deleting product: " + ex.Message;
                await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Error", $"Error deleting product: {ex.Message}");
            }

            return RedirectToPage();
        }
    }
}
