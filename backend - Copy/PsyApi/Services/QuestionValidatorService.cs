using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PsyApi.Data;
using PsyApi.Models;

namespace PsyApi.Services
{
    public class QuestionValidatorService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<QuestionValidatorService> _logger;

        // Canonical Arabic options
        private static readonly string[] FrequencyOptions = new[] { "أبدًا", "نادرًا", "أحيانًا", "غالبًا", "دائمًا" };
        private static readonly string[] LikertAgreementOptions = new[] { "لا أوافق بشدة", "لا أوافق", "محايد", "أوافق", "أوافق بشدة" };

        private const string FrequencyType = "Frequency";
        private const string LikertAgreementType = "LikertAgreement";
        private const string TextType = "Text";

        public QuestionValidatorService(AppDbContext db, ILogger<QuestionValidatorService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // For v1, avoid reclassification by text; trust stored type
        public string ClassifyQuestion(string questionText) => string.Empty;

        public string[] GetOptionsForQuestionType(string questionType)
        {
            return questionType switch
            {
                FrequencyType => FrequencyOptions,
                LikertAgreementType => LikertAgreementOptions,
                _ => Array.Empty<string>()
            };
        }

        public async Task<QuestionValidationResult> ValidateAllQuestionsAsync()
        {
            var items = await _db.Items.AsNoTracking().ToListAsync();
            var invalidQuestions = new List<InvalidQuestion>();
            int validCount = 0;

            foreach (var item in items)
            {
                var expectedType = (item.Type ?? string.Empty).Trim();
                var expectedOptions = GetOptionsForQuestionType(expectedType);

                var issues = new List<string>();

                // No type mismatch checks in v1

                if (expectedType == FrequencyType || expectedType == LikertAgreementType)
                {
                    var currentOptions = ExtractOptions(item.Options);
                    if (currentOptions.Count == 0 || !currentOptions.SequenceEqual(expectedOptions))
                    {
                        issues.Add($"Options not set to canonical for '{expectedType}'");
                    }
                    if (!string.IsNullOrWhiteSpace(item.CorrectAnswer))
                    {
                        issues.Add("CorrectAnswer must be null for option-only types");
                    }
                }
                else if (expectedType == TextType)
                {
                    if (!string.IsNullOrWhiteSpace(item.CorrectAnswer) || !string.IsNullOrWhiteSpace(item.Options))
                    {
                        issues.Add("Text item must not have CorrectAnswer/Options");
                    }
                }

                if (issues.Count > 0)
                {
                    invalidQuestions.Add(new InvalidQuestion
                    {
                        ItemId = item.Id,
                        TextAr = item.TextAr ?? string.Empty,
                        CurrentType = item.Type ?? string.Empty,
                        ExpectedType = expectedType,
                        CurrentOptions = ExtractOptions(item.Options),
                        ExpectedOptions = expectedOptions.ToList(),
                        Issues = issues
                    });
                }
                else validCount++;
            }

            return new QuestionValidationResult
            {
                TotalQuestions = items.Count,
                ValidQuestions = validCount,
                InvalidQuestions = invalidQuestions
            };
        }

        public async Task<QuestionFixResult> FixAllQuestionsAsync()
        {
            var items = await _db.Items.ToListAsync();
            var fixedQuestions = new List<FixedQuestion>();
            int fixedCount = 0;

            foreach (var item in items)
            {
                var expectedType = (item.Type ?? string.Empty).Trim();
                var expectedOptions = GetOptionsForQuestionType(expectedType);

                var originalType = item.Type ?? string.Empty;
                var originalOptions = ExtractOptions(item.Options);
                var wasFixed = false;

                // keep stored type as-is

                if (expectedType == FrequencyType || expectedType == LikertAgreementType)
                {
                    var optionsString = string.Join("|", expectedOptions);
                    if (item.Options != optionsString || !string.IsNullOrWhiteSpace(item.CorrectAnswer))
                    {
                        item.Options = optionsString;
                        item.CorrectAnswer = null;
                        wasFixed = true;
                    }
                }
                else if (expectedType == TextType)
                {
                    if (!string.IsNullOrWhiteSpace(item.CorrectAnswer) || !string.IsNullOrWhiteSpace(item.Options))
                    {
                        item.CorrectAnswer = null;
                        item.Options = null;
                        wasFixed = true;
                    }
                }

                if (wasFixed)
                {
                    fixedCount++;
                    fixedQuestions.Add(new FixedQuestion
                    {
                        ItemId = item.Id,
                        TextAr = item.TextAr ?? string.Empty,
                        OriginalType = originalType,
                        NewType = item.Type ?? string.Empty,
                        OriginalOptions = originalOptions,
                        NewOptions = expectedOptions.ToList()
                    });
                    _logger.LogInformation("Fixed question {ItemId}: {Text}", item.Id, item.TextAr);
                }
            }

            await _db.SaveChangesAsync();

            return new QuestionFixResult
            {
                TotalQuestions = items.Count,
                FixedQuestions = fixedCount,
                FixedQuestionDetails = fixedQuestions
            };
        }

        private List<string> ExtractOptions(string? optionsValue)
        {
            var options = new List<string>();
            if (string.IsNullOrWhiteSpace(optionsValue)) return options;
            var seps = new[] { "|", ";", "?", "," };
            foreach (var sep in seps)
            {
                if (optionsValue.Contains(sep, StringComparison.Ordinal))
                {
                    options = optionsValue.Split(sep, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    break;
                }
            }
            if (options.Count == 0 && !string.IsNullOrWhiteSpace(optionsValue)) options.Add(optionsValue);
            return options;
        }
    }

    public class QuestionValidationResult
    {
        public int TotalQuestions { get; set; }
        public int ValidQuestions { get; set; }
        public List<InvalidQuestion> InvalidQuestions { get; set; } = new();
    }

    public class InvalidQuestion
    {
        public int ItemId { get; set; }
        public string TextAr { get; set; } = string.Empty;
        public string CurrentType { get; set; } = string.Empty;
        public string ExpectedType { get; set; } = string.Empty;
        public List<string> CurrentOptions { get; set; } = new();
        public List<string> ExpectedOptions { get; set; } = new();
        public List<string> Issues { get; set; } = new();
    }

    public class QuestionFixResult
    {
        public int TotalQuestions { get; set; }
        public int FixedQuestions { get; set; }
        public List<FixedQuestion> FixedQuestionDetails { get; set; } = new();
    }

    public class FixedQuestion
    {
        public int ItemId { get; set; }
        public string TextAr { get; set; } = string.Empty;
        public string OriginalType { get; set; } = string.Empty;
        public string NewType { get; set; } = string.Empty;
        public List<string> OriginalOptions { get; set; } = new();
        public List<string> NewOptions { get; set; } = new();
    }
}
