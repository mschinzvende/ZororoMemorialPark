using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using ZororoMemorialPark.Pages.Reports; // for RevenueRow

namespace ZororoMemorialPark.Services
{
    public class RevenueReportPdfService
    {
        public byte[] GenerateRevenueReportPdf(
            List<RevenueRow> rows,
            DateOnly? fromDate,
            DateOnly? toDate,
            string category,
            double totalRevenue,
            double graveSalesRevenue,
            double reservationsRevenue,
            double tombstoneRevenue,
            double otherRevenue,
            string companyName,
            string companyAddress,
            string companyPhone,
            string companyEmail,
            string logoPath)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // HEADER
                    page.Header().Row(row =>
                    {
                        row.ConstantItem(80).Column(col =>
                        {
                            if (!string.IsNullOrWhiteSpace(logoPath))
                            {
                                col.Item().Height(50).Image(logoPath, ImageScaling.FitArea);
                            }
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(companyName).Bold().FontSize(16);
                            col.Item().Text(companyAddress).FontSize(10);
                            col.Item().Text($"{companyPhone} | {companyEmail}").FontSize(10);

                            col.Item().PaddingTop(8)
                                .Text($"Revenue Report - {CategoryFriendlyName(category)}")
                                .Bold().FontSize(13);

                            string rangeText = BuildDateRangeText(fromDate, toDate);
                            if (!string.IsNullOrEmpty(rangeText))
                                col.Item().Text(rangeText).FontSize(10);

                            col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingTop(10).Column(content =>
                    {
                        // SUMMARY CARDS ROW
                        content.Item().Row(row =>
                        {
                            row.RelativeItem(1).Element(c => SummaryCard(c, "Total Revenue", totalRevenue, Colors.Green.Darken2));
                            row.RelativeItem(1).Element(c => SummaryCard(c, "Grave Sales", graveSalesRevenue, Colors.Blue.Darken2));
                            row.RelativeItem(1).Element(c => SummaryCard(c, "Reservations", reservationsRevenue, Colors.Orange.Darken2));
                            row.RelativeItem(1).Element(c => SummaryCard(c, "Tombstones", tombstoneRevenue, Colors.Purple.Darken2));
                            row.RelativeItem(1).Element(c => SummaryCard(c, "Other Revenue", otherRevenue, Colors.Grey.Darken2));
                        });

                        content.Item().PaddingTop(12).Text("Detailed Transactions")
                            .Bold().FontSize(11);

                        // TABLE
                        content.Item().PaddingTop(4).Element(container =>
                        {
                            container.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80);   // Date
                                    columns.RelativeColumn(1.0f); // Type
                                    columns.ConstantColumn(70);   // InvoiceId
                                    columns.ConstantColumn(80);   // Amount
                                    columns.RelativeColumn(2.5f); // Notes
                                });

                                // HEADER
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellHeaderStyle).Text("Date");
                                    header.Cell().Element(CellHeaderStyle).Text("Type");
                                    header.Cell().Element(CellHeaderStyle).Text("Invoice Id");
                                    header.Cell().Element(CellHeaderStyle).AlignRight().Text("Amount");
                                    header.Cell().Element(CellHeaderStyle).Text("Notes");
                                });

                                if (rows != null && rows.Any())
                                {
                                    foreach (var r in rows.OrderBy(r => r.Date).ThenBy(r => r.InvoiceId))
                                    {
                                        table.Cell().Element(CellBodyStyle).Text(r.Date.ToString("yyyy-MM-dd"));
                                        table.Cell().Element(CellBodyStyle).Text(r.Type);
                                        table.Cell().Element(CellBodyStyle).Text(r.InvoiceId.ToString());
                                        table.Cell().Element(CellBodyStyle).AlignRight().Text($"${r.Amount:N2}");
                                        table.Cell().Element(CellBodyStyle).Text(r.Notes ?? "");
                                    }
                                }
                                else
                                {
                                    table.Cell().ColumnSpan(5)
                                        .Element(CellBodyStyle)
                                        .AlignCenter()
                                        .Text("No data for the selected period and category.")
                                        .Italic().FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span(companyName).SemiBold();
                        text.Span(" • Revenue Report • Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static string CategoryFriendlyName(string category)
        {
            return category switch
            {
                "GraveSales" => "Grave Sales",
                "Reservations" => "Grave Reservations",
                "Tombstones" => "Tombstone Sales",
                "Other" => "Other Revenue",
                _ => "Total Revenue"
            };
        }

        private static string BuildDateRangeText(DateOnly? from, DateOnly? to)
        {
            if (!from.HasValue && !to.HasValue)
                return string.Empty;

            string start = from.HasValue ? from.Value.ToString("yyyy-MM-dd") : "...";
            string end = to.HasValue ? to.Value.ToString("yyyy-MM-dd") : "...";

            return $"Date Range: {start} to {end}";
        }

        private static void SummaryCard(IContainer container, string title, double amount, string color)
        {
            container.PaddingRight(5).Column(col =>
            {
                col.Item().Border(0.5f)
                    .BorderColor(Colors.Grey.Lighten1)
                    .Background(Colors.White)
                    .Padding(8)
                    .Column(c =>
                    {
                        c.Item().Text(title)
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        c.Item().Text($"${amount:N2}")
                            .FontSize(14)
                            .Bold()
                            .FontColor(color);
                    });
            });
        }

        private static IContainer CellHeaderStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(t => t.SemiBold().FontSize(9));
        }

        private static IContainer CellBodyStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(2)
                .PaddingHorizontal(3);
        }
    }
}
