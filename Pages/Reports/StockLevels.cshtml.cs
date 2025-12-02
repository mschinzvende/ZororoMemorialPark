using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZororoMemorialPark.Data;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Pages.Reports
{
    public class StockLevelsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StockLevelsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Filters
        public string SelectedItemType { get; set; } = "Product";      // Product | Service | All
        public string SelectedCategory { get; set; } = "All";          // All | Sellable | Consumable
        public string SelectedStatus { get; set; } = "All";            // All | InStock | Low | Out
        public string SearchTerm { get; set; }

        // Threshold for low stock
        public int LowStockThreshold { get; set; } = 5;

        // Summary
        public int TotalItems { get; set; }
        public int TotalInStock { get; set; }
        public int TotalLowStock { get; set; }
        public int TotalOutOfStock { get; set; }
        public double TotalStockCost { get; set; }
        public double TotalStockPotentialRevenue { get; set; }

        // Table rows
        public List<StockRow> Rows { get; set; } = new();

        // Chart data
        public List<StockByCategoryDto> StockByCategory { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Read filters from query string
            SelectedItemType = Request.Query["itemType"].ToString();
            if (string.IsNullOrWhiteSpace(SelectedItemType))
                SelectedItemType = "Product";

            SelectedCategory = Request.Query["category"].ToString();
            if (string.IsNullOrWhiteSpace(SelectedCategory))
                SelectedCategory = "All";

            SelectedStatus = Request.Query["status"].ToString();
            if (string.IsNullOrWhiteSpace(SelectedStatus))
                SelectedStatus = "All";

            SearchTerm = Request.Query["search"].ToString() ?? string.Empty;

            int threshold;
            var thresholdStr = Request.Query["lowThreshold"].ToString();
            if (!string.IsNullOrWhiteSpace(thresholdStr) && int.TryParse(thresholdStr, out threshold))
                LowStockThreshold = threshold;

            // Base query: active items
            var query = _context.tbl_ProductsAndServices
                .AsNoTracking()
                .Where(p => p.sys_isActive);

            // Filter by item type
            if (SelectedItemType != "All")
            {
                query = query.Where(p => p.sys_itemType == SelectedItemType);
            }

            // Filter by category
            if (SelectedCategory == "Sellable" || SelectedCategory == "Consumable")
            {
                query = query.Where(p => p.sys_category == SelectedCategory);
            }

            var list = await query
                .OrderBy(p => p.sys_itemName)
                .ToListAsync();

            // Build rows in memory
            var rows = new List<StockRow>();

            foreach (var p in list)
            {
                string status;

                if (p.sys_itemType == "Product")
                {
                    if (p.sys_stockQuantity <= 0)
                        status = "Out of Stock";
                    else if (p.sys_stockQuantity <= LowStockThreshold)
                        status = "Low Stock";
                    else
                        status = "In Stock";
                }
                else
                {
                    // Services have no stock relevance
                    status = "Service";
                }

                var costValue = p.sys_stockQuantity * p.sys_unitCost;
                var revenueValue = p.sys_stockQuantity * p.sys_sellingPrice;

                rows.Add(new StockRow
                {
                    ItemId = p.sys_itemId,
                    ItemName = p.sys_itemName,
                    Description = p.sys_description,
                    ItemType = p.sys_itemType,
                    Category = p.sys_category,
                    UnitCost = p.sys_unitCost,
                    SellingPrice = p.sys_sellingPrice,
                    StockQuantity = p.sys_stockQuantity,
                    UnitOfMeasure = p.sys_unitOfMeasure,
                    Status = status,
                    StockValueCost = costValue,
                    StockValueRevenue = revenueValue
                });
            }

            // Apply search
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLower();
                rows = rows.Where(r =>
                        (!string.IsNullOrEmpty(r.ItemName) && r.ItemName.ToLower().Contains(term)) ||
                        (!string.IsNullOrEmpty(r.Category) && r.Category.ToLower().Contains(term)) ||
                        (!string.IsNullOrEmpty(r.ItemType) && r.ItemType.ToLower().Contains(term)))
                    .ToList();
            }

            // Apply status filter (only meaningful for products)
            if (SelectedStatus != "All")
            {
                if (SelectedStatus == "InStock")
                    rows = rows.Where(r => r.Status == "In Stock").ToList();
                else if (SelectedStatus == "Low")
                    rows = rows.Where(r => r.Status == "Low Stock").ToList();
                else if (SelectedStatus == "Out")
                    rows = rows.Where(r => r.Status == "Out of Stock").ToList();
            }

            Rows = rows;

            // Summary (only for Product rows)
            var productRows = Rows.Where(r => r.ItemType == "Product").ToList();

            TotalItems = productRows.Count;
            TotalInStock = productRows.Count(r => r.Status == "In Stock");
            TotalLowStock = productRows.Count(r => r.Status == "Low Stock");
            TotalOutOfStock = productRows.Count(r => r.Status == "Out of Stock");
            TotalStockCost = productRows.Sum(r => r.StockValueCost);
            TotalStockPotentialRevenue = productRows.Sum(r => r.StockValueRevenue);

            // Chart: stock by category (Sellable / Consumable / Other)
            StockByCategory = productRows
                .GroupBy(r => string.IsNullOrEmpty(r.Category) ? "Uncategorized" : r.Category)
                .Select(g => new StockByCategoryDto
                {
                    Category = g.Key,
                    TotalQuantity = g.Sum(x => x.StockQuantity)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ToList();

            // Chart: top 10 by quantity
            TopProducts = productRows
                .OrderByDescending(r => r.StockQuantity)
                .Take(10)
                .Select(r => new TopProductDto
                {
                    ItemName = r.ItemName,
                    Quantity = r.StockQuantity
                })
                .ToList();
        }
    }

    public class StockRow
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public string ItemType { get; set; }   // Product / Service
        public string Category { get; set; }   // Sellable / Consumable
        public double UnitCost { get; set; }
        public double SellingPrice { get; set; }
        public int StockQuantity { get; set; }
        public string UnitOfMeasure { get; set; }
        public string Status { get; set; }     // In Stock / Low Stock / Out of Stock / Service

        public double StockValueCost { get; set; }
        public double StockValueRevenue { get; set; }
    }

    public class StockByCategoryDto
    {
        public string Category { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class TopProductDto
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }
}
