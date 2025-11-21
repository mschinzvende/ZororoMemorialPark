using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using ZororoMemorialPark.Pages.Reports;

namespace ZororoMemorialPark.Services
{
    public class RevenueReportPdfService
    {
        public byte[] GenerateRevenuePdf(
            List<RevenueByDateDto> dailyTotals,
            RevenueSummaryDto summary,
            List<IncomeLineDto> allocationLines,
            List<IncomeLineDto> reservationLines,
            List<TombstoneRevenueLineDto> tombstoneLines,
            List<IncomeLineDto> otherLines,
            string companyName,
            string companyAddress,
            string companyPhone,
            string companyEmail,
            string logoPath,
            DateOnly? fromDate,
            DateOnly? toDate)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    // A4 Landscape
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor(Colors.Grey.Lighten5);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken4));

                    // Header
                    page.Header().Background(Colors.White).Padding(10).Row(row =>
                    {
                        row.ConstantItem(90).Column(col =>
                        {
                            if (!string.IsNullOrEmpty(logoPath))
                            {
                                col.Item().Height(60).Image(logoPath, ImageScaling.FitArea);
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName)
                                .FontSize(18).Bold()
                                .FontColor("#006A46");

                            col.Item().Text(companyAddress).FontSize(10);
                            col.Item().Text($"{companyPhone} | {companyEmail}").FontSize(10);

                            col.Item().PaddingTop(5).Text(txt =>
                            {
                                txt.Span("Revenue Report").FontSize(14).Bold();
                            });

                            if (fromDate.HasValue || toDate.HasValue)
                            {
                                string range = "Period: ";
                                if (fromDate.HasValue)
                                    range += fromDate.Value.ToString("yyyy-MM-dd");
                                range += " to ";
                                if (toDate.HasValue)
                                    range += toDate.Value.ToString("yyyy-MM-dd");

                                col.Item().Text(range).FontSize(10);
                            }

                            col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // Content
                    page.Content().PaddingTop(10).Column(content =>
                    {
                        // Summary cards
                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Element(c => SummaryCard(c, "Total Revenue",
                                summary.TotalRevenue, Colors.Green.Darken3));
                            row.RelativeItem().Element(c => SummaryCard(c, "Grave Sales",
                                summary.GraveSalesRevenue, Colors.Blue.Darken3));
                            row.RelativeItem().Element(c => SummaryCard(c, "Reservations",
                                summary.ReservationsRevenue, Colors.Purple.Darken3));
                            row.RelativeItem().Element(c => SummaryCard(c, "Tombstones",
                                summary.TombstoneRevenue, Colors.Orange.Darken3));
                            row.RelativeItem().Element(c => SummaryCard(c, "Other",
                                summary.OtherRevenue, Colors.Grey.Darken2));
                        });

                        // Daily totals table
                        content.Item().PaddingTop(10).Element(DailyTotalsTable(dailyTotals));

                        // Category breakdown tables (side by side if possible)
                        content.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Element(c =>
                                CategoryTable(c, "Grave Sales (Allocations)", allocationLines));
                            row.RelativeItem().Element(c =>
                                CategoryTable(c, "Reservations", reservationLines));
                        });

                        content.Item().PaddingTop(10).Row(row =>
                        {
                            row.RelativeItem().Element(c =>
                                TombstoneTable(c, "Tombstone Sales", tombstoneLines));
                            row.RelativeItem().Element(c =>
                                CategoryTable(c, "Other Revenue", otherLines));
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().PaddingTop(10).Text(txt =>
                    {
                        txt.Span(companyName).SemiBold().FontColor("#006A46");
                        txt.Span("  •  Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                    
                    
                });
            });

            return doc.GeneratePdf();
        }

        private static void SummaryCard(IContainer container, string title, double amount, string color)
        {
            container
                .PaddingRight(5)
                .Border(0.5f)
                .BorderColor(Colors.Grey.Lighten1)
                .Background(Colors.White)
                .Padding(8)
                .Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    col.Item().Text($"${amount:N2}")
                        .FontSize(14)
                        .Bold()
                        .FontColor(color);
                });
        }




        private static Action<IContainer> DailyTotalsTable(List<RevenueByDateDto> rows)
        {
            return container =>
            {
                container.Background(Colors.White).Padding(8).Column(col =>
                {
                    col.Item().Text("Daily Revenue Totals")
                        .FontSize(11).Bold()
                        .FontColor("#006A46");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Date");
                            header.Cell().Element(CellHeader).AlignRight().Text("Total Amount");
                        });

                        foreach (var r in rows.OrderBy(x => x.Date))
                        {
                            table.Cell().Element(CellBody).Text(r.Date.ToString("yyyy-MM-dd"));
                            table.Cell().Element(CellBody)
                                .AlignRight()
                                .Text($"${r.TotalAmount:N2}");
                        }

                        if (!rows.Any())
                        {
                            table.Cell().ColumnSpan(2).Element(CellBody)
                                .AlignCenter()
                                .Text("No data for selected period.");
                        }
                    });
                });
            };
        }

        private static Action<IContainer> CategoryTable(IContainer container, string title, List<IncomeLineDto> rows)
        {
            return c =>
            {
                c.Background(Colors.White).Padding(8).Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(11).Bold()
                        .FontColor("#006A46");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // Date
                            columns.RelativeColumn();     // Method
                            columns.RelativeColumn();     // Notes
                            columns.ConstantColumn(80);   // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Date");
                            header.Cell().Element(CellHeader).Text("Method");
                            header.Cell().Element(CellHeader).Text("Notes");
                            header.Cell().Element(CellHeader).AlignRight().Text("Amount");
                        });

                        foreach (var r in rows.OrderByDescending(x => x.Date))
                        {
                            table.Cell().Element(CellBody).Text(r.Date.ToString("yyyy-MM-dd"));
                            table.Cell().Element(CellBody).Text(r.PaymentMethod);
                            table.Cell().Element(CellBody).Text(r.Notes ?? "");
                            table.Cell().Element(CellBody).AlignRight().Text($"${r.Amount:N2}");
                        }

                        if (!rows.Any())
                        {
                            table.Cell().ColumnSpan(4).Element(CellBody)
                                .AlignCenter()
                                .Text("No records.");
                        }
                    });
                });
            };
        }

        private static Action<IContainer> TombstoneTable(IContainer container, string title, List<TombstoneRevenueLineDto> rows)
        {
            return c =>
            {
                c.Background(Colors.White).Padding(8).Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(11).Bold()
                        .FontColor("#006A46");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);  // Date
                            columns.RelativeColumn();     // Buyer
                            columns.RelativeColumn();     // Contact
                            columns.ConstantColumn(80);   // Status
                            columns.ConstantColumn(80);   // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("Date");
                            header.Cell().Element(CellHeader).Text("Buyer");
                            header.Cell().Element(CellHeader).Text("Contact");
                            header.Cell().Element(CellHeader).Text("Status");
                            header.Cell().Element(CellHeader).AlignRight().Text("Amount");
                        });

                        foreach (var r in rows.OrderByDescending(x => x.SaleDate))
                        {
                            table.Cell().Element(CellBody).Text(r.SaleDate.ToString("yyyy-MM-dd"));
                            table.Cell().Element(CellBody).Text(r.BuyerName);
                            table.Cell().Element(CellBody).Text(r.BuyerContact);
                            table.Cell().Element(CellBody).Text(r.PaymentStatus);
                            table.Cell().Element(CellBody).AlignRight().Text($"${r.AmountPaid:N2}");
                        }

                        if (!rows.Any())
                        {
                            table.Cell().ColumnSpan(5).Element(CellBody)
                                .AlignCenter()
                                .Text("No tombstone revenue records.");
                        }
                    });
                });
            };
        }

        private static IContainer CellHeader(IContainer container)
        {
            return container
                .DefaultTextStyle(x => x.SemiBold().FontSize(9))
                .PaddingVertical(3)
                .PaddingHorizontal(4)
                .Background(Colors.Grey.Lighten3)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten1);
        }

        private static IContainer CellBody(IContainer container)
        {
            return container
                .PaddingVertical(2)
                .PaddingHorizontal(4)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten3);
        }
    }
}
