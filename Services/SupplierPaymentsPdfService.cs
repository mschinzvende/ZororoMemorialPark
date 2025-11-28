using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using ZororoMemorialPark.Pages.Reports;
using ZororoMemorialPark.Data.Models;

namespace ZororoMemorialPark.Services
{
    public class SupplierPaymentsPdfService
    {
        public byte[] GenerateSupplierPaymentsPdf(
            List<SupplierPaymentRow> rows,
            DateOnly? fromDate,
            DateOnly? toDate,
            int? selectedSupplierId,
            int? selectedStageId,
            int? selectedSectionId,
            double totalDue,
            double totalPaid,
            double totalBalance,
            string companyName,
            string companyAddress,
            string companyPhone,
            string companyEmail,
            string logoPath,
            List<tbl_Suppliers> supplierLookup,
            List<tbl_DevelopmentStages> stageLookup,
            List<tbl_Sections> sectionLookup)
        {
            // resolve filter labels
            string supplierName = selectedSupplierId.HasValue
                ? supplierLookup.FirstOrDefault(s => s.sys_supplierId == selectedSupplierId.Value)?.sys_supplierName
                : null;

            string stageName = selectedStageId.HasValue
                ? stageLookup.FirstOrDefault(s => s.sys_stageId == selectedStageId.Value)?.sys_stageName
                : null;

            string sectionName = selectedSectionId.HasValue
                ? sectionLookup.FirstOrDefault(s => s.sys_sectionId == selectedSectionId.Value)?.sys_sectionName
                : null;

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(t => t.FontSize(10));

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

                            col.Item().PaddingTop(6).AlignRight().Text("Supplier Payments Report")
                                .Bold().FontSize(14).FontColor(Colors.Green.Darken2);

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

                            if (!string.IsNullOrWhiteSpace(rangeText))
                                col.Item().AlignRight().Text(rangeText).FontSize(9);

                            if (!string.IsNullOrWhiteSpace(supplierName))
                                col.Item().AlignRight().Text($"Supplier: {supplierName}").FontSize(9);

                            if (!string.IsNullOrWhiteSpace(stageName))
                                col.Item().AlignRight().Text($"Stage: {stageName}").FontSize(9);

                            if (!string.IsNullOrWhiteSpace(sectionName))
                                col.Item().AlignRight().Text($"Section: {sectionName}").FontSize(9);

                            col.Item().AlignRight().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}")
                                .FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });

                    // CONTENT
                    page.Content().PaddingTop(10).Column(content =>
                    {
                        // Summary cards row
                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Element(e =>
                            {
                                SummaryCard(e, "Total Amount Due", totalDue, Colors.Blue.Medium);
                            });
                            row.RelativeItem().Element(e =>
                            {
                                SummaryCard(e, "Total Amount Paid", totalPaid, Colors.Green.Darken2);
                            });
                            row.RelativeItem().Element(e =>
                            {
                                SummaryCard(e, "Outstanding Balance", totalBalance, Colors.Red.Darken2);
                            });
                        });

                        content.Item().PaddingTop(10).Element(e =>
                        {
                            BuildTable(e, rows);
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span(companyName).SemiBold();
                        txt.Span(" • Supplier Payments • Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private void SummaryCard(IContainer container, string title, double amount, string color)
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

        private void BuildTable(IContainer container, List<SupplierPaymentRow> rows)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(60);   // Date
                    columns.RelativeColumn(1.4f); // Supplier
                    columns.RelativeColumn(1.0f); // Section
                    columns.ConstantColumn(55);   // Plot
                    columns.RelativeColumn(1.2f); // Stage
                    columns.ConstantColumn(70);   // Due
                    columns.ConstantColumn(70);   // Paid
                    columns.ConstantColumn(80);   // Balance
                    columns.RelativeColumn(1.6f); // Notes
                });

                // HEADER
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Date");
                    header.Cell().Element(HeaderCell).Text("Supplier");
                    header.Cell().Element(HeaderCell).Text("Section");
                    header.Cell().Element(HeaderCell).Text("Plot");
                    header.Cell().Element(HeaderCell).Text("Stage");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount Due");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Paid");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Balance");
                    header.Cell().Element(HeaderCell).Text("Notes");
                });

                foreach (var r in rows.OrderBy(x => x.Date).ThenBy(x => x.SupplierName))
                {
                    table.Cell().Element(BodyCell).Text(r.Date.ToString("yyyy-MM-dd"));
                    table.Cell().Element(BodyCell).Text(r.SupplierName);
                    table.Cell().Element(BodyCell).Text(r.SectionName);
                    table.Cell().Element(BodyCell).Text(r.PlotNumber);
                    table.Cell().Element(BodyCell).Text(r.StageName);
                    table.Cell().Element(BodyCell).AlignRight().Text($"${r.AmountDue:N2}");
                    table.Cell().Element(BodyCell).AlignRight().Text($"${r.AmountPaid:N2}");

                    table.Cell().Element(c =>
                    {
                        var styled = BodyCell(c);
                        if (r.Balance > 0)
                        {
                            styled = styled.Background(Colors.Red.Lighten5);
                        }

                        styled.AlignRight()
                              .Text($"${r.Balance:N2}")
                              .FontColor(r.Balance > 0 ? Colors.Red.Darken2 : Colors.Black);
                    });

                    table.Cell().Element(BodyCell).Text(r.Notes ?? "");
                }

                if (!rows.Any())
                {
                    table.Cell().ColumnSpan(9)
                        .Element(BodyCell)
                        .AlignCenter()
                        .Text("No expenses found for the selected filters.");
                }
            });
        }

        private IContainer HeaderCell(IContainer container)
        {
            return container
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(t => t.SemiBold().FontSize(9))
                .Background(Colors.Grey.Lighten3);
        }

        private IContainer BodyCell(IContainer container)
        {
            return container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .PaddingVertical(2)
                .PaddingHorizontal(3);
        }
    }
}
