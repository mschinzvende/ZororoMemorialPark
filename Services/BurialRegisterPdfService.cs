using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using ZororoMemorialPark.Pages.Reports;

namespace ZororoMemorialPark.Services
{
    public class BurialRegisterPdfService
    {
        private readonly string PrimaryColor = "#006A46";
        private readonly string HeaderBackground = Colors.Grey.Lighten4;
        private readonly string TableHeaderBackground = Colors.Green.Lighten4;
        private readonly string RowStrip = Colors.Grey.Lighten3;

        public byte[] GenerateBurialRegisterPdf(
            List<BurialRegisterRow> rows,
            string companyName,
            string companyAddress,
            string companyPhone,
            string companyEmail,
            string logoPath,
            DateOnly? fromDate,
            DateOnly? toDate,
            string sectionNameFilter)
        {
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());      // LANDSCAPE MODE
                    page.Margin(25);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Black));

                    //=========================================
                    // HEADER
                    //=========================================
                    page.Header().Background(HeaderBackground).Padding(10).Row(row =>
                    {
                        row.ConstantItem(90).Column(col =>
                        {
                            if (!string.IsNullOrWhiteSpace(logoPath))
                                col.Item().Height(60).Image(logoPath, ImageScaling.FitWidth);
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(companyName).Bold().FontSize(20).FontColor(PrimaryColor);
                            col.Item().Text(companyAddress).FontSize(10);
                            col.Item().Text($"{companyPhone}  |  {companyEmail}").FontSize(10);

                            col.Item().PaddingTop(8)
                                .Text("BURIAL REGISTER REPORT")
                                .Bold()
                                .FontSize(14)
                                .FontColor(PrimaryColor);

                            // DATE RANGE
                            if (fromDate.HasValue || toDate.HasValue)
                            {
                                string range = "Date Range: ";
                                if (fromDate.HasValue) range += fromDate.Value;
                                range += " to ";
                                if (toDate.HasValue) range += toDate.Value;

                                col.Item().PaddingTop(4).Text(range).FontSize(10);
                            }

                            // SECTION
                            if (!string.IsNullOrWhiteSpace(sectionNameFilter))
                            {
                                col.Item().Text($"Section: {sectionNameFilter}").FontSize(10);
                            }
                        });
                    });

                    //=========================================
                    // CONTENT
                    //=========================================
                    page.Content().PaddingTop(10).Column(content =>
                    {
                        // Summary bar
                        content.Item().Background(Colors.Grey.Lighten5).Padding(8).Row(r =>
                        {
                            r.RelativeItem().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
                            r.RelativeItem().AlignRight()
                                .Text($"Total Burials: {rows.Count}").Bold().FontColor(PrimaryColor);
                        });

                        // MAIN TABLE
                        content.Item().PaddingTop(10).Element(container =>
                        {
                            container.Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(75);
                                    columns.ConstantColumn(55);
                                    columns.RelativeColumn(1.0f);
                                    columns.RelativeColumn(1.0f);
                                    columns.RelativeColumn(2.0f);
                                    columns.ConstantColumn(50);
                                    columns.ConstantColumn(45);
                                    columns.RelativeColumn(1.0f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.2f);
                                });

                                // HEADER
                                table.Header(header =>
                                {
                                    header.Cell().Element(TableHeader).Text("Date");
                                    header.Cell().Element(TableHeader).Text("Time");
                                    header.Cell().Element(TableHeader).Text("Section");
                                    header.Cell().Element(TableHeader).Text("Plot");
                                    header.Cell().Element(TableHeader).Text("Deceased");
                                    header.Cell().Element(TableHeader).Text("Gender");
                                    header.Cell().Element(TableHeader).AlignRight().Text("Age");
                                    header.Cell().Element(TableHeader).Text("Nationality");
                                    header.Cell().Element(TableHeader).Text("Customer");
                                    header.Cell().Element(TableHeader).Text("Phone");
                                });

                                int index = 0;

                                foreach (var r in rows)
                                {
                                    bool stripe = index % 2 == 0;
                                    index++;

                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.BurialDateTime.ToString("yyyy-MM-dd"));
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.BurialDateTime.ToString("HH:mm"));
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.SectionName);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.PlotNumber);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.DeceasedName);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.DeceasedGender);
                                    table.Cell().Element(c => TableBody(c, stripe)).AlignRight().Text(r.AgeAtDeath?.ToString() ?? "-");
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.DeceasedNationality);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.CustomerName);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.CustomerPhone);
                                }
                            });
                        });

                        // OPTIONAL EXTRA TABLE
                        if (rows.Any(r => !string.IsNullOrWhiteSpace(r.RepatriationSource) ||
                                          !string.IsNullOrWhiteSpace(r.BurialOrder)))
                        {
                            content.Item().PaddingTop(15).Text("Additional Case Details")
                                .Bold().FontColor(PrimaryColor).FontSize(13);

                            content.Item().PaddingTop(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80);
                                    columns.RelativeColumn(1.0f);
                                    columns.RelativeColumn(1.8f);
                                    columns.RelativeColumn(1.5f);
                                    columns.RelativeColumn(1.5f);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(TableHeader).Text("Date");
                                    header.Cell().Element(TableHeader).Text("Plot");
                                    header.Cell().Element(TableHeader).Text("Deceased");
                                    header.Cell().Element(TableHeader).Text("Repatriation");
                                    header.Cell().Element(TableHeader).Text("Burial Order No.");
                                });

                                int idx = 0;
                                foreach (var r in rows)
                                {
                                    bool stripe = idx % 2 == 0;
                                    idx++;

                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.BurialDateTime.ToString("yyyy-MM-dd"));
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.PlotNumber);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.DeceasedName);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.RepatriationSource);
                                    table.Cell().Element(c => TableBody(c, stripe)).Text(r.BurialOrder);
                                }
                            });
                        }
                    });

                    // FOOTER
                    page.Footer().Background(PrimaryColor)
                        .Padding(8)
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.DefaultTextStyle(x => x.FontColor(Colors.White));
                            txt.Span(companyName + "  •  Burial Register Report  •  Page ");
                            txt.CurrentPageNumber();
                            txt.Span(" of ");
                            txt.TotalPages();
                        });
                });
            });

            return doc.GeneratePdf();
        }

        private IContainer TableHeader(IContainer container) =>
            container
                .Background(TableHeaderBackground)
                .PaddingVertical(5)
                .PaddingHorizontal(4)
                .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.Black));

        private IContainer TableBody(IContainer container, bool stripe) =>
            container
                .Background(stripe ? RowStrip : Colors.White)
                .PaddingVertical(3)
                .PaddingHorizontal(4)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2);
    }
}
