using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using ZororoMemorialPark.Pages.Reports; // for OutstandingRow

namespace ZororoMemorialPark.Services
{
    public class OutstandingBalancesPdfService
    {
        public byte[] GenerateOutstandingBalancesPdf(
            List<OutstandingRow> rows,
            DateOnly? fromDate,
            DateOnly? toDate,
            string selectedCategoryKey,
            double totalInvoiced,
            double totalPaid,
            double totalOutstanding,
            double graveSalesOutstanding,
            double reservationsOutstanding,
            double tombstonesOutstanding,
            double otherOutstanding,
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
                            if (!string.IsNullOrEmpty(logoPath))
                            {
                                col.Item().Height(50).Image(logoPath, ImageScaling.FitArea);
                            }
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text(companyName).Bold().FontSize(16);
                            col.Item().AlignRight().Text(companyAddress).FontSize(10);
                            col.Item().AlignRight().Text($"{companyPhone} | {companyEmail}").FontSize(10);

                            col.Item().PaddingTop(8).AlignRight().Text("Outstanding Balances Report")
                                .Bold().FontSize(14);

                            // Filters
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

                            string categoryLabel = selectedCategoryKey switch
                            {
                                "GraveSales" => "Grave Sales",
                                "Reservations" => "Grave Reservations",
                                "Tombstones" => "Tombstone Sales",
                                "Other" => "Other Revenue",
                                _ => "All Invoices"
                            };

                            if (!string.IsNullOrEmpty(rangeText))
                                col.Item().AlignRight().Text(rangeText).FontSize(10);

                            col.Item().AlignRight().Text($"Category: {categoryLabel}").FontSize(10);
                            col.Item().AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingTop(10).Column(content =>
                    {
                        // Summary cards row
                        content.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Element(c =>
                                SummaryCard(c, "Total Invoiced", totalInvoiced, Colors.Blue.Darken2));
                            row.RelativeItem().Element(c =>
                                SummaryCard(c, "Total Paid", totalPaid, Colors.Green.Darken2));
                            row.RelativeItem().Element(c =>
                                SummaryCard(c, "Total Outstanding", totalOutstanding, Colors.Red.Darken2));
                        });

                        // Category breakdown mini-row
                        content.Item().PaddingBottom(10).Row(row =>
                        {
                            row.RelativeItem().Element(c =>
                                MiniCard(c, "Grave Sales Outstanding", graveSalesOutstanding));
                            row.RelativeItem().Element(c =>
                                MiniCard(c, "Reservations Outstanding", reservationsOutstanding));
                            row.RelativeItem().Element(c =>
                                MiniCard(c, "Tombstones Outstanding", tombstonesOutstanding));
                            row.RelativeItem().Element(c =>
                                MiniCard(c, "Other Outstanding", otherOutstanding));
                        });

                        // Table of rows
                        content.Item().Element(container =>
                        {
                            container.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(60);   // Date
                                    columns.ConstantColumn(60);   // Invoice
                                    columns.ConstantColumn(80);   // Category
                                    columns.RelativeColumn(1.5f); // Client
                                    columns.RelativeColumn(1.2f); // Phone
                                    columns.ConstantColumn(70);   // Total
                                    columns.ConstantColumn(70);   // Paid
                                    columns.ConstantColumn(80);   // Outstanding
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCellStyle).Text("Date");
                                    header.Cell().Element(HeaderCellStyle).Text("Invoice #");
                                    header.Cell().Element(HeaderCellStyle).Text("Category");
                                    header.Cell().Element(HeaderCellStyle).Text("Client");
                                    header.Cell().Element(HeaderCellStyle).Text("Phone");
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Total");
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Paid");
                                    header.Cell().Element(HeaderCellStyle).AlignRight().Text("Outstanding");
                                });

                                foreach (var r in rows.OrderByDescending(x => x.Outstanding))
                                {
                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.Date.ToString("yyyy-MM-dd"));

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.InvoiceId.ToString());

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.CategoryLabel);

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.ClientName);

                                    table.Cell().Element(BodyCellStyle)
                                        .Text(r.ClientPhone);

                                    table.Cell().Element(BodyCellStyle)
                                        .AlignRight().Text($"${r.Total:N2}");

                                    table.Cell().Element(BodyCellStyle)
                                        .AlignRight().Text($"${r.Paid:N2}");

                                    table.Cell().Element(container2 =>
                                    {
                                        var baseContainer = BodyCellStyle(container2);
                                        // Slight highlight if strongly outstanding
                                        if (r.Outstanding > 0)
                                        {
                                            baseContainer
                                                .Background(Colors.Red.Lighten5)
                                                .AlignRight()
                                                .Text($"${r.Outstanding:N2}")
                                                .FontColor(Colors.Red.Darken2)
                                                .SemiBold();
                                        }

                                        baseContainer.AlignRight()
                                            .Text($"${r.Outstanding:N2}");
                                    });
                                }

                                if (!rows.Any())
                                {
                                    table.Cell().ColumnSpan(8)
                                        .Element(BodyCellStyle)
                                        .AlignCenter()
                                        .Text("No outstanding invoices for the selected filters.");
                                }
                            });
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span(companyName).SemiBold();
                        txt.Span(" • Outstanding Balances • Page ");
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
                .Background(Colors.Grey.Lighten4)
                .Border(0.75f)
                .BorderColor(Colors.Grey.Lighten2)
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

        private static void MiniCard(IContainer container, string title, double amount)
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

                    col.Item().Text($"${amount:N2}")
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
