using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZororoMemorialPark.Data.Models;
using ZororoMemorialPark.Pages.Burials;

namespace ZororoMemorialPark.Services
{
    public class BurialSchedulePdfService
    {
        public byte[] GenerateSchedulePdf(
            List<BurialScheduleVM> burials,
            SystemSettings settings,
            string logoPath
        )
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var themeGreen = "#006A46";

            var doc = Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor("#F5F5F5"); // light grey background

                    page.DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black));

                    // ================= HEADER =================
                    page.Header().Background("#006A46").Padding(15).Row(row =>
                    {
                        // ================= LEFT: COMPANY =================
                        row.RelativeItem().AlignMiddle().Column(col =>
                        {
                            col.Item().Text(settings.CompanyName)
                                .Bold().FontSize(13).FontColor(Colors.White);

                            col.Item().Text(settings.PhysicalAddress)
                                .FontSize(9).FontColor(Colors.White);

                            col.Item().Text($"{settings.Phone} | {settings.Email}")
                                .FontSize(9).FontColor(Colors.White);
                        });

                        // ================= CENTER: LOGO =================
                        row.ConstantItem(100).AlignCenter().AlignMiddle().Element(container =>
                        {
                            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
                            {
                                container
                                    .Height(55)
                                    .AlignCenter()
                                    .Image(logoPath, ImageScaling.FitHeight);
                            }
                        });

                        // ================= RIGHT: TITLE =================
                        row.RelativeItem().AlignMiddle().AlignRight().Column(col =>
                        {
                            col.Item().AlignRight().Text("BURIAL SCHEDULE")
                                .Bold().FontSize(14).FontColor(Colors.White);

                            col.Item().AlignRight().Text($"Generated: {DateTime.Now:dd MMM yyyy}")
                                .FontSize(9).FontColor(Colors.White);
                        });
                    });

                    // ================= BODY CARD =================
                    page.Content().PaddingTop(15).Column(content =>
                    {
                        content.Item().Background(Colors.White).Padding(12).Column(card =>
                        {
                            // TABLE
                            card.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cd =>
                                {
                                    cd.ConstantColumn(75);
                                    cd.ConstantColumn(50);
                                    cd.RelativeColumn(3);
                                    cd.RelativeColumn(2);
                                    cd.RelativeColumn(2);
                                    cd.ConstantColumn(90);
                                });

                                // HEADER
                                table.Header(header =>
                                {
                                    header.Cell().Element(TableHeader).Text("Date");
                                    header.Cell().Element(TableHeader).Text("Time");
                                    header.Cell().Element(TableHeader).Text("Deceased");
                                    header.Cell().Element(TableHeader).Text("Section");
                                    header.Cell().Element(TableHeader).Text("Customer");
                                    header.Cell().Element(TableHeader).AlignCenter().Text("Payment");
                                });

                                foreach (var b in burials)
                                {
                                    table.Cell().Element(TableCell).Text(b.BurialDateTime.ToString("dd MMM yyyy"));
                                    table.Cell().Element(TableCell).Text(b.BurialDateTime.ToString("HH:mm"));
                                    table.Cell().Element(TableCell).Text(b.DeceasedFullName);
                                    table.Cell().Element(TableCell).Text(b.SectionName);
                                    table.Cell().Element(TableCell).Text(b.CustomerName);

                                    // PAYMENT STATUS
                                    table.Cell().Element(TableCell).AlignCenter().Column(col =>
                                    {
                                        if (b.IsFullyPaid)
                                        {
                                            col.Item().Text("PAID")
                                                .FontColor("#0A7D2C")
                                                .SemiBold();
                                        }
                                        else
                                        {
                                            col.Item().Text("PENDING")
                                                .FontColor("#D62828")
                                                .SemiBold();

                                            col.Item().Text($"${b.Balance:N2}")
                                                .FontSize(8)
                                                .FontColor("#D62828");
                                        }
                                    });
                                }
                            });

                            // ================= SUMMARY =================
                            card.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);

                            card.Item().PaddingTop(8).Row(row =>
                            {
                                row.RelativeItem().Text($"Total Burials: {burials.Count}")
                                    .SemiBold();

                                row.RelativeItem().AlignRight().Text(
                                    $"Paid: {burials.Count(x => x.IsFullyPaid)}   Pending: {burials.Count(x => !x.IsFullyPaid)}"
                                );
                            });
                        });
                    });

                    // ================= FOOTER =================
                    page.Footer().AlignCenter().Text(txt =>
                    {
                        txt.Span(settings.CompanyName).FontSize(8);
                        txt.Span(" • Page ");
                        txt.CurrentPageNumber();
                        txt.Span(" of ");
                        txt.TotalPages();
                    });
                });
            });

            return doc.GeneratePdf();
        }

        private static IContainer TableCell(IContainer container)
        {
            return container
                .PaddingVertical(6)
                .PaddingHorizontal(4)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2);
        }

        private static IContainer TableHeader(IContainer container)
        {
            return container
                .PaddingVertical(6)
                .PaddingHorizontal(4)
                .BorderBottom(1)
                .BorderColor("#006A46")
                .DefaultTextStyle(x => x.SemiBold().FontSize(9));
        }


        // ================= CELL =================
        private static IContainer Cell(IContainer container)
        {
            return container
                .PaddingVertical(3)
                .PaddingHorizontal(2)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2);
        }

        // ================= HEADER CELL =================
        private static IContainer HeaderCell(IContainer container, string color)
        {
            return container
                .Background(color)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.White).SemiBold());
        }
    }
}