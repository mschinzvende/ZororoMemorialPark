using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Services;

namespace ZororoMemorialPark.Pages.Reports
{
    public class RevenueReportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly RevenueReportPdfService _pdfService;

        public RevenueReportModel(ApplicationDbContext context, RevenueReportPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Filters (from querystring)
        [BindProperty(SupportsGet = true)]
        public DateOnly? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ToDate { get; set; }

        // Summary
        public RevenueSummaryDto Summary { get; set; } = new();

        // Daily totals (for table + chart)
        public List<RevenueByDateDto> DailyTotals { get; set; } = new();

        // For tables
        public List<IncomeLineDto> AllocationPayments { get; set; } = new();
        public List<IncomeLineDto> ReservationPayments { get; set; } = new();
        public List<IncomeLineDto> OtherPayments { get; set; } = new();
        public List<TombstoneRevenueLineDto> TombstonePayments { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Default: last 30 days if no filters
            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                ToDate = today;
                FromDate = today.AddDays(-30);
            }

            // === Base IncomePayments query ===
            var incomeQuery = _context.tbl_IncomePayments.AsQueryable();

            if (FromDate.HasValue)
                incomeQuery = incomeQuery.Where(p => p.sys_date >= FromDate.Value);
            if (ToDate.HasValue)
                incomeQuery = incomeQuery.Where(p => p.sys_date <= ToDate.Value);

            // Pre-load allocation & reservation invoice IDs
            var allocationInvoiceIds = await _context.tbl_GraveAllocationInvoices
                .Select(x => x.sys_invoiceId)
                .Distinct()
                .ToListAsync();

            var reservationInvoiceIds = await _context.tbl_GraveReservationInvoices
                .Select(x => x.sys_invoiceId)
                .Distinct()
                .ToListAsync();

            // Materialise payments for classification
            var incomePayments = await incomeQuery.AsNoTracking().ToListAsync();

            // Classify IncomePayments
            AllocationPayments = incomePayments
                .Where(p => allocationInvoiceIds.Contains(p.sys_invoiceId))
                .Select(p => new IncomeLineDto
                {
                    Date = p.sys_date,
                    Amount = p.sys_amount,
                    PaymentMethod = p.sys_paymentMethod,
                    Notes = p.sys_notes,
                    InvoiceId = p.sys_invoiceId
                   // ReceiptNumber = p.sys_receiptNumber
                })
                .ToList();

            ReservationPayments = incomePayments
                .Where(p => !allocationInvoiceIds.Contains(p.sys_invoiceId) &&
                            reservationInvoiceIds.Contains(p.sys_invoiceId))
                .Select(p => new IncomeLineDto
                {
                    Date = p.sys_date,
                    Amount = p.sys_amount,
                    PaymentMethod = p.sys_paymentMethod,
                    Notes = p.sys_notes,
                    InvoiceId = p.sys_invoiceId
                   // ReceiptNumber = p.sys_receiptNumber
                })
                .ToList();

            // "Other" = income payments not linked to allocation or reservation
            var knownInvoiceIds = allocationInvoiceIds
                .Concat(reservationInvoiceIds)
                .Distinct()
                .ToHashSet();

            OtherPayments = incomePayments
                .Where(p => !knownInvoiceIds.Contains(p.sys_invoiceId))
                .Select(p => new IncomeLineDto
                {
                    Date = p.sys_date,
                    Amount = p.sys_amount,
                    PaymentMethod = p.sys_paymentMethod,
                    Notes = p.sys_notes,
                    InvoiceId = p.sys_invoiceId
                   // ReceiptNumber = p.sys_receiptNumber
                })
                .ToList();

            // === Tombstone Revenue (tbl_TombstoneSales) ===
            var tombQuery = _context.tbl_TombstoneSales.AsQueryable();

            if (FromDate.HasValue)
                tombQuery = tombQuery.Where(t => t.sys_saleDate >= FromDate.Value);
            if (ToDate.HasValue)
                tombQuery = tombQuery.Where(t => t.sys_saleDate <= ToDate.Value);

            var tombstones = await tombQuery.AsNoTracking().ToListAsync();

            TombstonePayments = tombstones
                .Select(t => new TombstoneRevenueLineDto
                {
                    SaleDate = t.sys_saleDate,
                    AmountPaid = t.sys_amountPaid,
                    BuyerName = t.sys_buyerName,
                    BuyerContact = t.sys_buyerContact,
                    PaymentStatus = t.sys_paymentStatus,
                    InvoiceId = t.sys_invoiceId
                })
                .ToList();

            // === Totals ===
            double graveSalesRevenue = AllocationPayments.Sum(x => x.Amount);
            double reservationsRevenue = ReservationPayments.Sum(x => x.Amount);
            double otherRevenue = OtherPayments.Sum(x => x.Amount);
            double tombstoneRevenue = TombstonePayments.Sum(x => x.AmountPaid);

            Summary = new RevenueSummaryDto
            {
                GraveSalesRevenue = graveSalesRevenue,
                ReservationsRevenue = reservationsRevenue,
                OtherRevenue = otherRevenue,
                TombstoneRevenue = tombstoneRevenue
            };

            Summary.TotalRevenue =
                Summary.GraveSalesRevenue +
                Summary.ReservationsRevenue +
                Summary.OtherRevenue +
                Summary.TombstoneRevenue;

            // === Daily totals (for charts + table) ===

            var dailyIncomeTotals = incomePayments
                .GroupBy(p => p.sys_date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(x => x.sys_amount)
                })
                .ToList();

            var dailyTombTotals = tombstones
                .GroupBy(t => t.sys_saleDate)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(x => x.sys_amountPaid)
                })
                .ToList();

            var allDates = dailyIncomeTotals.Select(d => d.Date)
                .Union(dailyTombTotals.Select(t => t.Date))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            DailyTotals = allDates
                .Select(d =>
                {
                    var income = dailyIncomeTotals.FirstOrDefault(x => x.Date == d)?.Amount ?? 0;
                    var tomb = dailyTombTotals.FirstOrDefault(x => x.Date == d)?.Amount ?? 0;

                    return new RevenueByDateDto
                    {
                        Date = d,
                        TotalAmount = income + tomb
                    };
                })
                .ToList();
        }

        // === PDF Export ===
        public async Task<IActionResult> OnGetExportPdfAsync(DateOnly? fromDate, DateOnly? toDate)
        {
            FromDate = fromDate;
            ToDate = toDate;
            await OnGetAsync(); // reuse logic

            // In a real app, you might read these from config
            string companyName = "Zororo Memorial Park";
            string companyAddress = "Harare, Zimbabwe";
            string companyPhone = "+263 000000000";
            string companyEmail = "info@example.com";
            string logoPath = null; // or map a real path on the server

            var pdf = _pdfService.GenerateRevenuePdf(
                DailyTotals,
                Summary,
                AllocationPayments,
                ReservationPayments,
                TombstonePayments,
                OtherPayments,
                companyName,
                companyAddress,
                companyPhone,
                companyEmail,
                logoPath,
                FromDate,
                ToDate
            );

            string fileName = $"RevenueReport_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            return File(pdf, "application/pdf", fileName);
        }
    }

    // === DTOs ===

    public class RevenueSummaryDto
    {
        public double TotalRevenue { get; set; }
        public double GraveSalesRevenue { get; set; }
        public double ReservationsRevenue { get; set; }
        public double TombstoneRevenue { get; set; }
        public double OtherRevenue { get; set; }
    }

    public class RevenueByDateDto
    {
        public DateOnly Date { get; set; }
        public double TotalAmount { get; set; }
    }

    public class IncomeLineDto
    {
        public DateOnly Date { get; set; }
        public double Amount { get; set; }
        public string PaymentMethod { get; set; }
        public string Notes { get; set; }
        public int InvoiceId { get; set; }
        public int sys_receiptNumber { get; set; }
        public int ReceiptNumber => sys_receiptNumber;
    }

    public class TombstoneRevenueLineDto
    {
        public DateOnly SaleDate { get; set; }
        public double AmountPaid { get; set; }
        public string BuyerName { get; set; }
        public string BuyerContact { get; set; }
        public string PaymentStatus { get; set; }
        public int InvoiceId { get; set; }
    }
}
