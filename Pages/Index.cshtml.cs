using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;

namespace ZororoMemorialPark.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string CurrentMonthLabel { get; private set; } = string.Empty;
        public string DashboardRoleLabel { get; private set; } = "Workspace";

        // Manager-only financial position
        public decimal TotalInvoiced { get; private set; }
        public decimal TotalCollected { get; private set; }
        public decimal TotalOutstanding { get; private set; }
        public decimal ExpensesPaid { get; private set; }
        public decimal NetCashPosition { get; private set; }
        public string CashTrendLabelsJson { get; private set; } = "[]";
        public string CashCollectionTrendJson { get; private set; } = "[]";
        public string ExpensePaymentTrendJson { get; private set; } = "[]";

        // Operations workspace
        public int ActiveJobsCount { get; private set; }
        public int OverdueJobsCount { get; private set; }
        public int DiggingGravesCount { get; private set; }
        public int UnlinkedReservationsCount { get; private set; }
        public int UpcomingBurialsCount { get; private set; }
        public string OperationsStageLabelsJson { get; private set; } = "[]";
        public string OperationsStageCountsJson { get; private set; } = "[]";
        public string OperationsJobStatusLabelsJson { get; private set; } = "[]";
        public string OperationsJobStatusCountsJson { get; private set; } = "[]";

        // Finance workspace: collections and follow-up, not the overall financial position.
        public decimal CollectedThisMonth { get; private set; }
        public int OverdueInvoicesCount { get; private set; }
        public int InstallmentsDueCount { get; private set; }
        public string ReceivableAgingLabelsJson { get; private set; } = "[]";
        public string ReceivableAgingValuesJson { get; private set; } = "[]";

        // Agent workspace
        public bool HasLinkedAgentProfile { get; private set; }
        public int MyBookingsThisMonth { get; private set; }
        public int MyAwaitingPaymentCount { get; private set; }
        public int MyReadyForAllocationCount { get; private set; }
        public int MyUpcomingBurialsCount { get; private set; }
        public string MyBookingStatusLabelsJson { get; private set; } = "[]";
        public string MyBookingStatusCountsJson { get; private set; } = "[]";

        public async Task OnGetAsync()
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var monthStart = new DateOnly(today.Year, today.Month, 1);
            var nextMonthStart = monthStart.AddMonths(1);

            CurrentMonthLabel = DateTime.Today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

            // A user can belong to more than one legacy role. Manager is intentionally
            // evaluated first so the restricted executive view is the only one rendered.
            if (User.IsInRole("Manager"))
            {
                DashboardRoleLabel = "Executive overview";
                await LoadManagerDashboardAsync(today, monthStart, nextMonthStart);
                return;
            }

            if (User.IsInRole("OperationsOfficer"))
            {
                DashboardRoleLabel = "Operations control centre";
                await LoadOperationsDashboardAsync(today);
                return;
            }

            if (User.IsInRole("FinanceOfficer"))
            {
                DashboardRoleLabel = "Collections and receivables";
                await LoadFinanceDashboardAsync(today, monthStart, nextMonthStart);
                return;
            }

            if (User.IsInRole("Agent"))
            {
                DashboardRoleLabel = "My booking desk";
                await LoadAgentDashboardAsync(today, monthStart, nextMonthStart);
                return;
            }

            DashboardRoleLabel = "Cemetery workspace";
        }

        private async Task LoadManagerDashboardAsync(DateOnly today, DateOnly monthStart, DateOnly nextMonthStart)
        {
            var invoiceBalances = await LoadInvoiceBalancesAsync();

            TotalInvoiced = invoiceBalances.Sum(x => x.Total);
            TotalOutstanding = invoiceBalances.Sum(x => x.Outstanding);
            var tombstoneInvoiceIds = await _context.tbl_TombstoneSales
                .AsNoTracking()
                .Select(x => x.sys_invoiceId)
                .Distinct()
                .ToListAsync();

            var invoiceCollections = await _context.tbl_IncomePayments
                .AsNoTracking()
                .Where(x => !tombstoneInvoiceIds.Contains(x.sys_invoiceId))
                .SumAsync(x => (decimal?)x.sys_amount) ?? 0m;
            var tombstoneCollections = await _context.tbl_TombstoneSales
                .AsNoTracking()
                .SumAsync(x => (decimal?)x.sys_amountPaid) ?? 0m;

            // Tombstone sales manage their own paid value; excluding their invoice-linked
            // payments here prevents that cash from being counted twice.
            TotalCollected = invoiceCollections + tombstoneCollections;
            ExpensesPaid = await _context.tbl_Expenses
                .AsNoTracking()
                .SumAsync(x => (decimal?)x.sys_amountPaid) ?? 0m;
            NetCashPosition = TotalCollected - ExpensesPaid;

            var firstMonth = monthStart.AddMonths(-5);
            var monthKeys = Enumerable.Range(0, 6)
                .Select(offset => firstMonth.AddMonths(offset))
                .ToList();

            var collectionByMonth = await _context.tbl_IncomePayments
                .AsNoTracking()
                .Where(x => x.sys_date >= firstMonth && x.sys_date < nextMonthStart && !tombstoneInvoiceIds.Contains(x.sys_invoiceId))
                .GroupBy(x => new { x.sys_date.Year, x.sys_date.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Amount = group.Sum(x => (decimal)x.sys_amount)
                })
                .ToListAsync();

            var tombstoneCollectionsByMonth = await _context.tbl_TombstoneSales
                .AsNoTracking()
                .Where(x => x.sys_saleDate >= firstMonth && x.sys_saleDate < nextMonthStart)
                .GroupBy(x => new { x.sys_saleDate.Year, x.sys_saleDate.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Amount = group.Sum(x => (decimal)x.sys_amountPaid)
                })
                .ToListAsync();

            var supplierPaymentsByMonth = await _context.tbl_Expenses
                .AsNoTracking()
                .Where(x => x.sys_date >= firstMonth && x.sys_date < nextMonthStart)
                .GroupBy(x => new { x.sys_date.Year, x.sys_date.Month })
                .Select(group => new
                {
                    group.Key.Year,
                    group.Key.Month,
                    Amount = group.Sum(x => (decimal)x.sys_amountPaid)
                })
                .ToListAsync();

            var collectionMap = collectionByMonth.ToDictionary(x => (x.Year, x.Month), x => x.Amount);
            foreach (var tombstoneCollection in tombstoneCollectionsByMonth)
            {
                collectionMap[(tombstoneCollection.Year, tombstoneCollection.Month)] =
                    collectionMap.GetValueOrDefault((tombstoneCollection.Year, tombstoneCollection.Month)) + tombstoneCollection.Amount;
            }

            var expensePaymentMap = supplierPaymentsByMonth.ToDictionary(x => (x.Year, x.Month), x => x.Amount);

            CashTrendLabelsJson = JsonSerializer.Serialize(monthKeys.Select(x => x.ToString("MMM", CultureInfo.InvariantCulture)));
            CashCollectionTrendJson = JsonSerializer.Serialize(monthKeys.Select(x => collectionMap.GetValueOrDefault((x.Year, x.Month))));
            ExpensePaymentTrendJson = JsonSerializer.Serialize(monthKeys.Select(x => expensePaymentMap.GetValueOrDefault((x.Year, x.Month))));
        }

        private async Task LoadOperationsDashboardAsync(DateOnly today)
        {
            var activeJobs = _context.tbl_Jobs
                .AsNoTracking()
                .Where(x => x.sys_jobStatus == "Pending" || x.sys_jobStatus == "In Progress");

            ActiveJobsCount = await activeJobs.CountAsync();
            OverdueJobsCount = await activeJobs.CountAsync(x => x.sys_endDate < today);
            DiggingGravesCount = await (
                from job in activeJobs
                join stage in _context.tbl_DevelopmentStages.AsNoTracking() on job.sys_stageId equals stage.sys_stageId
                where EF.Functions.Like(stage.sys_stageName, "%dig%")
                select job.sys_graveId
            ).Distinct().CountAsync();

            UnlinkedReservationsCount = await _context.tbl_GraveReservations
                .AsNoTracking()
                .CountAsync(x => x.sys_status == "Active" && !x.sys_graveId.HasValue);

            var nextWeek = DateTime.Today.AddDays(7);
            UpcomingBurialsCount = await _context.tbl_BurialBookings
                .AsNoTracking()
                .CountAsync(x => x.sys_requestedBurialDateTime >= DateTime.Today &&
                                 x.sys_requestedBurialDateTime < nextWeek &&
                                 x.sys_bookingStatus != "Cancelled" &&
                                 x.sys_bookingStatus != "Rejected" &&
                                 x.sys_bookingStatus != "Expired");

            var jobsByStage = await (
                from job in activeJobs
                join stage in _context.tbl_DevelopmentStages.AsNoTracking() on job.sys_stageId equals stage.sys_stageId
                group job by stage.sys_stageName into stageGroup
                orderby stageGroup.Key
                select new { Name = stageGroup.Key, Count = stageGroup.Count() }
            ).ToListAsync();

            var jobsByStatus = await activeJobs
                .GroupBy(x => x.sys_jobStatus)
                .Select(group => new { Name = group.Key, Count = group.Count() })
                .ToListAsync();

            OperationsStageLabelsJson = JsonSerializer.Serialize(jobsByStage.Select(x => x.Name));
            OperationsStageCountsJson = JsonSerializer.Serialize(jobsByStage.Select(x => x.Count));
            OperationsJobStatusLabelsJson = JsonSerializer.Serialize(jobsByStatus.Select(x => x.Name));
            OperationsJobStatusCountsJson = JsonSerializer.Serialize(jobsByStatus.Select(x => x.Count));
        }

        private async Task LoadFinanceDashboardAsync(DateOnly today, DateOnly monthStart, DateOnly nextMonthStart)
        {
            var invoiceBalances = await LoadInvoiceBalancesAsync();

            TotalOutstanding = invoiceBalances.Sum(x => x.Outstanding);
            OverdueInvoicesCount = invoiceBalances.Count(x => x.Outstanding > 0 && x.DueDate < today);
            CollectedThisMonth = await _context.tbl_IncomePayments
                .AsNoTracking()
                .Where(x => x.sys_date >= monthStart && x.sys_date < nextMonthStart)
                .SumAsync(x => (decimal?)x.sys_amount) ?? 0m;
            InstallmentsDueCount = await _context.tbl_GraveReservations
                .AsNoTracking()
                .CountAsync(x => x.sys_status == "Active" &&
                                 x.sys_nextInstallmentDueDate.HasValue &&
                                 x.sys_nextInstallmentDueDate.Value <= today.AddDays(7));

            var current = invoiceBalances.Where(x => x.Outstanding > 0 && x.DueDate >= today).Sum(x => x.Outstanding);
            var oneToThirty = invoiceBalances.Where(x => x.Outstanding > 0 && x.DueDate < today && x.DueDate >= today.AddDays(-30)).Sum(x => x.Outstanding);
            var thirtyOneToSixty = invoiceBalances.Where(x => x.Outstanding > 0 && x.DueDate < today.AddDays(-30) && x.DueDate >= today.AddDays(-60)).Sum(x => x.Outstanding);
            var overSixty = invoiceBalances.Where(x => x.Outstanding > 0 && x.DueDate < today.AddDays(-60)).Sum(x => x.Outstanding);

            ReceivableAgingLabelsJson = JsonSerializer.Serialize(new[] { "Current", "1–30 days", "31–60 days", "Over 60 days" });
            ReceivableAgingValuesJson = JsonSerializer.Serialize(new[] { current, oneToThirty, thirtyOneToSixty, overSixty });
        }

        private async Task LoadAgentDashboardAsync(DateOnly today, DateOnly monthStart, DateOnly nextMonthStart)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var agentId = await _context.tbl_Agents
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.sys_isActive)
                .Select(x => (int?)x.sys_agentId)
                .FirstOrDefaultAsync();

            if (!agentId.HasValue)
            {
                return;
            }

            HasLinkedAgentProfile = true;
            var myBookings = _context.tbl_BurialBookings
                .AsNoTracking()
                .Where(x => x.sys_agentId == agentId.Value);

            MyBookingsThisMonth = await myBookings.CountAsync(x => x.sys_createdOn >= monthStart.ToDateTime(TimeOnly.MinValue) &&
                                                                  x.sys_createdOn < nextMonthStart.ToDateTime(TimeOnly.MinValue));
            MyAwaitingPaymentCount = await myBookings.CountAsync(x =>
                x.sys_bookingStatus == "PendingPayment" ||
                x.sys_bookingStatus == "Invoiced" ||
                x.sys_bookingStatus == "Partialy Paid" ||
                x.sys_bookingStatus == "PaidPartial");

            var allocatedBookingIds = _context.tbl_GraveAllocations
                .AsNoTracking()
                .Select(x => x.sys_bookingId);
            MyReadyForAllocationCount = await myBookings.CountAsync(x =>
                (x.sys_bookingStatus == "Paid in Full" || x.sys_bookingStatus == "PaidFull") &&
                !allocatedBookingIds.Contains(x.sys_bookingId));

            var nextWeek = DateTime.Today.AddDays(7);
            MyUpcomingBurialsCount = await myBookings.CountAsync(x => x.sys_requestedBurialDateTime >= DateTime.Today &&
                                                                       x.sys_requestedBurialDateTime < nextWeek &&
                                                                       x.sys_bookingStatus != "Cancelled" &&
                                                                       x.sys_bookingStatus != "Rejected" &&
                                                                       x.sys_bookingStatus != "Expired");

            var bookingStatusSummary = await myBookings
                .Where(x => x.sys_bookingStatus != "Cancelled" && x.sys_bookingStatus != "Rejected" && x.sys_bookingStatus != "Expired")
                .GroupBy(x => x.sys_bookingStatus)
                .Select(group => new { Name = group.Key, Count = group.Count() })
                .OrderBy(x => x.Name)
                .ToListAsync();

            MyBookingStatusLabelsJson = JsonSerializer.Serialize(bookingStatusSummary.Select(x => x.Name));
            MyBookingStatusCountsJson = JsonSerializer.Serialize(bookingStatusSummary.Select(x => x.Count));
        }

        private async Task<List<InvoiceBalanceMetric>> LoadInvoiceBalancesAsync()
        {
            var invoiceTotals = await _context.tbl_InvoiceItems
                .AsNoTracking()
                .GroupBy(x => x.sys_invoiceId)
                .Select(group => new
                {
                    InvoiceId = group.Key,
                    Total = group.Sum(x => (decimal)x.sys_itemCost)
                })
                .ToListAsync();

            var payments = await _context.tbl_IncomePayments
                .AsNoTracking()
                .GroupBy(x => x.sys_invoiceId)
                .Select(group => new
                {
                    InvoiceId = group.Key,
                    Paid = group.Sum(x => (decimal)x.sys_amount)
                })
                .ToListAsync();

            var invoices = await _context.tbl_Invoices
                .AsNoTracking()
                .Select(x => new { x.sys_invoiceId, x.sys_invoiceDueDate })
                .ToListAsync();

            var totalByInvoice = invoiceTotals.ToDictionary(x => x.InvoiceId, x => x.Total);
            var paidByInvoice = payments.ToDictionary(x => x.InvoiceId, x => x.Paid);

            return invoices
                .Where(x => totalByInvoice.ContainsKey(x.sys_invoiceId))
                .Select(x =>
                {
                    var total = totalByInvoice[x.sys_invoiceId];
                    var paid = paidByInvoice.GetValueOrDefault(x.sys_invoiceId);
                    return new InvoiceBalanceMetric
                    {
                        DueDate = x.sys_invoiceDueDate,
                        Total = total,
                        Paid = paid,
                        Outstanding = Math.Max(0m, total - paid)
                    };
                })
                .ToList();
        }

        private sealed class InvoiceBalanceMetric
        {
            public DateOnly DueDate { get; init; }
            public decimal Total { get; init; }
            public decimal Paid { get; init; }
            public decimal Outstanding { get; init; }
        }
    }
}
