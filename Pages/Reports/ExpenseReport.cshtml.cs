using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages.Reports
{
    [Authorize(Roles = "Manager,FinanceOfficer")]
    public class ExpenseReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ExpenseReportModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IReadOnlyList<string> ExpenseCategories { get; } = new[]
        {
            "Ground maintenance",
            "Equipment and fuel",
            "Utilities",
            "Office and administration",
            "Security",
            "Staff welfare",
            "Marketing",
            "Other",
            "Construction"
        };

        public DateOnly? FromDate { get; private set; }
        public DateOnly? ToDate { get; private set; }
        public string CategoryFilter { get; private set; } = "All";

        public double TotalExpenses { get; private set; }
        public double DailyOperatingExpenses { get; private set; }
        public double ConstructionExpenses { get; private set; }
        public double AverageDailyExpense { get; private set; }
        public List<ExpenseReportRow> Rows { get; private set; } = new();
        public List<string> CategoryChartLabels { get; private set; } = new();
        public List<double> CategoryChartValues { get; private set; } = new();

        public async Task OnGetAsync()
        {
            ParseFilters();
            await LoadReportAsync();
        }

        public async Task<IActionResult> OnGetCsvAsync()
        {
            ParseFilters();
            await LoadReportAsync();

            var csv = new StringBuilder();
            csv.AppendLine("Date,Category,Payee,Reference,Payment Method,Source,Amount Paid,Notes");
            foreach (var row in Rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(row.Date.ToString("yyyy-MM-dd")),
                    Csv(row.Category),
                    Csv(row.Payee),
                    Csv(row.Reference),
                    Csv(row.PaymentMethod),
                    Csv(row.Source),
                    Csv(row.AmountPaid.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)),
                    Csv(row.Notes)
                }));
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"ExpenseReport_{DateTime.Today:yyyyMMdd}.csv");
        }

        private void ParseFilters()
        {
            if (DateOnly.TryParse(Request.Query["from"], out var from))
            {
                FromDate = from;
            }

            if (DateOnly.TryParse(Request.Query["to"], out var to))
            {
                ToDate = to;
            }

            var requestedCategory = Request.Query["category"].ToString();
            CategoryFilter = ExpenseCategories.Contains(requestedCategory) ? requestedCategory : "All";

            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                FromDate = new DateOnly(today.Year, today.Month, 1);
                ToDate = today;
            }
        }

        private async Task LoadReportAsync()
        {
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
                select new ExpenseReportRow
                {
                    Date = expense.sys_date,
                    Category = expense.sys_jobId.HasValue
                        ? "Construction"
                        : (string.IsNullOrWhiteSpace(expense.sys_expenseCategory) ? "Other" : expense.sys_expenseCategory),
                    Payee = !string.IsNullOrWhiteSpace(expense.sys_payeeName)
                        ? expense.sys_payeeName
                        : (supplier != null ? supplier.sys_supplierName : "Not specified"),
                    Reference = expense.sys_reference ?? string.Empty,
                    Notes = expense.sys_notes ?? string.Empty,
                    PaymentMethod = paymentMethod != null ? paymentMethod.sys_paymentMethodName : "Not specified",
                    AmountPaid = expense.sys_amountPaid,
                    IsConstructionCost = expense.sys_jobId.HasValue,
                    IsRequisitionExpense = expense.sys_requisitionLineId.HasValue,
                    RequisitionId = expense.sys_requisitionId,
                    Source = expense.sys_requisitionLineId.HasValue
                        ? $"{(requisition != null ? requisition.RequestNumber : "Approved requisition")} · {(requisitionLine != null ? requisitionLine.Description : "approved item")}" 
                        : expense.sys_jobId.HasValue
                            ? $"Job #{expense.sys_jobId} · {(grave != null ? grave.sys_plotNumber : "grave development")}" 
                            : "Legacy entry"
                };

            if (FromDate.HasValue)
            {
                query = query.Where(x => x.Date >= FromDate.Value);
            }

            if (ToDate.HasValue)
            {
                query = query.Where(x => x.Date <= ToDate.Value);
            }

            if (CategoryFilter != "All")
            {
                query = query.Where(x => x.Category == CategoryFilter);
            }

            Rows = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.AmountPaid)
                .ToListAsync();

            TotalExpenses = Rows.Sum(x => x.AmountPaid);
            DailyOperatingExpenses = Rows.Where(x => !x.IsConstructionCost).Sum(x => x.AmountPaid);
            ConstructionExpenses = Rows.Where(x => x.IsConstructionCost).Sum(x => x.AmountPaid);
            var activeDays = Rows.Select(x => x.Date).Distinct().Count();
            AverageDailyExpense = activeDays == 0 ? 0 : TotalExpenses / activeDays;

            var categories = Rows
                .GroupBy(x => x.Category)
                .OrderByDescending(group => group.Sum(x => x.AmountPaid))
                .ToList();
            CategoryChartLabels = categories.Select(group => group.Key).ToList();
            CategoryChartValues = categories.Select(group => group.Sum(x => x.AmountPaid)).ToList();
        }

        private static string Csv(string? value)
        {
            return $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        }

        public class ExpenseReportRow
        {
            public DateOnly Date { get; set; }
            public string Category { get; set; } = string.Empty;
            public string Payee { get; set; } = string.Empty;
            public string Reference { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
            public string PaymentMethod { get; set; } = string.Empty;
            public double AmountPaid { get; set; }
            public bool IsConstructionCost { get; set; }
            public bool IsRequisitionExpense { get; set; }
            public int? RequisitionId { get; set; }
            public string Source { get; set; } = string.Empty;
        }
    }
}
