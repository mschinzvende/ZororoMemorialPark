using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ZororoMemorialPark.Data; // adjust namespace
using System.ComponentModel.DataAnnotations;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Pages.Graves;

namespace ZororoMemorialPark.Pages.Jobs
{
    public class SuppliersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 10;

        public SuppliersModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Pagination
        public PaginatedList<tbl_Suppliers> PaginatedSuppliers { get; set; }

        // For binding forms
        [BindProperty]
        public tbl_Suppliers Supplier { get; set; }

        

        // GET
        public async Task OnGetAsync(int pageIndex = 1)
        {
            var query = _context.tbl_Suppliers.OrderBy(s => s.sys_supplierName);
            PaginatedSuppliers = await PaginatedList<tbl_Suppliers>.CreateAsync(query.AsNoTracking(), pageIndex, PageSize);
        }

        // CREATE
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"Validation failed: {errors}";
                return RedirectToPage();
            }

            _context.tbl_Suppliers.Add(Supplier);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Supplier added successfully.";
            return RedirectToPage();
        }

        // EDIT
        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input!";
                return RedirectToPage();
            }

            var supplierInDb = await _context.tbl_Suppliers.FindAsync(Supplier.sys_supplierId);
            if (supplierInDb == null)
            {
                TempData["ErrorMessage"] = "Supplier not found!";
                return RedirectToPage();
            }

            supplierInDb.sys_supplierName = Supplier.sys_supplierName;
            supplierInDb.sys_supplierAddress = Supplier.sys_supplierAddress;
            supplierInDb.sys_supplierPhone = Supplier.sys_supplierPhone;
            supplierInDb.sys_supplierEmail = Supplier.sys_supplierEmail;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Supplier updated successfully.";
            return RedirectToPage();
        }

        // DELETE SINGLE
        public async Task<IActionResult> OnPostDeleteAsync(int sys_supplierId)
        {
            var supplier = await _context.tbl_Suppliers.FindAsync(sys_supplierId);
            if (supplier != null)
            {
                _context.tbl_Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Supplier deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Supplier not found!";
            }

            return RedirectToPage();
        }

        // DELETE MULTIPLE
        public async Task<IActionResult> OnPostDeleteSelectedAsync([FromForm] string SelectedIds)
        {
            if (string.IsNullOrWhiteSpace(SelectedIds))
            {
                TempData["ErrorMessage"] = "No suppliers selected!";
                return RedirectToPage();
            }

            var ids = SelectedIds.Split(',').Select(int.Parse).ToList();
            var suppliers = await _context.tbl_Suppliers.Where(s => ids.Contains(s.sys_supplierId)).ToListAsync();

            if (suppliers.Any())
            {
                _context.tbl_Suppliers.RemoveRange(suppliers);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{suppliers.Count} supplier(s) deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "No suppliers found for deletion!";
            }

            return RedirectToPage();
        }
    }

    

public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }

}
