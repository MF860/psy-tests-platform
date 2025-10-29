using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;
using Serilog;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace PsyApi.Services
{
    public class DataSeeder
    {
        // Canonical types used across BE/FE
        private static readonly HashSet<string> CanonicalTypes = new(StringComparer.Ordinal)
        {
            "MCQ", "Text", "ORDERING", "TIMED_NUMERIC", "LikertAgreement", "Frequency"
        };

        private const string LikertAgreementOptions = "لا أوافق بشدة|لا أوافق|محايد|أوافق|أوافق بشدة";
        private const string FrequencyOptions = "أبدًا|نادرًا|أحيانًا|غالبًا|دائمًا";

        private readonly AppDbContext _context;
        private readonly Serilog.ILogger _logger;

        public DataSeeder(AppDbContext context, Serilog.ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedItemsAsync(bool forceReseed = false)
        {
            if (await _context.Items.AnyAsync() && !forceReseed)
            {
                _logger.Information("Items table already contains data. Skipping seeding.");
                return;
            }

            if (forceReseed && await _context.Items.AnyAsync())
            {
                _logger.Information("Force reseeding: Clearing existing Items data.");
                _context.Items.RemoveRange(await _context.Items.ToListAsync());
                await _context.SaveChangesAsync();
            }

            var items = new List<Item>();
            var rejected = new List<(string ItemId, string Reason)>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
                        // SDJ V2 is now the DEFAULT mode. Set USE_SDJ=0 to use legacy questions, USE_SDJ=1 for V1.
            var sdjMode = Environment.GetEnvironmentVariable("USE_SDJ");
            var csvFileName = sdjMode == "0" 
                ? "questions.csv"  // Legacy mode (USE_SDJ=0)
                : (sdjMode == "1" 
                    ? "questions_sdj_ar.csv"  // SDJ V1 mode (USE_SDJ=1)
                    : "questions_sdj_v2_ar.csv");  // SDJ V2 mode (default, or USE_SDJ=2)
            
            var csvPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Questions", csvFileName);
            var modeLabel = sdjMode == "0" ? "LEGACY" : (sdjMode == "1" ? "SDJ V1" : "SDJ V2");
            _logger.Information("[{Mode}] Using CSV file: {CsvPath}", modeLabel, csvPath);

            try
            {
                if (!File.Exists(csvPath))
                {
                    _logger.Warning("CSV file {CsvPath} was not found.", csvPath);
                    return;
                }

                // Verify UTF-8 (no BOM) and comma delimiter via header
                var bytes = await File.ReadAllBytesAsync(csvPath);
                var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                var lines = await File.ReadAllLinesAsync(csvPath, new UTF8Encoding(false));
                if (lines.Length == 0)
                {
                    _logger.Warning("CSV file {CsvPath} is empty.", csvPath);
                    return;
                }
                var header = lines[0];
                _logger.Information("CSV header: {Header}", header);
                _logger.Information("CSV total lines (incl header): {Lines}; Encoding BOM: {BOM}", lines.Length, hasBom ? "present" : "absent");
                if (!header.Contains(','))
                {
                    _logger.Warning("CSV header does not appear to be comma-delimited.");
                }

                var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,
                    IgnoreBlankLines = true,
                    HeaderValidated = null,  // Allow missing headers (V1/V2 compatibility)
                    MissingFieldFound = null,  // Don't error on missing fields
                    BadDataFound = args => { try { _logger.Warning("Bad CSV data: {Raw}", args.RawRecord); } catch { } }
                };

                int parsedRows = 0;
                var isLegacy = sdjMode == "0";
                using (var reader = new StreamReader(csvPath, new UTF8Encoding(false)))
                using (var csv = new CsvReader(reader, cfg))
                {
                    if (!isLegacy)
                    {
                        // Parse SDJ CSV format (V1 or V2)
                        var records = csv.GetRecords<CsvSdjRow>();
                        foreach (var r in records)
                        {
                            parsedRows++;
                            
                            // Support both V1 (snake_case) and V2 (PascalCase) headers
                            var id = (!string.IsNullOrWhiteSpace(r.ItemCode) ? r.ItemCode : r.item_code ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(id)) { rejected.Add(("<missing>", "item_code missing")); continue; }
                            if (!seenIds.Add(id)) { rejected.Add((id, "duplicate item_code")); continue; }

                            var textAr = (!string.IsNullOrWhiteSpace(r.TextAr) ? r.TextAr : r.text_ar ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(textAr)) { rejected.Add((id, "text_ar missing")); continue; }

                            var typeRaw = !string.IsNullOrWhiteSpace(r.Type) ? r.Type : r.type ?? string.Empty;
                            var type = NormalizeType(typeRaw.Trim());
                            if (!CanonicalTypes.Contains(type)) { rejected.Add((id, $"unsupported type '{typeRaw}'")); continue; }

                            var dimension = (r.dimension ?? string.Empty).Trim();
                            var subDimension = (r.sub_dimension ?? string.Empty).Trim();
                            
                            // V2 Seven Patterns fields (optional, only present in questions_sdj_v2_ar.csv)
                            var patternId = (r.PatternId ?? string.Empty).Trim();
                            var patternKey = (r.PatternKey ?? string.Empty).Trim();
                            var patternNameAr = (r.PatternNameAr ?? string.Empty).Trim();
                            var subId = (r.SubId ?? string.Empty).Trim();
                            var subKey = (r.SubKey ?? string.Empty).Trim();
                            var subNameAr = (r.SubNameAr ?? string.Empty).Trim();
                            
                            // Determine if this is V2 by checking PatternId presence
                            bool isV2 = !string.IsNullOrWhiteSpace(patternId);
                            
                            if (!isV2)
                            {
                                // V1 validation: dimension and sub_dimension required
                                if (string.IsNullOrWhiteSpace(dimension) || string.IsNullOrWhiteSpace(subDimension))
                                {
                                    rejected.Add((id, "dimension or sub_dimension missing (V1 mode)"));
                                    continue;
                                }
                            }
                            else
                            {
                                // V2 validation: PatternId and SubId required
                                if (string.IsNullOrWhiteSpace(subId))
                                {
                                    rejected.Add((id, "SubId missing (V2 mode)"));
                                    continue;
                                }
                            }

                            // Parse difficulty - V2 doesn't have difficulty field
                            var difficultyStr = r.difficulty ?? string.Empty;
                            if (!int.TryParse(difficultyStr.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var difficulty) || difficulty < 1 || difficulty > 5)
                            {
                                if (!string.IsNullOrWhiteSpace(difficultyStr))
                                    _logger.Warning("Row {ItemId}: difficulty invalid '{Diff}', defaulting to 3", id, r.difficulty);
                                difficulty = 3;
                            }

                            // Parse time limit - V2 uses TimeLimitSeconds (PascalCase)
                            var timeLimitStr = !string.IsNullOrWhiteSpace(r.TimeLimitSeconds) ? r.TimeLimitSeconds : r.time_limit_seconds ?? string.Empty;
                            if (!int.TryParse(timeLimitStr.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeLimit) || timeLimit <= 0)
                            {
                                _logger.Warning("Row {ItemId}: time_limit_seconds invalid '{Sec}', defaulting to 45", id, timeLimitStr);
                                timeLimit = 45;
                            }

                            // Parse max score - V2 uses Weight instead of max_score
                            var maxScoreStr = !string.IsNullOrWhiteSpace(r.Weight) ? r.Weight : r.max_score ?? string.Empty;
                            if (!int.TryParse(maxScoreStr.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxScore) || maxScore <= 0)
                            {
                                _logger.Warning("Row {ItemId}: max_score invalid '{Max}', defaulting to 5", id, maxScoreStr);
                                maxScore = 5;
                            }

                            // Parse reverse - V2 uses Reverse (PascalCase)
                            var reverseStr = (!string.IsNullOrWhiteSpace(r.Reverse) ? r.Reverse : r.reverse ?? "0").Trim();
                            var reverse = reverseStr == "1" || string.Equals(reverseStr, "true", StringComparison.OrdinalIgnoreCase);

                            var anchorsAr = (r.anchors_ar ?? string.Empty).Trim();
                            
                            // MCQ fields (V2)
                            var correctAnswer = (r.CorrectAnswer ?? string.Empty).Trim();
                            var csvOptions = (r.Options ?? string.Empty).Trim();
                            
                            string? optionsStr = null;

                            switch (type)
                            {
                                case "LikertAgreement":
                                    optionsStr = !string.IsNullOrWhiteSpace(anchorsAr) ? anchorsAr : LikertAgreementOptions;
                                    break;
                                case "Frequency":
                                    optionsStr = !string.IsNullOrWhiteSpace(anchorsAr) ? anchorsAr : FrequencyOptions;
                                    break;
                                case "MCQ":
                                    optionsStr = !string.IsNullOrWhiteSpace(csvOptions) ? csvOptions : anchorsAr;
                                    if (string.IsNullOrWhiteSpace(optionsStr))
                                    {
                                        rejected.Add((id, "MCQ missing options"));
                                        continue;
                                    }
                                    break;
                                default:
                                    optionsStr = anchorsAr;
                                    break;
                            }

                            items.Add(new Item
                            {
                                ItemCode = id,
                                TextAr = textAr,
                                Type = type,
                                DimensionTags = isV2 
                                    ? $"{patternNameAr} > {subNameAr}"  // V2: Use pattern/sub names
                                    : $"{dimension} > {subDimension}",   // V1: Use dimension/subdimension
                                
                                // V1 fields (maintain for backward compatibility)
                                Dimension = isV2 ? patternNameAr : dimension,
                                SubDimension = isV2 ? subNameAr : subDimension,
                                Reverse = reverse,
                                
                                // V2 Seven Patterns fields (only populated for V2 CSV)
                                PatternId = isV2 ? patternId : null,
                                PatternKey = isV2 ? patternKey : null,
                                PatternNameAr = isV2 ? patternNameAr : null,
                                SubId = isV2 ? subId : null,
                                SubKey = isV2 ? subKey : null,
                                SubNameAr = isV2 ? subNameAr : null,
                                
                                Difficulty = difficulty,
                                TimeLimitSeconds = timeLimit,
                                MaxScore = maxScore,
                                CorrectAnswer = type == "MCQ" && !string.IsNullOrWhiteSpace(correctAnswer) ? correctAnswer : null,
                                Options = optionsStr
                            });
                        }
                    }
                    else
                    {
                        // Parse legacy CSV format
                        var records = csv.GetRecords<CsvQuestionRow>();
                    foreach (var r in records)
                    {
                        parsedRows++;
                        var id = (r.item_id ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(id)) { rejected.Add(("<missing>", "item_id missing")); continue; }
                        if (!seenIds.Add(id)) { rejected.Add((id, "duplicate item_id")); continue; }
                        
                        // Validate id format - should be "I" followed by numbers, or just numbers that we can convert
                        if (!id.StartsWith("I", StringComparison.OrdinalIgnoreCase) && !int.TryParse(id, out _))
                        {
                            rejected.Add((id, "item_id must start with 'I' followed by numbers, or be a number")); 
                            continue;
                        }

                        var textAr = (r.text_ar ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(textAr)) { rejected.Add((id, "text_ar missing")); continue; }

                        var type = NormalizeType((r.type ?? string.Empty).Trim());
                        if (!CanonicalTypes.Contains(type)) { rejected.Add((id, $"unsupported type '{r.type}'")); continue; }

                        var dimensionTags = (r.dimension_tags ?? string.Empty).Trim();
                        if (string.IsNullOrWhiteSpace(dimensionTags)) { rejected.Add((id, "dimension_tags missing")); continue; }

                        if (!int.TryParse((r.difficulty ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var difficulty) || difficulty < 1 || difficulty > 5)
                        {
                            _logger.Warning("Row {ItemId}: difficulty invalid '{Diff}', defaulting to 3", id, r.difficulty);
                            difficulty = 3;
                        }

                        if (!int.TryParse((r.time_limit_seconds ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var timeLimit) || timeLimit <= 0)
                        {
                            _logger.Warning("Row {ItemId}: time_limit_seconds invalid '{Sec}', defaulting to 45", id, r.time_limit_seconds);
                            timeLimit = 45;
                        }

                        if (!int.TryParse((r.max_score ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxScore) || maxScore <= 0)
                        {
                            _logger.Warning("Row {ItemId}: max_score invalid '{Max}', defaulting to 5", id, r.max_score);
                            maxScore = 5;
                        }

                        var correct = (r.correct_answer ?? string.Empty).Trim();
                        var optionsRaw = (r.options ?? string.Empty).Trim();
                        string? optionsStr = null; string? correctAnswer = null;
                        var rng = new Random(42);

                        switch (type)
                        {
                            case "LikertAgreement":
                                optionsStr = LikertAgreementOptions; correctAnswer = null; break;
                            case "Frequency":
                                optionsStr = FrequencyOptions; correctAnswer = null; break;
                            case "MCQ":
                            {
                                var tokens = SplitOptions(correct);
                                if (tokens.Count >= 2)
                                {
                                    correctAnswer = tokens[0];
                                    optionsStr = string.Join('|', tokens.OrderBy(_ => rng.Next()));
                                }
                                else if (!string.IsNullOrWhiteSpace(correct))
                                {
                                    var minimal = new List<string> { correct, "خيار آخر 1", "خيار آخر 2", "خيار آخر 3" };
                                    optionsStr = string.Join('|', minimal.OrderBy(_ => rng.Next()));
                                    correctAnswer = correct;
                                }
                                else { rejected.Add((id, "MCQ missing correct_answer")); continue; }
                                break;
                            }
                            case "ORDERING":
                                correctAnswer = correct; break;
                            case "TIMED_NUMERIC":
                                if (!double.TryParse(correct, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _)) { rejected.Add((id, "TIMED_NUMERIC correct_answer must be numeric")); continue; }
                                correctAnswer = correct; break;
                            case "Text":
                                correctAnswer = null; break;
                        }

                        // Enforce MCQ to use CSV options column and validate correct_answer
                        if (type == "MCQ")
                        {
                            var optionList = SplitOptions(optionsRaw);
                            if (optionList.Count < 2)
                            {
                                rejected.Add((id, "MCQ requires at least two options"));
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(correct))
                            {
                                rejected.Add((id, "MCQ missing correct_answer"));
                                continue;
                            }
                            if (!optionList.Any(o => string.Equals(o, correct, StringComparison.Ordinal)))
                            {
                                rejected.Add((id, "MCQ correct_answer not in options"));
                                continue;
                            }
                            optionsStr = string.Join('|', optionList);
                            correctAnswer = correct;
                        }

                        // Format ItemCode as "I001", "I002" etc.
                        // Extract numeric portion from item_id or use current count + 1
                        var numericId = int.TryParse(id.Replace("I", "").Trim(), out var n) ? n : items.Count + 1;
                        var itemCode = $"I{numericId:D03}"; // Ensures 3 digits with leading zeros

                        items.Add(new Item
                        {
                            ItemCode = itemCode,
                            TextAr = textAr,
                            Type = type,
                            DimensionTags = dimensionTags,
                            Difficulty = difficulty,
                            TimeLimitSeconds = timeLimit,
                            MaxScore = maxScore,
                            CorrectAnswer = correctAnswer,
                            Options = optionsStr
                        });
                    }
                    } // End of else block for legacy CSV
                }

                _logger.Information("Total rows parsed (excluding header): {Count}", parsedRows);

                // SDJ V1 has 125 items, V2 has 210 items (105 MCQ + 105 Likert), legacy has 200
                var expectedCount = isLegacy ? 200 : (sdjMode == "1" ? 125 : 210);
                if (items.Count > expectedCount)
                {
                    var skipped = items.Count - expectedCount;
                    _logger.Warning("Parsed {Parsed} valid items; limiting to {Expected}. Skipping {Skipped} items from tail.", 
                        items.Count, expectedCount, skipped);
                    items = items.Take(expectedCount).ToList();
                }

                if (items.Count == 0)
                {
                    _logger.Warning("No valid rows were parsed from CSV file {CsvPath}.", csvPath);
                    return;
                }

                await _context.Items.AddRangeAsync(items);
                await _context.SaveChangesAsync();

                var total = await _context.Items.CountAsync();
                var byType = await _context.Items.GroupBy(i => i.Type).Select(g => new { Type = g.Key, Count = g.Count() }).ToListAsync();
                _logger.Information("Seeded Items count: {Count}", total);
                foreach (var t in byType) _logger.Information("Type {Type}: {Count}", t.Type, t.Count);

                if (total != expectedCount)
                {
                    _logger.Error("Post-seed verification failed. Expected {Expected} items, found {Count}", expectedCount, total);
                    foreach (var r in rejected.Take(50)) _logger.Warning("Rejected item {ItemId}: {Reason}", r.ItemId, r.Reason);
                }
                else
                {
                    _logger.Information("Post-seed verification passed: {Count} items.", expectedCount);
                    if (!isLegacy)
                    {
                        // Check if V2 mode (PatternId populated)
                        var v2Count = await _context.Items.CountAsync(i => i.PatternId != null);
                        var isV2 = v2Count > 0;
                        
                        if (isV2)
                        {
                            _logger.Information("[SDJ V2] Seven Patterns mode detected: {V2Count} items with PatternId", v2Count);
                            
                            // V2: Log pattern distribution
                            var byPattern = await _context.Items
                                .Where(i => i.PatternId != null)
                                .GroupBy(i => new { i.PatternId, i.PatternNameAr })
                                .Select(g => new { PatternId = g.Key.PatternId, Name = g.Key.PatternNameAr, Count = g.Count() })
                                .OrderBy(x => x.PatternId)
                                .ToListAsync();
                            
                            _logger.Information("[SDJ V2] Pattern distribution:");
                            foreach (var p in byPattern) 
                                _logger.Information("  {PatternId} ({Name}): {Count} items", p.PatternId, p.Name, p.Count);
                            
                            // V2: Log sub-dimension distribution
                            var bySubDim = await _context.Items
                                .Where(i => i.SubId != null)
                                .GroupBy(i => new { i.SubId, i.SubNameAr })
                                .Select(g => new { SubId = g.Key.SubId, Name = g.Key.SubNameAr, Count = g.Count() })
                                .OrderBy(x => x.SubId)
                                .ToListAsync();
                            
                            _logger.Information("[SDJ V2] Sub-dimension distribution ({Count} sub-dimensions):", bySubDim.Count);
                            foreach (var s in bySubDim) 
                                _logger.Information("  {SubId} ({Name}): {Count} items", s.SubId, s.Name, s.Count);
                        }
                        else
                        {
                            // V1: Log dimension distribution
                            _logger.Information("[SDJ V1] Legacy dimension mode");
                            var byDimension = await _context.Items
                                .Where(i => i.Dimension != null)
                                .GroupBy(i => i.Dimension)
                                .Select(g => new { Dimension = g.Key, Count = g.Count() })
                                .ToListAsync();
                            _logger.Information("[SDJ V1] Dimension distribution:");
                            foreach (var d in byDimension) _logger.Information("  {Dimension}: {Count} items", d.Dimension, d.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while seeding items from CSV file.");
                throw;
            }
        }

        public async Task SeedItemParametersAsync(bool forceReseed = false)
        {
            if (await _context.ItemParameters.AnyAsync() && !forceReseed)
            {
                _logger.Information("ItemParameters table already contains data. Skipping seeding.");
                return;
            }

            if (forceReseed && await _context.ItemParameters.AnyAsync())
            {
                _logger.Information("Force reseeding: Clearing existing ItemParameters data.");
                _context.ItemParameters.RemoveRange(await _context.ItemParameters.ToListAsync());
                await _context.SaveChangesAsync();
            }

            try
            {
                var itemsWithoutParameters = await _context.Items
                    .Where(i => !_context.ItemParameters.Any(ip => ip.ItemId == i.Id))
                    .ToListAsync();

                var itemParametersList = new List<ItemParameters>();

                foreach (var item in itemsWithoutParameters)
                {
                    var itemParameter = new ItemParameters
                    {
                        ItemId = item.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    switch ((item.Type ?? string.Empty).Trim())
                    {
                        case "MCQ":
                            itemParameter.ModelType = "3PL";
                            itemParameter.A = 1.0;
                            itemParameter.B = 3 - item.Difficulty;
                            itemParameter.C = 0.15;
                            break;

                        case "LikertAgreement":
                        case "Frequency":
                            itemParameter.ModelType = "GRM";
                            itemParameter.ThresholdsJson = "[-1.5,-0.5,0.5,1.5]";
                            break;

                        case "ORDERING":
                            itemParameter.ModelType = "PCM";
                            break;

                        case "TIMED_NUMERIC":
                            itemParameter.ModelType = "NUMERIC";
                            itemParameter.TimeAlpha = 1.0;
                            itemParameter.TimeBeta = 0.0;
                            break;

                        case "Text":
                            itemParameter.ModelType = "TEXT";
                            break;

                        default:
                            _logger.Warning("Unknown item type: {Type} for item ID: {ItemId}", item.Type, item.Id);
                            continue;
                    }

                    itemParametersList.Add(itemParameter);
                }

                if (itemParametersList.Count == 0)
                {
                    _logger.Warning("No item parameters were generated because supported item types were not found.");
                    return;
                }

                await _context.ItemParameters.AddRangeAsync(itemParametersList);
                await _context.SaveChangesAsync();

                _logger.Information("Successfully seeded {Count} item parameters into the database.", itemParametersList.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while seeding item parameters.");
                throw;
            }
        }

        public async Task SeedMockUsersAsync(bool forceReseed = false)
        {
            if (await _context.Users.AnyAsync() && !forceReseed)
            {
                _logger.Information("Users table already contains data. Skipping mock user seeding.");
                return;
            }

            if (forceReseed && await _context.Users.AnyAsync())
            {
                _logger.Information("Force reseeding: Clearing existing Users data.");
                _context.Users.RemoveRange(await _context.Users.ToListAsync());
                await _context.SaveChangesAsync();
            }

            try
            {
                var mockUsers = Enumerable.Range(1, 10)
                    .Select(i => new User { 
                        NationalId = $"100000000{i}", 
                        FullName = $"مستخدم تجريبي {i}", 
                        CreatedAt = DateTime.UtcNow 
                    })
                    .ToList();
                await _context.Users.AddRangeAsync(mockUsers);
                await _context.SaveChangesAsync();

                _logger.Information("Successfully seeded {Count} mock users into the database.", mockUsers.Count);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "An error occurred while seeding mock users.");
                throw;
            }
        }

        private static string NormalizeType(string typeRaw)
        {
            var t = (typeRaw ?? string.Empty).Trim();
            if (string.Equals(t, "MCQ", StringComparison.OrdinalIgnoreCase)) return "MCQ";
            if (string.Equals(t, "TEXT", StringComparison.OrdinalIgnoreCase)) return "Text";
            if (string.Equals(t, "ORDERING", StringComparison.OrdinalIgnoreCase)) return "ORDERING";
            if (string.Equals(t, "TIMED_NUMERIC", StringComparison.OrdinalIgnoreCase)) return "TIMED_NUMERIC";
            if (string.Equals(t, "LIKERT", StringComparison.OrdinalIgnoreCase) || string.Equals(t, "LIKERTAGREEMENT", StringComparison.OrdinalIgnoreCase)) return "LikertAgreement";
            if (string.Equals(t, "FREQUENCY", StringComparison.OrdinalIgnoreCase)) return "Frequency";
            return t;
        }

        private static List<string> SplitOptions(string? raw)
        {
            var res = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return res;
            foreach (var sep in new[] { "|", ";", "," })
            {
                if (raw.Contains(sep, StringComparison.Ordinal))
                {
                    res = raw.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    break;
                }
            }
            if (res.Count == 0 && !string.IsNullOrWhiteSpace(raw)) res.Add(raw);
            return res;
        }

        private sealed class CsvQuestionRow
        {
            public string? item_id { get; set; }
            public string? text_ar { get; set; }
            public string? type { get; set; }
            public string? dimension_tags { get; set; }
            public string? difficulty { get; set; }
            public string? time_limit_seconds { get; set; }
            public string? max_score { get; set; }
            public string? correct_answer { get; set; }
            public string? options { get; set; }
        }

        private sealed class CsvSdjRow
        {
            // V2 Headers (PascalCase) - used in questions_sdj_v2_ar.csv
            public string? ItemCode { get; set; }
            public string? TextAr { get; set; }
            public string? Type { get; set; }
            public string? Reverse { get; set; }
            public string? TimeLimitSeconds { get; set; }
            public string? Weight { get; set; }  // V2 replaces max_score
            
            // V1 Headers (snake_case) - used in questions_sdj_ar.csv for backward compatibility
            public string? item_code { get; set; }
            public string? text_ar { get; set; }
            public string? type { get; set; }
            public string? anchors_ar { get; set; }
            public string? reverse { get; set; }
            public string? time_limit_seconds { get; set; }
            public string? max_score { get; set; }
            public string? difficulty { get; set; }
            public string? dimension { get; set; }
            public string? sub_dimension { get; set; }
            
            // V2 Seven Patterns Fields
            public string? PatternId { get; set; }
            public string? PatternKey { get; set; }
            public string? PatternNameAr { get; set; }
            public string? SubId { get; set; }
            public string? SubKey { get; set; }
            public string? SubNameAr { get; set; }
            
            // MCQ Fields (V2)
            public string? CorrectAnswer { get; set; }
            public string? Options { get; set; }
        }
    }
}

