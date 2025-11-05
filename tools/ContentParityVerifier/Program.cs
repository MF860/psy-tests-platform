using System.Text.Json;
using System.Text.Json.Serialization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ContentParityVerifier;

/// <summary>
/// Content Parity Verifier v6.1
/// Validates that the new 14-page report contains ALL content from the baseline.
/// Checks: page count, sections, tables, donut detection, garbled text.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("  Content Parity Verifier v6.1 — UltraHiFi AuroraNeo");
        Console.WriteLine("═══════════════════════════════════════════════════════════\n");

        var basePath = FindProjectRoot();
        if (basePath == null)
        {
            Console.WriteLine("❌ Could not find project root (no .git or .sln found)");
            return 1;
        }

        var artifactsPath = Path.Combine(basePath, "artifacts");
        var baselinePath = Path.Combine(artifactsPath, "baseline", "old_report.pdf");
        var newPath = Path.Combine(artifactsPath, "new", "new_report.pdf");
        var verifyPath = Path.Combine(artifactsPath, "verify", "parity.json");

        Console.WriteLine($"📂 Artifacts Path: {artifactsPath}");
        Console.WriteLine($"📄 Baseline PDF: {(File.Exists(baselinePath) ? "✓ Found" : "✗ Not Found")}");
        Console.WriteLine($"📄 New PDF: {(File.Exists(newPath) ? "✓ Found" : "✗ Not Found")}\n");

        if (!File.Exists(newPath))
        {
            Console.WriteLine("⚠️  New report not found. Generate it first using OfflineReportHarness.");
            return WriteParityResult(verifyPath, new ParityResult
            {
                Status = "FAIL",
                OldPages = 0,
                NewPages = 0,
                MissingSections = new List<string> { "New report file not found" },
                MissingTables = new List<string>(),
                DonutDetected = false,
                Message = "New report does not exist"
            });
        }

        // Analyze new report
        var newManifest = AnalyzePdf(newPath, "new");
        
        // If baseline exists, compare
        ParityResult result;
        if (File.Exists(baselinePath))
        {
            Console.WriteLine("\n🔍 Baseline found - performing full comparison...\n");
            var oldManifest = AnalyzePdf(baselinePath, "old");
            result = CompareManifests(oldManifest, newManifest);
        }
        else
        {
            Console.WriteLine("\n⚠️  No baseline found - validating new report only...\n");
            result = ValidateNewReport(newManifest);
        }

        // Write result
        var exitCode = WriteParityResult(verifyPath, result);
        
        Console.WriteLine($"\n{'═', 63}");
        Console.WriteLine($"  VALIDATION {result.Status}");
        Console.WriteLine($"{'═', 63}\n");

        if (result.Status == "PASS")
        {
            Console.WriteLine("✅ All checks passed!");
            Console.WriteLine($"   • Pages: {result.NewPages} (>= 14 required)");
            Console.WriteLine($"   • Donut Charts: {(result.DonutDetected ? "❌ DETECTED" : "✓ None")}");
            Console.WriteLine($"   • Missing Sections: {result.MissingSections.Count}");
            Console.WriteLine($"   • Missing Tables: {result.MissingTables.Count}");
        }
        else
        {
            Console.WriteLine("❌ Validation failed:");
            if (result.MissingSections.Any())
            {
                Console.WriteLine($"\n   Missing Sections ({result.MissingSections.Count}):");
                foreach (var sec in result.MissingSections)
                    Console.WriteLine($"     • {sec}");
            }
            if (result.MissingTables.Any())
            {
                Console.WriteLine($"\n   Missing Tables ({result.MissingTables.Count}):");
                foreach (var tbl in result.MissingTables)
                    Console.WriteLine($"     • {tbl}");
            }
            if (result.DonutDetected)
            {
                Console.WriteLine("\n   ⚠️  Donut chart detected (FORBIDDEN in v6.1)");
            }
            if (!string.IsNullOrEmpty(result.Message))
            {
                Console.WriteLine($"\n   Message: {result.Message}");
            }
        }

        Console.WriteLine($"\n📝 Parity report written to: {verifyPath}\n");
        return exitCode;
    }

    static PdfManifest AnalyzePdf(string path, string label)
    {
        Console.WriteLine($"📖 Analyzing {label.ToUpper()} PDF: {Path.GetFileName(path)}");
        
        var manifest = new PdfManifest { FilePath = path };

        try
        {
            using var document = PdfDocument.Open(path);
            manifest.PageCount = document.NumberOfPages;
            
            Console.WriteLine($"   Pages: {manifest.PageCount}");

            // Extract text from all pages
            var allText = new List<string>();
            for (int i = 1; i <= document.NumberOfPages; i++)
            {
                var page = document.GetPage(i);
                var text = page.Text;
                allText.Add(text);
                
                // Detect sections (look for common Arabic/English headers)
                var sectionMatches = new[] { "تفاصيل", "توصيات", "مخاطر", "ملحق", "مسرد", "Quality", "Methodology", 
                    "Details", "Recommendations", "Risk", "Appendix", "Glossary", "Percentile", "Tracking" };
                
                foreach (var keyword in sectionMatches)
                {
                    if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!manifest.Sections.Contains(keyword))
                            manifest.Sections.Add(keyword);
                    }
                }
                
                // Detect donut charts (look for "donut", "دونات", "المحاور" with circular patterns)
                if (text.Contains("donut", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("دونات") ||
                    (text.Contains("المحاور") && text.Contains("توزيع") && i <= 3))
                {
                    manifest.DonutDetected = true;
                    Console.WriteLine($"   ⚠️  Possible donut chart detected on page {i}");
                }
                
                // Check for garbled text
                if (text.Contains('�') || text.Contains('\uFFFD'))
                {
                    manifest.HasGarbledText = true;
                    Console.WriteLine($"   ⚠️  Garbled text detected on page {i}");
                }
            }

            // Count tables (rough heuristic: multiple aligned columns)
            var fullText = string.Join("\n", allText);
            manifest.TableCount = CountTables(fullText);
            
            Console.WriteLine($"   Sections detected: {manifest.Sections.Count}");
            Console.WriteLine($"   Tables detected: {manifest.TableCount}");
            Console.WriteLine($"   Donut charts: {(manifest.DonutDetected ? "YES (❌)" : "NO (✓)")}");
            Console.WriteLine($"   Garbled text: {(manifest.HasGarbledText ? "YES (❌)" : "NO (✓)")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Error analyzing PDF: {ex.Message}");
        }

        return manifest;
    }

    static int CountTables(string text)
    {
        // Simple heuristic: count lines with 3+ columns separated by spaces/tabs
        var lines = text.Split('\n');
        int tableCount = 0;
        bool inTable = false;
        
        foreach (var line in lines)
        {
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && parts.Length <= 10)
            {
                if (!inTable)
                {
                    tableCount++;
                    inTable = true;
                }
            }
            else if (string.IsNullOrWhiteSpace(line))
            {
                inTable = false;
            }
        }
        
        return tableCount;
    }

    static ParityResult CompareManifests(PdfManifest oldManifest, PdfManifest newManifest)
    {
        var result = new ParityResult
        {
            OldPages = oldManifest.PageCount,
            NewPages = newManifest.PageCount,
            DonutDetected = newManifest.DonutDetected,
            MissingSections = new List<string>(),
            MissingTables = new List<string>()
        };

        // Rule 1: New must have >= 14 pages
        if (newManifest.PageCount < 14)
        {
            result.Status = "FAIL";
            result.Message = $"Page count {newManifest.PageCount} < 14 (required minimum)";
            return result;
        }

        // Rule 2: No donut charts allowed
        if (newManifest.DonutDetected)
        {
            result.Status = "FAIL";
            result.Message = "Donut chart detected (FORBIDDEN in v6.1)";
            return result;
        }

        // Rule 3: No garbled text
        if (newManifest.HasGarbledText)
        {
            result.Status = "FAIL";
            result.Message = "Garbled text (�) detected - Arabic normalization failed";
            return result;
        }

        // Rule 4: All old sections must exist in new (or new has equivalents)
        foreach (var oldSection in oldManifest.Sections)
        {
            if (!newManifest.Sections.Contains(oldSection))
            {
                result.MissingSections.Add(oldSection);
            }
        }

        // Rule 5: New should have >= old table count (content parity)
        if (newManifest.TableCount < oldManifest.TableCount)
        {
            result.MissingTables.Add($"Table count: old={oldManifest.TableCount}, new={newManifest.TableCount} (expected >=)");
        }

        // Determine pass/fail
        if (result.MissingSections.Count == 0 && result.MissingTables.Count == 0)
        {
            result.Status = "PASS";
            result.Message = "All content parity checks passed";
        }
        else
        {
            result.Status = "FAIL";
            result.Message = $"Missing {result.MissingSections.Count} sections, {result.MissingTables.Count} table mismatches";
        }

        return result;
    }

    static ParityResult ValidateNewReport(PdfManifest newManifest)
    {
        var result = new ParityResult
        {
            OldPages = 0,
            NewPages = newManifest.PageCount,
            DonutDetected = newManifest.DonutDetected,
            MissingSections = new List<string>(),
            MissingTables = new List<string>()
        };

        // Without baseline, just validate constraints
        if (newManifest.PageCount < 14)
        {
            result.Status = "FAIL";
            result.Message = $"Page count {newManifest.PageCount} < 14 (required)";
            return result;
        }

        if (newManifest.DonutDetected)
        {
            result.Status = "FAIL";
            result.Message = "Donut chart detected (FORBIDDEN)";
            return result;
        }

        if (newManifest.HasGarbledText)
        {
            result.Status = "FAIL";
            result.Message = "Garbled text detected";
            return result;
        }

        // Check for required sections in v6.1
        var requiredSections = new[] { "Details", "Risk", "Recommendations", "Tracking", "Appendix", "Glossary" };
        foreach (var req in requiredSections)
        {
            if (!newManifest.Sections.Any(s => s.Contains(req, StringComparison.OrdinalIgnoreCase)))
            {
                result.MissingSections.Add(req);
            }
        }

        if (result.MissingSections.Count == 0)
        {
            result.Status = "PASS";
            result.Message = "New report validated successfully";
        }
        else
        {
            result.Status = "FAIL";
            result.Message = $"Missing required sections: {string.Join(", ", result.MissingSections)}";
        }

        return result;
    }

    static int WriteParityResult(string path, ParityResult result)
    {
        try
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions 
            { 
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            });
            
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, json);
            
            return result.Status == "PASS" ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to write parity result: {ex.Message}");
            return 1;
        }
    }

    static string? FindProjectRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(dir))
        {
            if (Directory.Exists(Path.Combine(dir, ".git")) ||
                Directory.GetFiles(dir, "*.sln").Any())
            {
                return dir;
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }
}

class PdfManifest
{
    public string FilePath { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public List<string> Sections { get; set; } = new();
    public int TableCount { get; set; }
    public bool DonutDetected { get; set; }
    public bool HasGarbledText { get; set; }
}

class ParityResult
{
    public string Status { get; set; } = "UNKNOWN";
    public int OldPages { get; set; }
    public int NewPages { get; set; }
    public List<string> MissingSections { get; set; } = new();
    public List<string> MissingTables { get; set; } = new();
    public bool DonutDetected { get; set; }
    public string Message { get; set; } = string.Empty;
}
