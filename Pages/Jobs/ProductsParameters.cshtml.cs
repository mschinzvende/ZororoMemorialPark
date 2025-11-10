using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Jobs
{
    public class ProductsParametersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public ProductsParametersModel(ApplicationDbContext context) => _context = context;

        [BindProperty]
        public int ParameterId { get; set; }

        public tbl_jobEvaluationParameters Parameter { get; set; }
        public List<tbl_EvaluationParameterProducts> LinkedProducts { get; set; }
        public List<SelectListItem> ProductList { get; set; }

        [BindProperty]
        public int SelectedProductId { get; set; }

        [BindProperty]
        public double DefaultQty { get; set; } = 1;

        public async Task OnGetAsync(int id)
        {
            ParameterId = id;
            Parameter = await _context.tbl_jobEvaluationParameters.FindAsync(id);

            LinkedProducts = await _context.tbl_EvaluationParameterProducts
                .Include(p => p.tbl_ProductsAndServices)
                .Where(p => p.sys_jobEvaluationParameterId == id)
                .ToListAsync();

            ProductList = await _context.tbl_ProductsAndServices
                .Where(p => p.sys_itemType == "Product")
                .Select(p => new SelectListItem
                {
                    Value = p.sys_itemId.ToString(),
                    Text = p.sys_itemName
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            var exists = await _context.tbl_EvaluationParameterProducts
                .AnyAsync(p => p.sys_jobEvaluationParameterId == ParameterId && p.sys_itemId == SelectedProductId);

            if (exists)
            {
                TempData["Error"] = "This product is already linked.";
                return RedirectToPage(new { id = ParameterId });
            }

            _context.tbl_EvaluationParameterProducts.Add(new tbl_EvaluationParameterProducts
            {
                sys_jobEvaluationParameterId = ParameterId,
                sys_itemId = SelectedProductId,
                sys_defaultQty = DefaultQty
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product linked successfully.";
            return RedirectToPage(new { id = ParameterId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int linkId)
        {
            var record = await _context.tbl_EvaluationParameterProducts.FindAsync(linkId);
            if (record != null)
            {
                _context.tbl_EvaluationParameterProducts.Remove(record);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Product link removed.";
            }
            return RedirectToPage(new { id = ParameterId });
        }
    }
}
