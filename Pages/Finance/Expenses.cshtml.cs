using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Finance
{
    [Authorize(Roles = "Manager,FinanceOfficer")]
    public class ExpensesModel : PageModel
    {
        private const int DefaultPageSize = 20;
        private readonly ApplicationDbContext _context;

        public ExpensesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<tbl_ExpenseCategories> ExpenseCategories { get; private set; } = new();

        [BindProperty]
        public RequisitionExpenseInput NewExpense { get; set; } = new()
        {
            ExpenseDate = DateOnly.FromDateTime(DateTime.Today)
        };

        [BindProperty]
        public RequisitionExpenseInput EditExpense { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public DateOnly? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string CategoryFilter { get; set; } = "All";

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int? RequisitionId { get; set; }

        public int TotalPages { get; private set; } = 1;
        public int TotalRecords { get; private set; }
        public double TotalExpenses { get; private set; }
        public double DailyOperatingExpenses { get; private set; }
        public double ConstructionExpenses { get; private set; }
        public List<ExpenseRow> Expenses { get; private set; } = new();
        public List<tbl_Suppliers> Suppliers { get; private set; } = new();
        public List<tbl_PaymentMethods> PaymentMethods { get; private set; } = new();
        public List<ApprovedRequisitionLine> ApprovedRequisitionLines { get; private set; } = new();

        public async Task OnGetAsync()
        {
            SetDefaultDateRange();
            await LoadPageAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var validationError = await ValidateInputAsync(NewExpense);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToPage();
            }

            var approvedLine = await LoadApprovedLineAsync(NewExpense.RequisitionLineId);
            if (approvedLine == null)
            {
                TempData["ErrorMessage"] = "Choose an approved requisition item that has not already been paid.";
                return RedirectToPage();
            }

            if (approvedLine.ApprovedAmount <= 0)
            {
                TempData["ErrorMessage"] = "The selected approved requisition item has no payable value.";
                return RedirectToPage();
            }

            if (await _context.tbl_Expenses.AnyAsync(x => x.sys_requisitionLineId == approvedLine.LineId))
            {
                TempData["ErrorMessage"] = "An expense has already been captured for this approved requisition item.";
                return RedirectToPage(new { requisitionId = approvedLine.RequisitionId });
            }

            _context.tbl_Expenses.Add(BuildApprovedExpense(NewExpense, approvedLine));

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "An expense has already been captured for this approved requisition item.";
                return RedirectToPage(new { requisitionId = approvedLine.RequisitionId });
            }

            TempData["SuccessMessage"] = $"Expense captured from approved requisition {approvedLine.RequestNumber}.";
            return RedirectToPage(new { requisitionId = approvedLine.RequisitionId });
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var expense = await _context.tbl_Expenses.FindAsync(EditExpense.ExpenseId);
            if (expense == null)
            {
                TempData["ErrorMessage"] = "The expense record could not be found.";
                return RedirectToPage();
            }

            if (expense.sys_jobId.HasValue)
            {
                TempData["ErrorMessage"] = "Construction costs must be maintained from the related job payment screen.";
                return RedirectToPage();
            }

            if (!expense.sys_requisitionLineId.HasValue)
            {
                TempData["ErrorMessage"] = "Only expenses linked to an approved requisition can be maintained here.";
                return RedirectToPage();
            }

            // The source line is never taken from the edit form. This prevents an
            // update request from being repointed to another approved item.
            EditExpense.RequisitionLineId = expense.sys_requisitionLineId.Value;

            var validationError = await ValidateInputAsync(EditExpense);
            if (validationError != null)
            {
                TempData["ErrorMessage"] = validationError;
                return RedirectToPage();
            }

            ApplyExpenseDetails(EditExpense, expense);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Approved requisition expense details updated successfully.";
            return RedirectToPage(new { requisitionId = expense.sys_requisitionId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int expenseId)
        {
            if (!User.IsInRole("Manager"))
            {
                return Forbid();
            }

            var expense = await _context.tbl_Expenses.FindAsync(expenseId);
            if (expense == null)
            {
                TempData["ErrorMessage"] = "The expense record could not be found.";
                return RedirectToPage();
            }

            if (expense.sys_jobId.HasValue)
            {
                TempData["ErrorMessage"] = "Construction costs cannot be deleted here. Use the related job payment workflow.";
                return RedirectToPage();
            }

            if (expense.sys_requisitionLineId.HasValue)
            {
                TempData["ErrorMessage"] = "Approved requisition expenses are retained for audit purposes and cannot be deleted.";
                return RedirectToPage(new { requisitionId = expense.sys_requisitionId });
            }

            _context.tbl_Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Daily expense deleted.";
            return RedirectToPage();
        }

        private async Task LoadPageAsync()
        {
            ExpenseCategories = await _context.tbl_ExpenseCategories
                .AsNoTracking()
                .OrderBy(x => x.sys_displayOrder)
                .ThenBy(x => x.sys_expenseCategoryName)
                .ToListAsync();

            Suppliers = await _context.tbl_Suppliers
                .AsNoTracking()
                .OrderBy(x => x.sys_supplierName)
                .ToListAsync();

            PaymentMethods = await _context.tbl_PaymentMethods
                .AsNoTracking()
                .OrderBy(x => x.sys_paymentMethodName)
                .ToListAsync();

            var approvedLinesQuery =
                from line in _context.tbl_RequisitionLines.AsNoTracking()
                join requisition in _context.tbl_Requisitions.AsNoTracking() on line.RequisitionId equals requisition.Id
                where requisition.Status == RequisitionStatus.Approved
                    && !_context.tbl_Expenses.Any(expense => expense.sys_requisitionLineId == line.Id)
                select new ApprovedRequisitionLine
                {
                    LineId = line.Id,
                    RequisitionId = requisition.Id,
                    RequestNumber = requisition.RequestNumber,
                    Description = line.Description,
                    ApprovedQuantity = line.CurrentQuantity,
                    ApprovedUnitCost = line.CurrentUnitCost,
                    ApprovedAmount = (double)(line.CurrentQuantity * line.CurrentUnitCost)
                };

            if (RequisitionId.HasValue)
            {
                approvedLinesQuery = approvedLinesQuery.Where(x => x.RequisitionId == RequisitionId.Value);
            }

            ApprovedRequisitionLines = await approvedLinesQuery
                .OrderBy(x => x.RequestNumber)
                .ThenBy(x => x.LineId)
                .ToListAsync();

            var query =
                from expense in _context.tbl_Expenses.AsNoTracking()
                join supplier in _context.tbl_Suppliers.AsNoTracking() on expense.sys_supplierId equals (int?)supplier.sys_supplierId into supplierJoin
                from supplier in supplierJoin.DefaultIfEmpty()
                join paymentMethod in _context.tbl_PaymentMethods.AsNoTracking() on expense.sys_paymentMethodId equals paymentMethod.sys_paymentMethodId into paymentMethodJoin
                from paymentMethod in paymentMethodJoin.DefaultIfEmpty()
                join job in _context.tbl_Jobs.AsNoTracking() on expense.sys_jobId equals (int?)job.sys_jobId into jobJoin
                from job in jobJoin.DefaultIfEmpty()
                join grave in _context.tbl_Graves.AsNoTracking() on job.sys_graveId equals grave.sys_graveId into graveJoin
                from grave in graveJoin.DefaultIfEmpty()
                join requisition in _context.tbl_Requisitions.AsNoTracking() on expense.sys_requisitionId equals (int?)requisition.Id into requisitionJoin
                from requisition in requisitionJoin.DefaultIfEmpty()
                join requisitionLine in _context.tbl_RequisitionLines.AsNoTracking() on expense.sys_requisitionLineId equals (int?)requisitionLine.Id into requisitionLineJoin
                from requisitionLine in requisitionLineJoin.DefaultIfEmpty()
                select new ExpenseRow
                {
                    ExpenseId = expense.sys_expenseId,
                    Date = expense.sys_date,
                    Category = expense.sys_jobId.HasValue
                        ? "Construction"
                        : (string.IsNullOrWhiteSpace(expense.sys_expenseCategory) ? "Other" : expense.sys_expenseCategory),
                    Payee = !string.IsNullOrWhiteSpace(expense.sys_payeeName)
                        ? expense.sys_payeeName
                        : (supplier != null ? supplier.sys_supplierName : "Not specified"),
                    SupplierId = expense.sys_supplierId,
                    SupplierName = supplier != null ? supplier.sys_supplierName : string.Empty,
                    Reference = expense.sys_reference ?? string.Empty,
                    Notes = expense.sys_notes ?? string.Empty,
                    PaymentMethodId = expense.sys_paymentMethodId,
                    PaymentMethod = paymentMethod != null ? paymentMethod.sys_paymentMethodName : "Not specified",
                    AmountPaid = expense.sys_amountPaid,
                    IsConstructionCost = expense.sys_jobId.HasValue,
                    JobId = expense.sys_jobId,
                    PlotNumber = grave != null ? grave.sys_plotNumber : string.Empty,
                    IsRequisitionExpense = expense.sys_requisitionLineId.HasValue,
                    RequisitionId = expense.sys_requisitionId,
                    RequisitionNumber = requisition != null ? requisition.RequestNumber : string.Empty,
                    RequisitionLineDescription = requisitionLine != null ? requisitionLine.Description : string.Empty
                };

            if (FromDate.HasValue)
            {
                query = query.Where(x => x.Date >= FromDate.Value);
            }

            if (ToDate.HasValue)
            {
                query = query.Where(x => x.Date <= ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "All")
            {
                query = query.Where(x => x.Category == CategoryFilter);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var search = SearchTerm.Trim();
                query = query.Where(x => x.Payee.Contains(search) ||
                                         x.Reference.Contains(search) ||
                                         x.Notes.Contains(search) ||
                                         x.Category.Contains(search) ||
                                         x.PlotNumber.Contains(search) ||
                                         x.RequisitionNumber.Contains(search) ||
                                         x.RequisitionLineDescription.Contains(search));
            }

            TotalRecords = await query.CountAsync();
            TotalExpenses = await query.SumAsync(x => (double?)x.AmountPaid) ?? 0d;
            DailyOperatingExpenses = await query
                .Where(x => !x.IsConstructionCost)
                .SumAsync(x => (double?)x.AmountPaid) ?? 0d;
            ConstructionExpenses = await query
                .Where(x => x.IsConstructionCost)
                .SumAsync(x => (double?)x.AmountPaid) ?? 0d;

            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalRecords / (double)DefaultPageSize));
            PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

            Expenses = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.ExpenseId)
                .Skip((PageNumber - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();
        }

        private void SetDefaultDateRange()
        {
            if (FromDate.HasValue || ToDate.HasValue)
            {
                return;
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            FromDate = new DateOnly(today.Year, today.Month, 1);
            ToDate = today;
        }

        private async Task<string?> ValidateInputAsync(RequisitionExpenseInput input)
        {
            if (input.ExpenseDate == default)
            {
                return "Expense date is required.";
            }

            if (input.RequisitionLineId <= 0)
            {
                return "Select an approved requisition item.";
            }

            if (!await _context.tbl_ExpenseCategories
                .AnyAsync(x => x.sys_expenseCategoryName == input.Category))
            {
                return "Select a valid expense category.";
            }

            if (string.IsNullOrWhiteSpace(input.PayeeName))
            {
                return "Payee or vendor is required.";
            }

            if (!await _context.tbl_PaymentMethods.AnyAsync(x => x.sys_paymentMethodId == input.PaymentMethodId))
            {
                return "Select a valid payment method.";
            }

            if (input.SupplierId.HasValue && !await _context.tbl_Suppliers.AnyAsync(x => x.sys_supplierId == input.SupplierId.Value))
            {
                return "The selected supplier could not be found.";
            }

            return null;
        }

        private async Task<ApprovedRequisitionLine?> LoadApprovedLineAsync(int requisitionLineId)
        {
            if (requisitionLineId <= 0)
            {
                return null;
            }

            return await (
                from line in _context.tbl_RequisitionLines.AsNoTracking()
                join requisition in _context.tbl_Requisitions.AsNoTracking() on line.RequisitionId equals requisition.Id
                where line.Id == requisitionLineId && requisition.Status == RequisitionStatus.Approved
                select new ApprovedRequisitionLine
                {
                    LineId = line.Id,
                    RequisitionId = requisition.Id,
                    RequestNumber = requisition.RequestNumber,
                    Description = line.Description,
                    ApprovedQuantity = line.CurrentQuantity,
                    ApprovedUnitCost = line.CurrentUnitCost,
                    ApprovedAmount = (double)(line.CurrentQuantity * line.CurrentUnitCost)
                }).SingleOrDefaultAsync();
        }

        private static tbl_Expenses BuildApprovedExpense(RequisitionExpenseInput input, ApprovedRequisitionLine approvedLine)
        {
            var expense = new tbl_Expenses
            {
                sys_requisitionId = approvedLine.RequisitionId,
                sys_requisitionLineId = approvedLine.LineId,
                sys_jobId = null,
                sys_amountDue = approvedLine.ApprovedAmount,
                sys_amountPaid = approvedLine.ApprovedAmount,
                sys_balanceDue = 0
            };

            ApplyExpenseDetails(input, expense);
            return expense;
        }

        private static void ApplyExpenseDetails(RequisitionExpenseInput input, tbl_Expenses expense)
        {
            expense.sys_date = input.ExpenseDate;
            expense.sys_supplierId = input.SupplierId;
            expense.sys_expenseCategory = input.Category.Trim();
            expense.sys_payeeName = input.PayeeName.Trim();
            expense.sys_reference = input.Reference?.Trim() ?? string.Empty;
            expense.sys_notes = input.Notes?.Trim() ?? string.Empty;
            expense.sys_paymentMethodId = input.PaymentMethodId;
        }

        public class RequisitionExpenseInput
        {
            public int ExpenseId { get; set; }

            [Required]
            public int RequisitionLineId { get; set; }

            [Required]
            public DateOnly ExpenseDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

            [Required]
            public string Category { get; set; } = string.Empty;

            [Required]
            public string PayeeName { get; set; } = string.Empty;

            public int? SupplierId { get; set; }

            [Required]
            public int PaymentMethodId { get; set; }

            public string? Reference { get; set; }
            public string? Notes { get; set; }
        }

        public class ExpenseRow
        {
            public int ExpenseId { get; set; }
            public DateOnly Date { get; set; }
            public string Category { get; set; } = string.Empty;
            public string Payee { get; set; } = string.Empty;
            public int? SupplierId { get; set; }
            public string SupplierName { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public int PaymentMethodId { get; set; }
            public string PaymentMethod { get; set; } = string.Empty;
            public double AmountPaid { get; set; }
            public bool IsConstructionCost { get; set; }
            public int? JobId { get; set; }
            public string PlotNumber { get; set; } = string.Empty;
            public bool IsRequisitionExpense { get; set; }
            public int? RequisitionId { get; set; }
            public string RequisitionNumber { get; set; } = string.Empty;
            public string RequisitionLineDescription { get; set; } = string.Empty;
        }

        public class ApprovedRequisitionLine
        {
            public int LineId { get; set; }
            public int RequisitionId { get; set; }
            public string RequestNumber { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public decimal ApprovedQuantity { get; set; }
            public decimal ApprovedUnitCost { get; set; }
            public double ApprovedAmount { get; set; }
        }
    }
}
