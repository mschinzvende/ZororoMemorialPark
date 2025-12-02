using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    public class StockMovementModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly StockMovementPdfService _pdfService;

        public StockMovementModel(ApplicationDbContext context, StockMovementPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // Filters
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedItemId { get; set; }     // filter by product
        public string SelectedMovementType { get; set; } = "All"; // All | Purchases | Sales | JobUsage | Adjustments | In | Out

        // Dropdown data
        public List<tbl_ProductsAndServices> Products { get; set; } = new();

        // Summary
        public double TotalQuantityIn { get; set; }
        public double TotalQuantityOut { get; set; }
        public double NetQuantity => TotalQuantityIn - TotalQuantityOut;

        public double PurchaseQty { get; set; }
        public double SaleQty { get; set; }
        public double JobUsageQty { get; set; }
        public double AdjustmentQty { get; set; }

        // Detailed table rows
        public List<StockMovementRow> Rows { get; set; } = new();

        // Chart data
        public List<MovementByDateDto> MovementByDate { get; set; } = new();
        public List<MovementByItemDto> MovementByItem { get; set; } = new();

        public async Task OnGetAsync()
        {
            ParseFiltersFromQuery();
            await LoadDropdownsAsync();
            await LoadDataAsync();
        }

        // PDF export
        public async Task<IActionResult> OnGetPdfAsync()
        {
            ParseFiltersFromQuery();
            await LoadDropdownsAsync();
            await LoadDataAsync();

            // Company info – adjust to your actual configs
            string companyName = "Zororo Memorial Park";
            string companyAddress = "Harare, Zimbabwe";
            string companyPhone = "+263 000 000 000";
            string companyEmail = "info@zororo.co.zw";
            string logoPath = null; // e.g. Path.Combine(env.WebRootPath, "images", "logo.png")

            var pdfBytes = _pdfService.GenerateStockMovementPdf(
                rows: Rows,
                fromDate: FromDate,
                toDate: ToDate,
                selectedMovementType: SelectedMovementType,
                totalIn: TotalQuantityIn,
                totalOut: TotalQuantityOut,
                netQuantity: NetQuantity,
                purchaseQty: PurchaseQty,
                saleQty: SaleQty,
                jobUsageQty: JobUsageQty,
                adjustmentQty: AdjustmentQty,
                companyName: companyName,
                companyAddress: companyAddress,
                companyPhone: companyPhone,
                companyEmail: companyEmail,
                logoPath: logoPath
            );

            string filename = $"StockMovement_{DateTime.Now:yyyyMMddHHmm}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }

        private void ParseFiltersFromQuery()
        {
            // Movement type
            var mt = Request.Query["movementType"].ToString();
            SelectedMovementType = string.IsNullOrWhiteSpace(mt) ? "All" : mt;

            // Item
            var itemStr = Request.Query["itemId"].ToString();
            if (int.TryParse(itemStr, out int itemId))
                SelectedItemId = itemId;
            else
                SelectedItemId = null;

            // Date range
            var fromStr = Request.Query["from"].ToString();
            var toStr = Request.Query["to"].ToString();

            if (DateTime.TryParse(fromStr, out var fromDt))
                FromDate = fromDt.Date;
            else
                FromDate = null;

            if (DateTime.TryParse(toStr, out var toDt))
                ToDate = toDt.Date.AddDays(1).AddTicks(-1); // include whole day
            else
                ToDate = null;

            // Default to current month if nothing specified
            if (!FromDate.HasValue && !ToDate.HasValue)
            {
                var today = DateTime.Today;
                FromDate = new DateTime(today.Year, today.Month, 1);
                ToDate = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month))
                    .AddDays(1).AddTicks(-1);
            }
        }

        private async Task LoadDropdownsAsync()
        {
            Products = await _context.tbl_ProductsAndServices
                .Where(p => p.sys_isActive && p.sys_itemType == "Product")
                .OrderBy(p => p.sys_itemName)
                .AsNoTracking()
                .ToListAsync();
        }

        private async Task LoadDataAsync()
        {
            // --- 1) STOCK TRANSACTIONS (Purchases, Sales, Consumption, Adjustments) ---

            var txQuery =
                from t in _context.tbl_StockTransactions
                join p in _context.tbl_ProductsAndServices on t.sys_itemId equals p.sys_itemId
                where p.sys_isActive && p.sys_itemType == "Product"
                select new { t, p };

            if (FromDate.HasValue)
                txQuery = txQuery.Where(x => x.t.sys_transactionDate >= FromDate.Value);
            if (ToDate.HasValue)
                txQuery = txQuery.Where(x => x.t.sys_transactionDate <= ToDate.Value);

            if (SelectedItemId.HasValue)
                txQuery = txQuery.Where(x => x.p.sys_itemId == SelectedItemId.Value);

            var txList = await txQuery.AsNoTracking().ToListAsync();

            // --- 2) JOB USAGE (tbl_JobEvaluationProductUsage) ---

            var usageQuery =
                from u in _context.tbl_JobEvaluationProductUsage
                join p in _context.tbl_ProductsAndServices on u.sys_itemId equals p.sys_itemId
                where p.sys_isActive && p.sys_itemType == "Product"
                select new { u, p };

            if (FromDate.HasValue)
                usageQuery = usageQuery.Where(x => x.u.sys_dateUsed >= FromDate.Value);
            if (ToDate.HasValue)
                usageQuery = usageQuery.Where(x => x.u.sys_dateUsed <= ToDate.Value);

            if (SelectedItemId.HasValue)
                usageQuery = usageQuery.Where(x => x.p.sys_itemId == SelectedItemId.Value);

            var usageList = await usageQuery.AsNoTracking().ToListAsync();

            var rows = new List<StockMovementRow>();

            // --- Map StockTransactions -> rows ---
            foreach (var x in txList)
            {
                var r = new StockMovementRow
                {
                    Date = x.t.sys_transactionDate,
                    ItemId = x.p.sys_itemId,
                    ItemName = x.p.sys_itemName,
                    UnitCost = x.t.sys_unitCost,
                    Reference = x.t.sys_referenceNumber,
                    Notes = x.t.sys_notes
                };

                // Movement type & direction
                switch (x.t.sys_transactionType)
                {
                    case "Purchase":
                        r.MovementType = "Purchase";
                        r.QuantityIn = x.t.sys_quantity;
                        PurchaseQty += x.t.sys_quantity;
                        break;

                    case "Sale":
                        r.MovementType = "Sale";
                        r.QuantityOut = x.t.sys_quantity;
                        SaleQty += x.t.sys_quantity;
                        break;

                    case "Consumption":
                        r.MovementType = "Consumption";
                        r.QuantityOut = x.t.sys_quantity;
                        // treat as "Adjustment" bucket or its own if you like
                        AdjustmentQty += x.t.sys_quantity;
                        break;

                    case "Adjustment":
                        r.MovementType = "Adjustment";
                        // Depending on sign of quantity you can treat as in/out
                        if (x.t.sys_quantity >= 0)
                            r.QuantityIn = x.t.sys_quantity;
                        else
                            r.QuantityOut = Math.Abs(x.t.sys_quantity);
                        AdjustmentQty += Math.Abs(x.t.sys_quantity);
                        break;

                    default:
                        r.MovementType = x.t.sys_transactionType ?? "Other";
                        break;
                }

                r.TotalCost = r.UnitCost * (r.QuantityIn > 0 ? r.QuantityIn : r.QuantityOut);
                rows.Add(r);
            }

            // --- Map JobUsage -> rows ---
            foreach (var x in usageList)
            {
                var r = new StockMovementRow
                {
                    Date = x.u.sys_dateUsed,
                    ItemId = x.p.sys_itemId,
                    ItemName = x.p.sys_itemName,
                    MovementType = "Job Usage",
                    QuantityOut = x.u.sys_quantityUsed,
                    UnitCost = x.p.sys_unitCost,
                    Reference = $"Job #{x.u.sys_jobId}",
                    Notes = $"Eval Param {x.u.sys_jobEvaluationParameterId}"
                };

                r.TotalCost = r.UnitCost * r.QuantityOut;
                rows.Add(r);
                JobUsageQty += x.u.sys_quantityUsed;
            }

            // --- Apply movement-type filter (All | Purchases | Sales | JobUsage | Adjustments | In | Out) ---

            if (SelectedMovementType == "Purchases")
                rows = rows.Where(r => r.MovementType == "Purchase").ToList();
            else if (SelectedMovementType == "Sales")
                rows = rows.Where(r => r.MovementType == "Sale").ToList();
            else if (SelectedMovementType == "JobUsage")
                rows = rows.Where(r => r.MovementType == "Job Usage").ToList();
            else if (SelectedMovementType == "Adjustments")
                rows = rows.Where(r => r.MovementType == "Adjustment" || r.MovementType == "Consumption").ToList();
            else if (SelectedMovementType == "In")
                rows = rows.Where(r => r.QuantityIn > 0).ToList();
            else if (SelectedMovementType == "Out")
                rows = rows.Where(r => r.QuantityOut > 0).ToList();
            // "All" = no extra filter

            // --- Summaries ---

            TotalQuantityIn = rows.Sum(r => r.QuantityIn);
            TotalQuantityOut = rows.Sum(r => r.QuantityOut);

            Rows = rows
                .OrderBy(r => r.Date)
                .ThenBy(r => r.ItemName)
                .ToList();

            // --- Chart data: by date ---

            MovementByDate = Rows
                .GroupBy(r => r.Date.Date)
                .Select(g => new MovementByDateDto
                {
                    Date = g.Key,
                    QuantityIn = g.Sum(x => x.QuantityIn),
                    QuantityOut = g.Sum(x => x.QuantityOut)
                })
                .OrderBy(x => x.Date)
                .ToList();

            // --- Chart data: by item (top moved) ---

            MovementByItem = Rows
                .GroupBy(r => r.ItemName)
                .Select(g => new MovementByItemDto
                {
                    ItemName = g.Key,
                    TotalMovement = g.Sum(x => x.QuantityIn + x.QuantityOut)
                })
                .OrderByDescending(x => x.TotalMovement)
                .Take(10)
                .ToList();
        }
    }

    public class StockMovementRow
    {
        public DateTime Date { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string MovementType { get; set; }
        public double QuantityIn { get; set; }
        public double QuantityOut { get; set; }
        public double UnitCost { get; set; }
        public double TotalCost { get; set; }
        public string Reference { get; set; }
        public string Notes { get; set; }
    }

    public class MovementByDateDto
    {
        public DateTime Date { get; set; }
        public double QuantityIn { get; set; }
        public double QuantityOut { get; set; }
    }

    public class MovementByItemDto
    {
        public string ItemName { get; set; }
        public double TotalMovement { get; set; }
    }
}
