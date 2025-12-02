using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using ZororoMemorialPark.Pages.Reports; // for PaymentLedgerRow namespace

namespace ZororoMemorialPark.Services
{
    public class PaymentLedgerPdfService
    {
        public byte[] GeneratePaymentLedgerPdf(
            List<PaymentLedgerRow> rows,
            DateOnly? fromDate,
            DateOnly? toDate,
            string category,
            double totalAmount,
            double graveSalesAmount,
            double reservationsAmount,
            double tombstoneAmount,
            double otherAmount,
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
                    page.Margin(25);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Header
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
                            col.Item().Text(companyName).Bold().FontSize(16).FontColor(Colors.Green.Darken2);
                            col.Item().Text(companyAddress).FontSize(10);
                            col.Item().Text($"{companyPhone} | {companyEmail}").FontSize(10);

                            col.Item().PaddingTop(6).Text("Payment Ledger").Bold().FontSize(14);

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

                            if (!string.IsNullOrEmpty(rangeText))
                                col.Item().Text(rangeText).FontSize(9);

                            if (!string.IsNullOrEmpty(category) && category != "All")
                                col.Item().Text($"Category: {category}").FontSize(9);
                        });
                    });

                    page.Content().PaddingTop(10).Column(content =>
                    {
                        // Summary row with coloured "cards"
                        content.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(5).Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Green.Lighten5)
                                .Padding(6)
                                .Column(c =>
                                {
                                    c.Item().Text("Total Amount")
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                    c.Item().Text($"${totalAmount:N2}")
                                        .FontSize(14)
                                        .Bold()
                                        .FontColor(Colors.Green.Darken2);
                                });

                            row.RelativeItem().PaddingRight(5).Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.LightBlue.Lighten4)
                                .Padding(6)
                                .Column(c =>
                                {
                                    c.Item().Text("Grave Sales").FontSize(9);
                                    c.Item().Text($"${graveSalesAmount:N2}")
                                        .FontSize(11).Bold();
                                });

                            row.RelativeItem().PaddingRight(5).Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Amber.Lighten4)
                                .Padding(6)
                                .Column(c =>
                                {
                                    c.Item().Text("Reservations").FontSize(9);
                                    c.Item().Text($"${reservationsAmount:N2}")
                                        .FontSize(11).Bold();
                                });

                            row.RelativeItem().PaddingRight(5).Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.DeepPurple.Lighten5)
                                .Padding(6)
                                .Column(c =>
                                {
                                    c.Item().Text("Tombstones").FontSize(9);
                                    c.Item().Text($"${tombstoneAmount:N2}")
                                        .FontSize(11).Bold();
                                });

                            row.RelativeItem().Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten1)
                                .Background(Colors.Grey.Lighten4)
                                .Padding(6)
                                .Column(c =>
                                {
                                    c.Item().Text("Other Revenue").FontSize(9);
                                    c.Item().Text($"${otherAmount:N2}")
                                        .FontSize(11).Bold();
                                });
                        });

                        content.Item().PaddingTop(10).Element(container =>
                        {
                            container.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(70);   // Date
                                    columns.RelativeColumn(1.3f); // Category
                                    columns.ConstantColumn(60);   // Invoice
                                    columns.ConstantColumn(70);   // Receipt
                                    columns.RelativeColumn(1.2f); // Method
                                    columns.RelativeColumn(1.5f); // Payer
                                    columns.ConstantColumn(80);   // Amount
                                    columns.RelativeColumn(2.0f); // Notes
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(CellHeaderStyle).Text("Date");
                                    header.Cell().Element(CellHeaderStyle).Text("Category");
                                    header.Cell().Element(CellHeaderStyle).Text("Invoice");
                                    header.Cell().Element(CellHeaderStyle).Text("Receipt #");
                                    header.Cell().Element(CellHeaderStyle).Text("Method");
                                    header.Cell().Element(CellHeaderStyle).Text("Payer");
                                    header.Cell().Element(CellHeaderStyle).AlignRight().Text("Amount");
                                    header.Cell().Element(CellHeaderStyle).Text("Notes");
                                });

                                foreach (var r in rows)
                                {
                                    table.Cell().Element(CellBodyStyle)
                                        .Text(r.Date.ToString("yyyy-MM-dd"));
                                    table.Cell().Element(CellBodyStyle)
                                        .Text(r.Category);
                                    table.Cell().Element(CellBodyStyle)
                                        .Text(r.InvoiceId.ToString());
                                    table.Cell().Element(CellBodyStyle)
                                        .Text(r.ReceiptNumber?.ToString() ?? "-");
                                    table.Cell().Element(CellBodyStyle)
                                        .Text(r.PaymentMethod);
                                    table.Cell().Element(CellBodyStyle)
                                        .Text(r.Payer);
                                    table.Cell().Element(CellBodyStyle)
                                        .AlignRight().Text($"${r.Amount:N2}");
                                    table.Cell().Element(CellBodyStyle)
                                        .Text(r.Notes ?? "");
                                }
                            });
                        });

                        if (rows.Any())
                        {
                            content.Item().PaddingTop(8)
                                .Text($"Total records: {rows.Count}")
                                .FontSize(9);
                        }
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span(companyName).SemiBold();
                        txt.Span(" • Payment Ledger • Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static IContainer CellHeaderStyle(IContainer container)
        {
            return container
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(x => x.SemiBold().FontSize(9))
                .Background(Colors.Green.Lighten4)
                .BorderBottom(1)
                .BorderColor(Colors.Green.Darken2);
        }

        private static IContainer CellBodyStyle(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten3)
                .PaddingVertical(2)
                .PaddingHorizontal(3);
        }
    }
}
