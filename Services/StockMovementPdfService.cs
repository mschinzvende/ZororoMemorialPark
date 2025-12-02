using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using ZororoMemorialPark.Pages.Reports; // for StockMovementRow

namespace ZororoMemorialPark.Services
{
    public class StockMovementPdfService
    {
        public byte[] GenerateStockMovementPdf(
            List<StockMovementRow> rows,
            DateTime? fromDate,
            DateTime? toDate,
            string selectedMovementType,
            double totalIn,
            double totalOut,
            double netQuantity,
            double purchaseQty,
            double saleQty,
            double jobUsageQty,
            double adjustmentQty,
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
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // HEADER
                    page.Header().Row(row =>
                    {
                        row.ConstantItem(80).Column(col =>
                        {
                            if (!string.IsNullOrEmpty(logoPath))
                            {
                                col.Item().Height(50).Image(logoPath, ImageScaling.FitArea);
                            }
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text(companyName).Bold().FontSize(16);
                            col.Item().AlignRight().Text(companyAddress).FontSize(9);
                            col.Item().AlignRight().Text($"{companyPhone} | {companyEmail}").FontSize(9);

                            col.Item().PaddingTop(8).AlignRight().Text("Stock Movement Report")
                                .Bold().FontSize(14);

                            string rangeText = "";
                            if (fromDate.HasValue || toDate.HasValue)
                            {
                                rangeText = "Date Range: ";
                                if (fromDate.HasValue)
                                    rangeText += fromDate.Value.ToString("yyyy-MM-dd");
                                rangeText += " to ";
                                if (toDate.HasValue)
                                    rangeText += toDate.Value.ToString("yyyy-MM-dd");
                            }

                            string typeLabel = selectedMovementType switch
                            {
                                "Purchases" => "Purchases Only",
                                "Sales" => "Sales Only",
                                "JobUsage" => "Job Usage Only",
                                "Adjustments" => "Adjustments / Consumption",
                                "In" => "Inbound Movements",
                                "Out" => "Outbound Movements",
                                _ => "All Movement Types"
                            };

                            if (!string.IsNullOrEmpty(rangeText))
                                col.Item().AlignRight().Text(rangeText).FontSize(9);

                            col.Item().AlignRight().Text($"Filter: {typeLabel}").FontSize(9);
                            col.Item().AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingTop(10).Column(content =>
                    {
                        // Summary row
                        content.Item().PaddingBottom(8).Row(row =>
                        {
                            row.RelativeItem().Element(c =>
                            {
                                SummaryCard(c, "Total Qty In", totalIn, Colors.Green.Darken2);
                            });
                            row.RelativeItem().Element(c =>
                            {
                                SummaryCard(c, "Total Qty Out", totalOut, Colors.Red.Darken2);
                            });
                            row.RelativeItem().Element(c =>
                            {
                                SummaryCard(c, "Net Movement (In − Out)", netQuantity,
                                    netQuantity >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            });
                        });

                        // Breakdown row
                        content.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Element(c =>
                            {
                                MiniCard(c, "Purchases", purchaseQty);
                            });
                            row.RelativeItem().Element(c =>
                            {
                                MiniCard(c, "Sales", saleQty);
                            });
                            row.RelativeItem().Element(c =>
                            {
                                MiniCard(c, "Job Usage", jobUsageQty);
                            });
                            row.RelativeItem().Element(c =>
                            {
                                MiniCard(c, "Adjustments / Consumption", adjustmentQty);
                            });
                        });

                        // TABLE
                        content.Item().Element(container2 =>
                        {
                            container2.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);   // Date
                                    columns.RelativeColumn(1.2f); // Item
                                    columns.ConstantColumn(70);   // Type
                                    columns.ConstantColumn(55);   // In
                                    columns.ConstantColumn(55);   // Out
                                    columns.ConstantColumn(65);   // Unit Cost
                                    columns.ConstantColumn(70);   // Total Cost
                                    columns.RelativeColumn(1.0f); // Reference
                                    columns.RelativeColumn(1.4f); // Notes
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Date");
                                    header.Cell().Element(HeaderCellStyle).Text("Item");
                                    header.Cell().Element(HeaderCellStyle).Text("Type");
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty In");
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Qty Out");
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Unit Cost");
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Total Cost");
                                    header.Cell().Element(HeaderCellStyle).Text("Reference");
                                    header.Cell().Element(HeaderCellStyle).Text("Notes");
                                });

                                foreach (var r in rows.OrderBy(x => x.Date).ThenBy(x => x.ItemName))
                                {
                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.Date.ToString("yyyy-MM-dd"));

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.ItemName);

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.MovementType);

                                    table.Cell().Element(BodyCellStyle)
                                        .AlignRight().Text(r.QuantityIn > 0 ? r.QuantityIn.ToString("N2") : "");

                                    table.Cell().Element(BodyCellStyle)
                                        .AlignRight().Text(r.QuantityOut > 0 ? r.QuantityOut.ToString("N2") : "");

                                    table.Cell().Element(BodyCellStyle)
                                        .AlignRight().Text($"${r.UnitCost:N2}");

                                    table.Cell().Element(BodyCellStyle)
                                        .AlignRight().Text($"${r.TotalCost:N2}");

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.Reference ?? "");

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.Notes ?? "");
                                }

                                if (!rows.Any())
                                {
                                    table.Cell().ColumnSpan(9)
                                        .Element(BodyCellStyle)
                                        .AlignCenter()
                                        .Text("No movements for the selected filters.");
                                }
                            });
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span(companyName).SemiBold();
                        txt.Span(" • Stock Movement • Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static void SummaryCard(IContainer container, string title, double value, string color)
        {
            container
                .Background(Colors.Grey.Lighten4)
                .Border(0.75f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(8)
                .Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);

                    col.Item().Text(value.ToString("N2"))
                        .FontSize(14)
                        .Bold()
                        .FontColor(color);
                });
        }

        private static void MiniCard(IContainer container, string title, double value)
        {
            container
                .Background(Colors.Grey.Lighten5)
                .Border(0.5f)
                .BorderColor(Colors.Grey.Lighten3)
                .Padding(6)
                .Column(col =>
                {
                    col.Item().Text(title)
                        .FontSize(8)
                        .FontColor(Colors.Grey.Darken2);

                    col.Item().Text(value.ToString("N2"))
                        .FontSize(10)
                        .Bold();
                });
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(x => x.SemiBold().FontSize(9))
                .Background(Colors.Grey.Lighten3);
        }

        private static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(2)
                .PaddingHorizontal(3);
        }
    }
}
