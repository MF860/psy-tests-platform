using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PsyApi.Data;
using PsyApi.Models;

namespace PsyApi.Services.Scoring
{

public class ScoringService : IScoringService
    {
        private readonly AppDbContext _db;
        private readonly Serilog.ILogger _log;
        private static readonly object _weightsLock = new();
        private static Dictionary<string, double>? _dimWeights; // loaded from env JSON: { "dim": weight }

        public ScoringService(AppDbContext db, Serilog.ILogger logger)
        {
            _db = db;
            _log = logger;
        }

        public async Task<EnhancedScoreSummary> ComputeSessionScores(int sessionId)
        {
            var session = await _db.Sessions
                .Include(s => s.SessionItems)
                    .ThenInclude(si => si.Item)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session == null)
            {
                throw new InvalidOperationException($"Session {sessionId} not found");
            }

            var summary = new EnhancedScoreSummary();

            var itemIds = session.SessionItems.Select(si => si.ItemId).Distinct().ToList();
            var paramByItem = await _db.ItemParameters
                .Where(p => itemIds.Contains(p.ItemId))
                .ToDictionaryAsync(p => p.ItemId, p => p);
            
            // First validate response patterns
            var validity = new ResponseValidityScore();
            summary.Validity = validity;
            
            // Score each item
            foreach (var si in session.SessionItems)
            {
                var item = si.Item;
                paramByItem.TryGetValue(item.Id, out var param);

                try
                {
                    var type = (item.Type ?? string.Empty).Trim().ToUpperInvariant();
                    switch (type)
                    {
                        case "MCQ":
                        {
                            var (rawCorrect, score) = ScoreMcq(item, si.Answer, param);
                            si.RawCorrect = rawCorrect;
                            si.Score = score;
                            break;
                        }
                        case "TIMED_NUMERIC":
                        {
                            var score = ScoreTimedNumeric(item, si.Answer, si.ResponseTimeMs, param);
                            si.RawCorrect = score >= (int)Math.Round(item.MaxScore * 0.5); // heuristic
                            si.Score = score;
                            break;
                        }
                        case "ORDERING":
                        {
                            var answerOrder = SplitOrder(si.Answer);
                            var correctOrder = SplitOrder(item.CorrectAnswer);
                            var score = ScoreOrdering(item, answerOrder, correctOrder, param);
                            si.RawCorrect = null;
                            si.Score = score;
                            break;
                        }
                        case "LIKERT":
                        case "LIKERTAGREEMENT":
                        case "FREQUENCY":
                        {
                            if (!int.TryParse((si.Answer ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                            {
                                v = 3; // neutral
                            }
                            var score = ScoreLikert(item, v, param);
                            si.RawCorrect = null;
                            si.Score = score;
                            break;
                        }
                        case "TEXT":
                        {
                            var score = ScoreText(item, si.Answer ?? string.Empty);
                            si.RawCorrect = null;
                            si.Score = score;
                            break;
                        }
                        default:
                            _log.Warning("Unknown item type {Type} for item {ItemId}", item.Type, item.Id);
                            si.Score = 0;
                            si.RawCorrect = null;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Scoring error for sessionItem {SessionItemId}, item {ItemId}", si.Id, si.ItemId);
                    si.Score = 0;
                    si.RawCorrect = null;
                }
            }

            await _db.SaveChangesAsync();

            // Add cognitive load analysis
            var cogLoad = Scoring.CognitiveLoadAnalyzer.AnalyzeCognitiveLoad(session.SessionItems.ToList());
            summary.CognitiveLoadMetrics = cogLoad.DetailedMetrics;
            summary.CognitiveLoadObservations = cogLoad.Observations;

            // Update scores by dimension
            summary = await AggregateByDimension(session.SessionItems.ToList());
            
            // Update version and total score
            var totalScore = session.SessionItems.Where(x => x.Score.HasValue).Sum(x => (double)x.Score!.Value);
            summary.TotalScore = totalScore;
            summary.Version = "v1.1";

            // Persist result
            var existingResult = await _db.Results.FirstOrDefaultAsync(r => r.SessionId == sessionId);
            var summaryJson = JsonSerializer.Serialize(summary);
            if (existingResult == null)
            {
                existingResult = new Result
                {
                    SessionId = sessionId,
                    TotalScore = (int)Math.Round(totalScore),
                    DimensionScoresJson = summaryJson,
                    ScoringModelVersion = summary.Version
                };
                _db.Results.Add(existingResult);
            }
            else
            {
                existingResult.TotalScore = (int)Math.Round(totalScore);
                existingResult.DimensionScoresJson = summaryJson;
                existingResult.ScoringModelVersion = summary.Version;
            }
            await _db.SaveChangesAsync();

            return summary;
        }

        // Score MCQ with Arabic-friendly fuzzy matching or exact numeric compare
        public (bool rawCorrect, int score) ScoreMcq(Item item, string? answer, ItemParameters? param)
        {
            if (string.IsNullOrWhiteSpace(item.CorrectAnswer))
                return (false, 0);

            var corr = item.CorrectAnswer!.Trim();
            var ans = (answer ?? string.Empty).Trim();

            // Numeric exact match if applicable
            if (TryParseDouble(corr, out var corrNum) && TryParseDouble(ans, out var ansNum))
            {
                var equal = Math.Abs(ansNum - corrNum) < 1e-9;
                return (equal, equal ? item.MaxScore : 0);
            }

            // Arabic tolerant fuzzy match for strings
            var nCorr = NormalizeArabic(corr);
            var nAns = NormalizeArabic(ans);
            var sim = SimilarityRatio(nCorr, nAns);
            var match = sim >= 0.9 || string.Equals(nCorr, nAns, StringComparison.Ordinal);
            return (match, match ? item.MaxScore : 0);
        }

        // Score timed numeric with tolerance and time decay
        public int ScoreTimedNumeric(Item item, string? answer, int? responseTimeMs, ItemParameters? param)
        {
            if (string.IsNullOrWhiteSpace(item.CorrectAnswer)) return 0;
            if (!TryParseDouble(item.CorrectAnswer!, out var corr)) return 0;
            if (!TryParseDouble(answer ?? string.Empty, out var ans)) return 0;

            var diffAbs = Math.Abs(ans - corr);
            double tolerancePct = item.Difficulty switch
            {
                1 => 0.0,
                2 => 0.0,
                3 => 0.02,
                4 => 0.05,
                5 => 0.05,
                _ => 0.0
            };

            bool withinTol;
            if (Math.Abs(corr) < 1e-9)
            {
                withinTol = diffAbs < 1e-9; // exact when correct value is 0
            }
            else
            {
                var diffPct = Math.Abs(diffAbs / corr);
                withinTol = diffPct <= tolerancePct;
            }

            var baseScore = withinTol ? item.MaxScore : 0;

            var t = responseTimeMs ?? (item.TimeLimitSeconds * 1000);
            var limit = Math.Max(1, item.TimeLimitSeconds * 1000);
            var delayRatio = Math.Max(0.0, (t - limit) / (double)limit);
            var lambda = param?.TimeAlpha ?? 1.0;
            var w_t = Math.Exp(-lambda * delayRatio);

            var final = baseScore * w_t;
            final = Math.Clamp(final, 0.0, item.MaxScore);
            return (int)Math.Round(final);
        }

        // Score ordering via normalized Kendall tau accuracy
        public int ScoreOrdering(Item item, IList<string> answerOrder, IList<string> correctOrder, ItemParameters? param)
        {
            if (correctOrder.Count == 0 || answerOrder.Count == 0)
                return 0;

            // consider only items present in both
            var set = new HashSet<string>(correctOrder);
            var filteredAns = answerOrder.Where(set.Contains).ToList();

            var pos = new Dictionary<string, int>();
            for (int i = 0; i < correctOrder.Count; i++) pos[correctOrder[i]] = i;

            int discordant = 0;
            int totalPairs = 0;
            for (int i = 0; i < filteredAns.Count; i++)
            {
                for (int j = i + 1; j < filteredAns.Count; j++)
                {
                    var a = filteredAns[i];
                    var b = filteredAns[j];
                    if (!pos.ContainsKey(a) || !pos.ContainsKey(b)) continue;
                    totalPairs++;
                    var concordant = (pos[a] < pos[b] && i < j) || (pos[a] > pos[b] && i > j);
                    if (!concordant) discordant++;
                }
            }

            if (totalPairs == 0) return 0;

            var K_n = discordant / (double)totalPairs;
            var accuracy = 1.0 - K_n; // in [0,1]
            var final = accuracy * item.MaxScore;
            return (int)Math.Round(Math.Clamp(final, 0.0, item.MaxScore));
        }

        // Score Likert; scaffold GRM helpers for future
        public int ScoreLikert(Item item, int value, ItemParameters? param)
        {
            var v = Math.Max(1, Math.Min(5, value));
            var score = (v - 1) / 4.0 * item.MaxScore;
            // GRM scaffolding (not used in v1): compute category probs if thresholds available
            if (!string.IsNullOrWhiteSpace(param?.ThresholdsJson))
            {
                try
                {
                    var thresholds = JsonSerializer.Deserialize<double[]>(param!.ThresholdsJson!);
                    if (thresholds != null)
                    {
                        _ = GrmComputeCategoryProbabilities(0.0, thresholds); // placeholder at theta=0
                    }
                }
                catch { /* ignore malformed thresholds */ }
            }
            return (int)Math.Round(score);
        }

        // Score Text using simple keyword rubric + length + coherence
        public int ScoreText(Item item, string text)
        {
            var dims = SplitDimensions(item.DimensionTags);

            var tokens = TokenizeArabic(text);
            var unique = tokens.ToHashSet();

            // Derive simple keywords from dimension tags themselves (fallback rubric)
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in dims)
            {
                foreach (var k in TokenizeArabic(d))
                {
                    if (!string.IsNullOrWhiteSpace(k)) keywords.Add(k);
                }
            }

            int matched = keywords.Count == 0 ? 0 : keywords.Count(k => unique.Contains(k));
            int possible = Math.Max(1, keywords.Count);
            double coverage = matched / (double)possible; // [0,1]

            // length bonus
            double lengthBonus = Math.Min(0.2, tokens.Count / 200.0);

            // coherence heuristic: ratio of unique tokens and average sentence length
            double uniqueness = tokens.Count > 0 ? unique.Count / (double)tokens.Count : 0.0; // [0,1]
            var sentences = Regex.Split(text, "[.!؟!\n]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            double avgSentence = sentences.Count > 0 ? sentences.Average(s => TokenizeArabic(s).Count) : tokens.Count;
            double coherence = 0.5 * uniqueness + 0.5 * Math.Min(1.0, avgSentence / 12.0);

            double combined = 0.6 * coverage + 0.2 * lengthBonus + 0.2 * coherence;
            combined = Math.Clamp(combined, 0.0, 1.0);
            var final = combined * item.MaxScore;
            return (int)Math.Round(final);
        }

        // Aggregate by dimension; compute Z, T, percentile
        private async Task<EnhancedScoreSummary> AggregateByDimension(IList<SessionItem> sessionItems)
        {
            var dimScoresRaw = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            var dimItemCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dimMaxRaw = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            foreach (var si in sessionItems)
            {
                var score = si.Score ?? 0;
                var dims = SplitDimensions(si.Item.DimensionTags);
                foreach (var d in dims)
                {
                    var w = GetDimensionWeight(d);
                    if (!dimScoresRaw.ContainsKey(d)) dimScoresRaw[d] = 0.0;
                    dimScoresRaw[d] += score * w;
                    if (!dimItemCount.ContainsKey(d)) dimItemCount[d] = 0;
                    dimItemCount[d] += 1;
                    if (!dimMaxRaw.ContainsKey(d)) dimMaxRaw[d] = 0.0;
                    dimMaxRaw[d] += Math.Max(0, si.Item.MaxScore) * w;
                }
            }

            // Build empirical distributions from previous results if available
            var priorJson = await _db.Results.AsNoTracking()
                .Where(r => r.DimensionScoresJson != null)
                .Select(r => r.DimensionScoresJson!)
                .ToListAsync();

            var empirical = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
            foreach (var json in priorJson)
            {
                try
                {
                    var ss = JsonSerializer.Deserialize<EnhancedScoreSummary>(json);
                    if (ss?.DimensionScores == null) continue;
                    foreach (var ds in ss.DimensionScores)
                    {
                        if (!empirical.TryGetValue(ds.Dimension, out var list))
                        {
                            list = new List<double>();
                            empirical[ds.Dimension] = list;
                        }
                        list.Add(ds.Raw);
                    }
                }
                catch { }
            }

            var summary = new EnhancedScoreSummary();
            foreach (var (dim, raw) in dimScoresRaw)
            {
                var values = empirical.TryGetValue(dim, out var list) ? list : null;
                double mean = 0.0, sd = 1.0;
                if (values != null && values.Count >= 5)
                {
                    mean = values.Average();
                    var variance = values.Select(v => (v - mean) * (v - mean)).Average();
                    sd = Math.Sqrt(Math.Max(variance, 1e-9));
                }
                else
                {
                    // Data-sparse fallback: adjust for knowledge-heavy dimensions
                    var maxRaw = dimMaxRaw.TryGetValue(dim, out var mr) ? mr : Math.Max(10.0, Math.Abs(raw));
                    var weight = GetDimensionWeight(dim);
                    
                    // For knowledge dimensions (weight >= 1.5), assume lower baseline performance
                    if (weight >= 1.5)
                    {
                        mean = 0.3 * maxRaw; // lower mean for knowledge tests
                        sd = 0.3 * maxRaw;   // wider spread
                    }
                    else
                    {
                        mean = 0.5 * maxRaw; // standard mean for subjective measures
                        sd = 0.2 * maxRaw;   // narrower spread
                    }
                    sd = Math.Max(sd, 1.0);
                }
                var z = (raw - mean) / sd;
                // Reduced shrinkage for better T-score distribution
                var n = dimItemCount.TryGetValue(dim, out var cnt) ? cnt : 0;
                var k = 2.0; // reduced stabilizer for less aggressive shrinkage
                var shrink = n > 0 ? (n / (n + k)) : 0.0;
                
                // Apply minimum shrinkage to preserve high scores
                shrink = Math.Max(shrink, 0.7); // minimum 70% of original z-score
                
                var zPrime = z * shrink;
                var t = 50.0 + 10.0 * zPrime;

                double percentile;
                if (values != null && values.Count > 0)
                {
                    var less = values.Count(v => v < raw);
                    var equal = values.Count(v => Math.Abs(v - raw) < 1e-9);
                    percentile = ((less + 0.5 * equal) / values.Count) * 100.0;
                }
                else
                {
                    // Data-sparse: approximate percentile via Normal CDF of shrunken Z
                    percentile = 100.0 * NormalCdf(zPrime);
                }

                var ds = new DimensionScore
                {
                    Dimension = dim,
                    Raw = Math.Round(raw, 3),
                    Z = Math.Round(zPrime, 3),
                    T = Math.Round(Math.Clamp(t, 20.0, 80.0), 1),
                    Percentile = Math.Round(percentile, 1)
                };
                summary.DimensionScores.Add(ds);
            }

            return summary;
        }

        // Standard normal CDF approximation (Hart, 1968-inspired)
        private static double NormalCdf(double x)
        {
            // Abramowitz-Stegun approximation for Phi(x)
            var sign = x < 0 ? -1.0 : 1.0;
            x = Math.Abs(x) / Math.Sqrt(2.0);
            double t = 1.0 / (1.0 + 0.3275911 * x);
            // Coefficients
            double a1 = 0.254829592, a2 = -0.284496736, a3 = 1.421413741, a4 = -1.453152027, a5 = 1.061405429;
            double erf = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);
            double phi = 0.5 * (1.0 + sign * erf);
            return Math.Clamp(phi, 0.0, 1.0);
        }

        private static double GetDimensionWeight(string dim)
        {
            try
            {
                if (_dimWeights != null && _dimWeights.Count > 0)
                {
                    if (_dimWeights.TryGetValue(dim, out var w)) return w;
                }
                lock (_weightsLock)
                {
                    if (_dimWeights == null)
                    {
                        var json = Environment.GetEnvironmentVariable("SCORING_DIM_WEIGHTS");
                        if (!string.IsNullOrWhiteSpace(json))
                        {
                            try
                            {
                                _dimWeights = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(json) ?? new();
                            }
                            catch { _dimWeights = new(); }
                        }
                        else _dimWeights = new();
                    }
                }
            }
            catch { }
            return 1.0; // default
        }

        // Helpers
        private static bool TryParseDouble(string s, out double value)
        {
            return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)
                || double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, new CultureInfo("ar-EG"), out value);
        }

        private static string NormalizeArabic(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input;
            // unify alef forms, ya, ta marbuta, hamza on waw/ya
            s = s.Replace('\u0622', '\u0627') // ALEF WITH MADDA -> ALEF
                 .Replace('\u0623', '\u0627') // ALEF WITH HAMZA ABOVE -> ALEF
                 .Replace('\u0625', '\u0627') // ALEF WITH HAMZA BELOW -> ALEF
                 .Replace('\u0671', '\u0627') // ALEF WASLA -> ALEF
                 .Replace('\u0649', '\u064A') // ALEF MAKSURA -> YA
                 .Replace('\u06CC', '\u064A') // FARSI YA -> YA
                 .Replace('\u0629', '\u0647'); // TA MARBUTA -> HEH

            // remove tatweel
            s = s.Replace("\u0640", string.Empty);

            // remove diacritics
            var sb = new StringBuilder();
            foreach (var ch in s)
            {
                var code = (int)ch;
                if ((code >= 0x064B && code <= 0x065F) || code == 0x0670)
                    continue; // skip harakat
                sb.Append(ch);
            }

            s = sb.ToString();
            s = s.ToLowerInvariant();
            // remove punctuation/symbols, keep letters/digits/spaces, then whitespace reduce
            s = new string(s.Select(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch) ? ch : ' ').ToArray());
            var sbws = new StringBuilder();
            bool inSpace = false;
            foreach (var ch in s)
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!inSpace) { sbws.Append(' '); inSpace = true; }
                }
                else
                {
                    sbws.Append(ch);
                    inSpace = false;
                }
            }
            s = sbws.ToString().Trim();
            return s;
        }

        private static double SimilarityRatio(string a, string b)
        {
            if (a.Length == 0 && b.Length == 0) return 1.0;
            if (a.Length == 0 || b.Length == 0) return 0.0;
            var dist = Levenshtein(a, b);
            var maxLen = Math.Max(a.Length, b.Length);
            return 1.0 - dist / (double)maxLen;
        }

        private static int Levenshtein(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        private static IList<string> SplitOrder(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .Select(NormalizeArabic)
                      .ToList();
        }

        private static IList<string> SplitDimensions(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
            return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        // Intentionally omit hardcoded Arabic rubric to avoid encoding issues in code.

        private static List<string> TokenizeArabic(string text)
        {
            var norm = NormalizeArabic(text);
            if (string.IsNullOrWhiteSpace(norm)) return new List<string>();
            return norm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        // GRM helpers (scaffolding)
        private static double[] GrmComputeCategoryProbabilities(double theta, double[] thresholds)
        {
            // thresholds tau_1..tau_{m-1}; m categories
            int m = thresholds.Length + 1;
            var Pstar = new double[m + 1]; // P*_k for k=0..m
            Pstar[0] = 1.0; // P*_0 = 1 by definition
            Pstar[m] = 0.0; // P*_m = 0 by definition

            for (int k = 1; k <= m - 1; k++)
            {
                var val = 1.0 / (1.0 + Math.Exp(-(theta - thresholds[k - 1])));
                Pstar[k] = val;
            }

            var P = new double[m];
            for (int k = 0; k < m; k++)
            {
                P[k] = Pstar[k] - Pstar[k + 1];
            }
            return P;
        }
    }
}
