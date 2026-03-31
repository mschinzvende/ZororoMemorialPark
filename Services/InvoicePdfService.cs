using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using QuestPDF.Drawing;
using System.IO;
using ZororoMemorialPark.Data.Models;
using System.Collections.Generic;
using System.Linq;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace ZororoMemorialPark.Services
{
    public class InvoicePdfService
    {
        public byte[] GenerateInvoicePdf(
        tbl_Invoices invoice,
        SystemSettings settings,
        tbl_BurialBookings booking,
        List<tbl_InvoiceItems> items,
        List<tbl_BurialPayments> payments,
        string logoPath // can be null
    )
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var themeGreen = "#006A46";
            var themeGrey = Colors.Grey.Lighten3;

            var total = items.Sum(i => i.sys_itemCost);
            var paid = payments.Sum(p => p.sys_amountPaid);
            var balance = total - paid;

            var customerName = $"{booking.sys_deceasedFirstName} {booking.sys_deceasedLastName}";

            var doc = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));
                    page.PageColor(Colors.White);

                    // ---------- HEADER ----------
                    page.Header().Background(themeGreen).Padding(15).Row(row =>
                    {
                        // Logo
                        row.ConstantItem(80).Column(col =>
                        {
                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                            {
                                col.Item().Height(50).Image(logoPath, ImageScaling.FitArea);
                            }
                        });

                        // Company info
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(settings.CompanyName)
                                .FontColor(Colors.White)
                                .Bold()
                                .FontSize(16);

                            col.Item().Text(settings.PhysicalAddress)
                                .FontColor(Colors.White)
                                .FontSize(10);

                            col.Item().Text($"{settings.Phone} | {settings.Email}")
                                .FontColor(Colors.White)
                                .FontSize(10);
                        });
                    });

                    // ---------- BODY ----------
                    page.Content().PaddingVertical(15).Column(content =>
                    {
                        // Invoice info
                        content.Item().Background(themeGrey).Padding(10).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Invoice No: {invoice.sys_invoiceId}").Bold();
                                c.Item().Text($"Date: {invoice.sys_invoiceDate:dd-MM-yyyy}");
                                c.Item().Text($"Due Date: {invoice.sys_invoiceDueDate:dd-MM-yyyy}");
                            });

                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Bill To:").SemiBold();
                                c.Item().Text(customerName);
                            });
                        });

                        // Items
                        content.Item().PaddingTop(15)
                            .Element(c => ComposeItemsTable(c, items, themeGreen));

                        // Totals
                        content.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            c.Item().Background(themeGrey).Padding(8).Column(inner =>
                            {
                                inner.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal:");
                                    r.ConstantItem(100).AlignRight().Text($"{total:N2}");
                                });

                                inner.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Paid:");
                                    r.ConstantItem(100).AlignRight().Text($"{paid:N2}");
                                });

                                inner.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Balance:").Bold();
                                    r.ConstantItem(100)
                                        .AlignRight()
                                        .Text($"{balance:N2}")
                                        .Bold()
                                        .FontColor(themeGreen);
                                });
                            });
                        });

                        // Payments
                        content.Item().PaddingTop(20).Column(c =>
                        {
                            c.Item().Text("Payments")
                                .FontColor(themeGreen)
                                .Bold()
                                .FontSize(13);

                            if (payments.Any())
                            {
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn();
                                        cd.RelativeColumn();
                                        cd.ConstantColumn(80);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellHeaderStyle(themeGreen))
                                            .Text("Date").SemiBold().FontColor(Colors.White);

                                        header.Cell().Element(CellHeaderStyle(themeGreen))
                                            .Text("Receipt No").SemiBold().FontColor(Colors.White);

                                        header.Cell().Element(CellHeaderStyle(themeGreen))
                                            .AlignRight()
                                            .Text("Amount").SemiBold().FontColor(Colors.White);
                                    });

                                    foreach (var p in payments)
                                    {
                                        table.Cell().Element(CellStyle)
                                            .Text(p.sys_paymentDate.ToString("dd-MM-yyyy"));

                                        table.Cell().Element(CellStyle)
                                            .Text(p.sys_receiptNumber.ToString());

                                        table.Cell().Element(CellStyle)
                                            .AlignRight()
                                            .Text($"{p.sys_amountPaid:N2}");
                                    }
                                });
                            }
                            else
                            {
                                c.Item()
                                    .Text("No payments recorded")
                                    .Italic()
                                    .FontColor(Colors.Grey.Darken1);
                            }
                        });
                    });

                    // ---------- FOOTER ----------
                    page.Footer().AlignCenter().PaddingTop(10).Text(txt =>
                    {
                        txt.Span(settings.CompanyName).SemiBold().FontColor(themeGreen);
                        txt.Span("  •  Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }


        public byte[] GenerateInvoicePdf(
     tbl_Invoices invoice,
     string companyName,
     string companyAddress,
     string companyPhone,
     string companyEmail,
     string logoPath,
     string customerName,
     List<tbl_InvoiceItems> items,
     List<tbl_IncomePayments> payments)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var themeGreen = "#006A46";

            var total = items.Sum(i => i.sys_itemCost);
            var paid = payments.Sum(p => p.sys_amount);
            var balance = total - paid;

            var doc = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor("#F5F5F5");

                    page.DefaultTextStyle(x => x.FontSize(9));

                    // ================= HEADER =================
                    page.Header().Background(themeGreen).Padding(15).Row(row =>
                    {
                        // LEFT
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName).Bold().FontSize(13).FontColor(Colors.White);
                            col.Item().Text(companyAddress).FontSize(9).FontColor(Colors.White);
                            col.Item().Text($"{companyPhone} | {companyEmail}")
                                .FontSize(9).FontColor(Colors.White);
                        });

                        // CENTER LOGO
                        row.ConstantItem(100).AlignCenter().AlignMiddle().Element(container =>
                        {
                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                            {
                                container.Height(55).Image(logoPath, ImageScaling.FitHeight);
                            }
                        });

                        // RIGHT
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text("INVOICE")
                                .Bold().FontSize(14).FontColor(Colors.White);

                            col.Item().AlignRight().Text($"Date: {invoice.sys_invoiceDate:dd MMM yyyy}")
                                .FontSize(9).FontColor(Colors.White);
                        });
                    });

                    // ================= BODY =================
                    page.Content().PaddingTop(15).Column(content =>
                    {
                        content.Item().Background(Colors.White).Padding(12).Column(card =>
                        {
                            // CUSTOMER + META
                            card.Item().Row(r =>
                            {
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("Bill To").SemiBold();
                                    c.Item().Text(customerName);
                                });

                                r.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().Text($"Invoice #: {invoice.sys_invoiceId}");
                                    c.Item().Text($"Due: {invoice.sys_invoiceDueDate:dd MMM yyyy}");
                                });
                            });

                            // ITEMS TABLE
                            card.Item().PaddingTop(10).Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.RelativeColumn(4);
                                    cd.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderCell).Text("Item");
                                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                                });

                                foreach (var it in items)
                                {
                                    table.Cell().Element(Cell).Text(it.sys_itemName);
                                    table.Cell().Element(Cell).AlignRight().Text($"${it.sys_itemCost:N2}");
                                }
                            });

                            // TOTALS
                            card.Item().PaddingTop(10).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Total: ${total:N2}");
                                c.Item().Text($"Paid: ${paid:N2}");

                                c.Item().Text($"Balance: ${balance:N2}")
                                    .Bold()
                                    .FontColor(balance > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                            });

                            // PAYMENTS
                            if (payments.Any())
                            {
                                card.Item().PaddingTop(15).Text("Payments").SemiBold();

                                card.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn();
                                        cd.RelativeColumn();
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Element(HeaderCell).Text("Date");
                                        h.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                                    });

                                    foreach (var p in payments)
                                    {
                                        table.Cell().Element(Cell).Text(p.sys_date.ToString("dd MMM yyyy"));
                                        table.Cell().Element(Cell).AlignRight().Text($"${p.sys_amount:N2}");
                                    }
                                });
                            }
                        });
                    });

                    // ================= FOOTER =================
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span(companyName).FontSize(8);
                        txt.Span(" • Page ");
                        txt.CurrentPageNumber();
                    });
                });
            });

            return doc.GeneratePdf();
        }


        // STYLES
        private static IContainer Cell(IContainer c) =>
            c.PaddingVertical(4).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);

        private static IContainer HeaderCell(IContainer c) =>
            c.BorderBottom(1).BorderColor("#006A46").PaddingVertical(4).DefaultTextStyle(x => x.SemiBold());

        // ---------- ITEMS TABLE ----------
        private static void ComposeItemsTable(IContainer container, List<tbl_InvoiceItems> items, string themeGreen)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(4);
                    cd.RelativeColumn(1);
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellHeaderStyle(themeGreen)).Text("Item").SemiBold().FontColor(Colors.White);
                    header.Cell().Element(CellHeaderStyle(themeGreen)).AlignRight().Text("Cost").SemiBold().FontColor(Colors.White);
                });

                // Rows
                foreach (var it in items)
                {
                    table.Cell().Element(CellStyle).Text(it.sys_itemName);
                    table.Cell().Element(CellStyle).AlignRight().Text($"${it.sys_itemCost:N2}");
                }
            });
        }

        // ---------- STYLES ----------
        private static IContainer CellStyle(IContainer container)
        {
            return container
                .PaddingVertical(5)
                .PaddingHorizontal(4)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2);
        }

        private static Func<IContainer, IContainer> CellHeaderStyle(string color)
        {
            return container => container
                .Background(color)
                .PaddingVertical(6)
                .PaddingHorizontal(4);
        }
    }
}
