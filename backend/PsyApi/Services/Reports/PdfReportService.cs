using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PsyApi.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace PsyApi.Services.Reports
{
    using DimensionScoreDto = PsyApi.Models.DimensionScore;

    public class PdfReportService : IPdfReportService
    {
        private static bool _fontsRegistered;
        private static readonly object _lock = new();

        public PdfReportService()
        {
            EnsureFonts();
        }

        public Task<byte[]> RenderResultPdfAsync(Result result, User user, IEnumerable<DimensionScoreDto> dimensions, CancellationToken ct = default)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var dim = dimensions?.ToList() ?? new List<DimensionScoreDto>();

            byte[] bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.PageColor(Colors.White);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("تقرير التقييم النفسي").FontSize(18).AlignRight();
                            col.Item().Text("الجهة: PsyApi").AlignRight();
                        });
                        var logoPath = Path.Combine(AppContext.BaseDirectory, "Resources", "branding", "logo.png");
                        if (File.Exists(logoPath))
                        {
                            row.ConstantItem(80).Image(logoPath, ImageScaling.FitArea);
                        }
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(6);
                        col.Item().BorderBottom(1).PaddingBottom(6);
                        col.Item().Text($"الرقم الوطني: {user.NationalId}").AlignRight();
                        col.Item().Text($"الاسم: {user.FullName ?? "N/A"}").AlignRight();
                        col.Item().Text($"رقم الجلسة: {result.SessionId}").AlignRight();
                        col.Item().Text($"تاريخ الإنشاء: {result.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}").AlignRight();
                        col.Item().Text($"النتيجة الإجمالية: {result.TotalScore}").Bold().AlignRight();

                        col.Item().PaddingTop(10).Text("الدرجات حسب الأبعاد:").Bold().AlignRight();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("البعد").Bold();
                                header.Cell().Element(CellStyle).Text("Raw");
                                header.Cell().Element(CellStyle).Text("T");
                                header.Cell().Element(CellStyle).Text("Percentile");

                                static IContainer CellStyle(IContainer c) => c.PaddingVertical(4).BorderBottom(1);
                            });

                            foreach (var d in dim)
                            {
                                table.Cell().Element(CellStyle).Text(d.Dimension).AlignRight();
                                table.Cell().Element(CellStyle).Text(d.Raw.ToString("0.###"));
                                table.Cell().Element(CellStyle).Text(d.T.ToString("0.0"));
                                table.Cell().Element(CellStyle).Text(d.Percentile.ToString("0.0"));

                                static IContainer CellStyle(IContainer c) => c.PaddingVertical(2).BorderBottom(1);
                            }
                        });
                    });

                    page.Footer().AlignRight().Text($"نسخة النموذج: {result.ScoringModelVersion ?? "n/a"} — تم الإنشاء: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9);
                });
            }).GeneratePdf();

            return Task.FromResult(bytes);
        }

        private static void EnsureFonts()
        {
            if (_fontsRegistered) return;
            lock (_lock)
            {
                if (_fontsRegistered) return;

                try
                {
                    // Optional custom font registration (if available)
                    // var fontPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "NotoNaskhArabic-Regular.ttf");
                    // if (File.Exists(fontPath)) { QuestPDF.Helpers.FontManager.RegisterFont(fontPath); }
                }
                catch { }
                _fontsRegistered = true;
            }
        }
    }
}
