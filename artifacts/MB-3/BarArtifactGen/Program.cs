using Microsoft.Extensions.Logging.Abstractions;
using PsyApi.Models;
using PsyApi.Services;
using PsyApi.Services.Reports;
using PsyApi.Services.Scoring;

var repoRoot = ResolveRepoRoot();
var releaseDir = Path.Combine(repoRoot, "backend", "PsyApi", "bin", "Release", "net8.0");

if (!Directory.Exists(releaseDir))
	throw new DirectoryNotFoundException($"Release build not found at {releaseDir}. Run dotnet build first.");

AppDomain.CurrentDomain.SetData("APP_CONTEXT_BASE_DIRECTORY", releaseDir);
Directory.SetCurrentDirectory(releaseDir);

DesignTokens.CurrentTheme = DesignTokens.ReportThemeMode.AuroraNeo;
LocalizationStrings.CurrentLanguage = ReportLanguage.AR;

var recommendationService = new RecommendationService(NullLogger<RecommendationService>.Instance);
var pdfService = new ModernPdfReportService(recommendationService);

var outputDir = Path.Combine(repoRoot, "artifacts", "MB-3");
Directory.CreateDirectory(outputDir);

var result = new Result
{
	Id = 9999,
	SessionId = 9003,
	TotalScore = 418,
	CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
	ScoringModelVersion = "AuroraNeo-MB3"
};

var user = new User
{
	FullName = "اختبار تجريبي",
	NationalId = "TEST-001",
	Username = "test.user",
	PasswordHash = "placeholder",
	Role = "user"
};

var dimensions = CreateDimensions();

Console.WriteLine("Generating MB-3 PDF...");
var pdfBytes = await pdfService.RenderResultPdfAsync(result, user, dimensions);
var pdfPath = Path.Combine(outputDir, "UltraHiFi_MB3_AuroraNeo.pdf");
await File.WriteAllBytesAsync(pdfPath, pdfBytes);

Console.WriteLine("Exporting MB-3 horizontal bars @600dpi...");
var barsPng = HorizontalBarChartRenderer.RenderHorizontalBars(dimensions, width: 640, maxDimensions: 12, dpi: 600);
var pngPath = Path.Combine(outputDir, "UltraHiFi_MB3_Bars@2x.png");
await File.WriteAllBytesAsync(pngPath, barsPng);

Console.WriteLine($"✓ PDF saved: {pdfPath}");
Console.WriteLine($"✓ Bars PNG saved: {pngPath}");

static string ResolveRepoRoot()
{
	var directory = new DirectoryInfo(AppContext.BaseDirectory);
	for (int i = 0; i < 7; i++)
	{
		if (directory.Parent == null)
			break;

		directory = directory.Parent;
		if (string.Equals(directory.Name, "psy-tests-platform", StringComparison.OrdinalIgnoreCase))
			return directory.FullName;
	}

	throw new InvalidOperationException("Unable to locate repository root from current directory.");
}

static List<DimensionScore> CreateDimensions() => new()
{
	new DimensionScore { Dimension = "القيادة الاستراتيجية", Raw = 62, Z = 1.2, T = 62, Percentile = 0.86 },
	new DimensionScore { Dimension = "التخطيط والتنظيم", Raw = 58, Z = 0.8, T = 58, Percentile = 0.78 },
	new DimensionScore { Dimension = "حل المشكلات", Raw = 64, Z = 1.4, T = 64, Percentile = 0.90 },
	new DimensionScore { Dimension = "التواصل", Raw = 55, Z = 0.5, T = 55, Percentile = 0.72 },
	new DimensionScore { Dimension = "الذكاء العاطفي", Raw = 52, Z = 0.2, T = 52, Percentile = 0.62 },
	new DimensionScore { Dimension = "العمل الجماعي", Raw = 57, Z = 0.7, T = 57, Percentile = 0.76 },
	new DimensionScore { Dimension = "المرونة والتكيف", Raw = 49, Z = -0.1, T = 49, Percentile = 0.55 },
	new DimensionScore { Dimension = "الابتكار والإبداع", Raw = 61, Z = 1.1, T = 61, Percentile = 0.84 },
	new DimensionScore { Dimension = "اتخاذ القرار", Raw = 46, Z = -0.4, T = 46, Percentile = 0.45 },
	new DimensionScore { Dimension = "إدارة الوقت", Raw = 54, Z = 0.4, T = 54, Percentile = 0.69 },
	new DimensionScore { Dimension = "التركيز والدقة", Raw = 51, Z = 0.1, T = 51, Percentile = 0.60 },
	new DimensionScore { Dimension = "إدارة المخاطر", Raw = 47, Z = -0.3, T = 47, Percentile = 0.48 },
	new DimensionScore { Dimension = "المثابرة", Raw = 59, Z = 0.9, T = 59, Percentile = 0.80 },
	new DimensionScore { Dimension = "الثقة بالنفس", Raw = 63, Z = 1.3, T = 63, Percentile = 0.88 },
	new DimensionScore { Dimension = "تحليل البيانات", Raw = 56, Z = 0.6, T = 56, Percentile = 0.74 },
	new DimensionScore { Dimension = "التفكير النقدي", Raw = 60, Z = 1.0, T = 60, Percentile = 0.82 }
};
