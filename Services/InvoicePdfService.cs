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


        public byte[] GenerateInvoicePdf(tbl_Invoices invoice,
            string companyName,
            string companyAddress,
            string companyPhone,
            string companyEmail,
            string logoPath, // path on server or null
            string customerName,
            List<tbl_InvoiceItems> items,
            List<tbl_IncomePayments> payments)
        {
            var themeGreen = "#006A46";
            var themeGrey = Colors.Grey.Lighten3;

            var total = items.Sum(i => i.sys_itemCost);
            var paid = payments.Sum(p => p.sys_amount);
            var balance = total - paid;

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
                        // Logo Section
                        row.ConstantItem(80).Column(col =>
                        {
                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                            {
                                col.Item().Height(50).Image(logoPath, ImageScaling.FitArea);
                            }
                        });

                        // Company Info
                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(companyName).FontColor(Colors.White).Bold().FontSize(16);
                            col.Item().Text(companyAddress).FontColor(Colors.White).FontSize(10);
                            col.Item().Text($"{companyPhone} | {companyEmail}").FontColor(Colors.White).FontSize(10);
                        });
                    });

                    // ---------- BODY ----------
                    page.Content().PaddingVertical(15).Column(content =>
                    {
                        // Invoice Info + Customer
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
                                c.Item().Text(customerName ?? "Customer");
                            });
                        });

                        // Items Table
                        content.Item().PaddingTop(15).Element(c => ComposeItemsTable(c, items, themeGreen));

                        // Totals Box
                        content.Item().PaddingTop(10).AlignRight().Column(c =>
                        {
                            c.Item().Background(themeGrey).Padding(8).Column(inner =>
                            {
                                inner.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Subtotal:");
                                    r.ConstantItem(80).AlignRight().Text($"${total:N2}");
                                });
                                inner.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Paid:");
                                    r.ConstantItem(80).AlignRight().Text($"${paid:N2}");
                                });
                                inner.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Balance:").Bold();
                                    r.ConstantItem(80).AlignRight().Text($"${balance:N2}").Bold().FontColor(themeGreen);
                                });
                            });
                        });

                        // Payments Section
                        content.Item().PaddingTop(20).Column(c =>
                        {
                            c.Item().Text("Payments").FontColor(themeGreen).Bold().FontSize(13);

                            if (payments != null && payments.Any())
                            {
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn();
                                        cd.RelativeColumn();
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(CellHeaderStyle(themeGreen)).Text("Date").SemiBold().FontColor(Colors.White);
                                        header.Cell().Element(CellHeaderStyle(themeGreen)).AlignRight().Text("Amount").SemiBold().FontColor(Colors.White);
                                    });

                                    foreach (var p in payments)
                                    {
                                        table.Cell().Element(CellStyle).Text(p.sys_date.ToString("dd-MM-yyyy"));
                                        table.Cell().Element(CellStyle).AlignRight().Text($"${p.sys_amount:N2}");
                                    }
                                });
                            }
                            else
                            {
                                c.Item().Text("No payments recorded").Italic().FontColor(Colors.Grey.Darken1);
                            }
                        });
                    });

                    // ---------- FOOTER ----------
                    page.Footer().AlignCenter().PaddingTop(10).Text(txt =>
                    {
                        txt.Span(companyName).SemiBold().FontColor(themeGreen);
                        txt.Span("  •  Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }

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
