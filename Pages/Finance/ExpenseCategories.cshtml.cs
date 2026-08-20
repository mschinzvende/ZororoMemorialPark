using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Finance
{
    [Authorize(Roles = "Manager")]
    public class ExpenseCategoriesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExpenseCategoriesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public CategoryInput NewCategory { get; set; } = new();

        [BindProperty]
        public CategoryInput EditCategory { get; set; } = new();

        public List<CategoryRow> Categories { get; private set; } = new();

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var categoryName = await ValidateCategoryAsync(NewCategory);
            if (categoryName == null)
            {
                return RedirectToPage();
            }

            _context.tbl_ExpenseCategories.Add(new tbl_ExpenseCategories
            {
                sys_expenseCategoryName = categoryName,
                sys_displayOrder = NewCategory.DisplayOrder
            });

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{categoryName} was added to the expense categories.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "An expense category with that name already exists.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var category = await _context.tbl_ExpenseCategories.FindAsync(EditCategory.Id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "The expense category could not be found.";
                return RedirectToPage();
            }

            var categoryName = await ValidateCategoryAsync(EditCategory, category.sys_expenseCategoryId);
            if (categoryName == null)
            {
                return RedirectToPage();
            }

            var previousName = category.sys_expenseCategoryName;
            category.sys_expenseCategoryName = categoryName;
            category.sys_displayOrder = EditCategory.DisplayOrder;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SaveChangesAsync();

                if (!string.Equals(previousName, categoryName, StringComparison.Ordinal))
                {
                    await _context.tbl_Expenses
                        .Where(x => !x.sys_jobId.HasValue && x.sys_expenseCategory == previousName)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(x => x.sys_expenseCategory, categoryName));
                }

                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"{categoryName} was updated. Existing expense records were retained under the updated category name.";
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "An expense category with that name already exists.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int categoryId)
        {
            var category = await _context.tbl_ExpenseCategories.FindAsync(categoryId);
            if (category == null)
            {
                TempData["ErrorMessage"] = "The expense category could not be found.";
                return RedirectToPage();
            }

            var inUse = await _context.tbl_Expenses
                .AnyAsync(x => !x.sys_jobId.HasValue && x.sys_expenseCategory == category.sys_expenseCategoryName);

            if (inUse)
            {
                TempData["ErrorMessage"] = $"{category.sys_expenseCategoryName} cannot be deleted because it is used by recorded expenses.";
                return RedirectToPage();
            }

            _context.tbl_ExpenseCategories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{category.sys_expenseCategoryName} was deleted.";
            return RedirectToPage();
        }

        private async Task<string?> ValidateCategoryAsync(CategoryInput input, int? currentCategoryId = null)
        {
            var categoryName = input.Name?.Trim();
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                TempData["ErrorMessage"] = "Category name is required.";
                return null;
            }

            if (categoryName.Length > 100)
            {
                TempData["ErrorMessage"] = "Category name cannot exceed 100 characters.";
                return null;
            }

            if (string.Equals(categoryName, "Construction", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Construction is reserved for grave-development job payments.";
                return null;
            }

            if (input.DisplayOrder < 0 || input.DisplayOrder > 999)
            {
                TempData["ErrorMessage"] = "Display order must be between 0 and 999.";
                return null;
            }

            var normalizedName = categoryName.ToUpper();
            var exists = await _context.tbl_ExpenseCategories.AnyAsync(x =>
                x.sys_expenseCategoryName.ToUpper() == normalizedName &&
                (!currentCategoryId.HasValue || x.sys_expenseCategoryId != currentCategoryId.Value));

            if (exists)
            {
                TempData["ErrorMessage"] = "An expense category with that name already exists.";
                return null;
            }

            return categoryName;
        }

        private async Task LoadCategoriesAsync()
        {
            var categoryRows = await _context.tbl_ExpenseCategories
                .AsNoTracking()
                .OrderBy(x => x.sys_displayOrder)
                .ThenBy(x => x.sys_expenseCategoryName)
                .ToListAsync();

            var usage = await _context.tbl_Expenses
                .AsNoTracking()
                .Where(x => !x.sys_jobId.HasValue)
                .GroupBy(x => x.sys_expenseCategory)
                .Select(group => new { Category = group.Key, Count = group.Count(), Total = group.Sum(x => x.sys_amountPaid) })
                .ToListAsync();

            var usageByCategory = usage.ToDictionary(x => x.Category, StringComparer.OrdinalIgnoreCase);
            Categories = categoryRows.Select(category =>
            {
                usageByCategory.TryGetValue(category.sys_expenseCategoryName, out var categoryUsage);
                return new CategoryRow
                {
                    Id = category.sys_expenseCategoryId,
                    Name = category.sys_expenseCategoryName,
                    DisplayOrder = category.sys_displayOrder,
                    ExpenseCount = categoryUsage?.Count ?? 0,
                    TotalPaid = categoryUsage?.Total ?? 0d
                };
            }).ToList();
        }

        public class CategoryInput
        {
            public int Id { get; set; }

            [Required]
            public string Name { get; set; } = string.Empty;

            [Range(0, 999)]
            public int DisplayOrder { get; set; }
        }

        public class CategoryRow
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int DisplayOrder { get; set; }
            public int ExpenseCount { get; set; }
            public double TotalPaid { get; set; }
        }
    }
}
