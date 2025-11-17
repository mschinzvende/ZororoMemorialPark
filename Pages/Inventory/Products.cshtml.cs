using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

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
            Products = await _context.tbl_ProductsAndServices
                .Where(p => p.sys_itemType == "Product")
                .OrderBy(p => p.sys_itemName)
                .ToListAsync();

            await _appLogger.LogActivityAsync(_userManager.GetUserId(User), "Access", "Accessed Products page");
            return Page();
        }

        public async Task<IActionResult> OnPostAddProductAsync()
        {
            try
            {
                var product = new tbl_ProductsAndServices
                {
                    sys_itemName = Request.Form["sys_itemName"],
                    sys_description = Request.Form["sys_description"],
                    sys_category = Request.Form["sys_category"],
                    sys_sellingPrice = double.Parse(Request.Form["sys_unitPrice"]),
                    sys_stockQuantity = Int32.Parse(Request.Form["sys_stockQuantity"]),
                    sys_unitOfMeasure = Request.Form["sys_measurementUnit"],
                    sys_isActive = true,
                    sys_itemType = "Product"
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
                product.sys_sellingPrice = double.Parse(Request.Form["sys_unitPrice"]);
                product.sys_stockQuantity = Int32.Parse(Request.Form["sys_stockQuantity"]);
                product.sys_unitOfMeasure = Request.Form["sys_measurementUnit"];

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
