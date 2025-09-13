using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Pages.Graves;
using System.Drawing.Printing;

namespace ZororoMemorialPark.Pages.PaymentMethods
{
    public class PaymentMethodsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        public PaymentMethodsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Pagination
        public PaginatedList<tbl_PaymentMethods> PaginatedMethods { get; set; }
        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;
        private const int pageSize = 10;

        // Form binder
        [BindProperty] public tbl_PaymentMethods PaymentMethod { get; set; }

       // [BindProperty] public string SelectedIds { get; set; }

        public async Task OnGetAsync(int? pageIndex)
        {
            PageIndex = pageIndex ?? 1;

            var query = _context.tbl_PaymentMethods.OrderBy(pm => pm.sys_paymentMethodName).AsNoTracking();
            

            PaginatedMethods = await PaginatedList<tbl_PaymentMethods>.CreateAsync(query, PageIndex, pageSize);
        }

        // --- CRUD ---
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid) {

                var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"Validation failed: {errors}";
                return RedirectToPage();
            }
           

            _context.tbl_PaymentMethods.Add(PaymentMethod);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Payment method added successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.tbl_PaymentMethods.Update(PaymentMethod);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Payment method updated successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {

            var method = await _context.tbl_PaymentMethods.FindAsync(PaymentMethod.sys_paymentMethodId);
            if (method != null)
            {
                _context.tbl_PaymentMethods.Remove(method);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payment method deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Payment method not found.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteSelectedAsync([FromForm] string SelectedIds)
        {
            if (string.IsNullOrEmpty(SelectedIds)) return RedirectToPage();

            var ids = SelectedIds.Split(',').Select(int.Parse).ToList();
            var methods = _context.tbl_PaymentMethods.Where(pm => ids.Contains(pm.sys_paymentMethodId));

            _context.tbl_PaymentMethods.RemoveRange(methods);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"{ids.Count} payment method(s) deleted successfully.";
            return RedirectToPage();
        }
    }
}
