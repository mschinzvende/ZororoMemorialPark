/*using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using QuestPDF.Drawing;
using System.IO;
using ZororoMemorialPark.Data.Models;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using IContainer = QuestPDF.Infrastructure.IContainer;

namespace ZororoMemorialPark.Services
{
    public class InvoicePdfService
    {
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
            // Calculate totals
            var total = items.Sum(i => i.sys_itemCost);
            var paid = payments.Sum(p => p.sys_amount);
            var balance = total - paid;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            if (!string.IsNullOrEmpty(logoPath))
                            {
                                col.Item().Height(60).AlignLeft().Image(logoPath, ImageScaling.FitArea);
                            }
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text(companyName).Bold().FontSize(14);
                            col.Item().Text(companyAddress).FontSize(10);
                            col.Item().Text($"{companyPhone} | {companyEmail}").FontSize(10);
                        });
                    });

                    page.Content().PaddingVertical(10).Column(content =>
                    {
                        content.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Invoice: {invoice.sys_invoiceNumber}").Bold();
                                c.Item().Text($"Date: {invoice.sys_invoiceDate.ToString("yyyy-MM-dd")}");
                                c.Item().Text($"Due: {invoice.sys_invoiceDueDate.ToString("yyyy-MM-dd")}");
                            });

                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("Bill To:").SemiBold();
                                c.Item().Text(customerName ?? "Customer");
                            });
                        });

                        content.Item().PaddingTop(8).Element(ComposeItemsTable(items));

                        content.Item().PaddingTop(8).Row(r =>
                        {
                            r.RelativeItem();
                            r.ConstantItem(250).Column(c =>
                            {
                                c.Item().Row(rr =>
                                {
                                    rr.RelativeItem().Text("Subtotal:");
                                    rr.ConstantItem(80).AlignRight().Text($"${total:N2}");
                                });
                                c.Item().Row(rr =>
                                {
                                    rr.RelativeItem().Text("Paid:");
                                    rr.ConstantItem(80).AlignRight().Text($"${paid:N2}");
                                });
                                c.Item().Row(rr =>
                                {
                                    rr.RelativeItem().Text("Balance:").Bold();
                                    rr.ConstantItem(80).AlignRight().Text($"${balance:N2}").Bold();
                                });
                            });
                        });

                        // Payments detail
                        content.Item().PaddingTop(12).Column(c =>
                        {
                            c.Item().Text("Payments").SemiBold();
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
                                        header.Cell().Text("Date").SemiBold();
                                        header.Cell().AlignRight().Text("Amount").SemiBold();
                                    });

                                    foreach (var p in payments)
                                    {
                                        table.Cell().Text(p.sys_date.ToString("yyyy-MM-dd"));
                                        table.Cell().AlignRight().Text($"${p.sys_amount:N2}");
                                    }
                                });
                            }
                            else
                            {
                                c.Item().Text("No payments recorded").Italic();
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span(companyName).SemiBold();
                        txt.Span(" • Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            byte[] pdf = doc.GeneratePdf();
            return pdf;
        }

        private static IContainer ComposeItemsTable(List<tbl_InvoiceItems> items)
        {
            return Container.Create(container =>
            {
                container.Table(table =>
                {
                    table.ColumnsDefinition(cd =>
                    {
                        cd.RelativeColumn(4); // name
                        cd.RelativeColumn(1); // price
                    });

                    // header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Item").SemiBold();
                        header.Cell().Element(CellStyle).AlignRight().Text("Cost").SemiBold();
                    });

                    foreach (var it in items)
                    {
                        table.Cell().Element(CellStyle).Text(it.sys_itemName);
                        table.Cell().Element(CellStyle).AlignRight().Text($"${it.sys_itemCost:N2}");
                    }
                });
            });
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.PaddingVertical(5).PaddingHorizontal(4);
        }
    }
}
*/