using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using PsyApi.Data;
using PsyApi.Models;
using PsyApi.Services.Scoring;
using PsyApi.Utils;
namespace PsyApi.Controllers
{
    public class QuestionOption
    {
        public QuestionOption(string value, string label)
        {
            Value = value;
            Label = label;
        }

        [JsonPropertyName("value")]
        public string Value { get; }

        [JsonPropertyName("label")]
        public string Label { get; }
    }

    public class SessionPayload
    {
        public DateTime StartedAtUtc { get; set; }
        public List<SessionAnswer> Answers { get; set; } = new();
        public SdjDataPayload? SdjData { get; set; }
    }

    public class SessionAnswer
    {
        public string ItemId { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class SdjDataPayload
    {
        public List<SdjDimensionPayload> Dimensions { get; set; } = new();
        public List<SdjSubDimensionPayload> SubDimensions { get; set; } = new();
        public List<SdjTrackFitPayload> TrackFits { get; set; } = new();
        public List<SevenPatternPayload>? SevenPatternScores { get; set; } = new(); // NEW: For 7-pattern report
        public string? Version { get; set; } // NEW: Scoring model version
    }

    public class SdjDimensionPayload
    {
        public string Dimension { get; set; } = string.Empty;
        public double Raw { get; set; }
        public double T { get; set; }
        public double Percentile { get; set; }
        public string Band { get; set; } = string.Empty;
    }

    public class SdjSubDimensionPayload
    {
        public string Dimension { get; set; } = string.Empty;
        public string SubDimension { get; set; } = string.Empty;
        public double T { get; set; }
        public string Band { get; set; } = string.Empty;
    }

    public class SdjTrackFitPayload
    {
        public string TrackNameAr { get; set; } = string.Empty;
        public string FitLevel { get; set; } = string.Empty;
        public double FitScore { get; set; }
    }

    public class SevenPatternPayload
    {
        public string PatternNameAr { get; set; } = string.Empty;
        public string PatternNameEn { get; set; } = string.Empty;
        public double TScore { get; set; }
        public string Band { get; set; } = string.Empty;
        public List<string> SubDimensions { get; set; } = new();
    }

    public class Models
    {
        public class StartSessionRequest
        {
            [Required]
            public string NationalId { get; set; } = string.Empty;
            public string? ClientId { get; set; }
            public string? UserAgent { get; set; }
        }

        public class SubmitAnswerRequest
        {
            [Required]
            public string ItemId { get; set; } = string.Empty;
            public string? Answer { get; set; }
            [System.Text.Json.Serialization.JsonConverter(typeof(PsyApi.Controllers.Json.LenientNullableDoubleConverter))]
            public double? NumericAnswer { get; set; }
            public int? ResponseTimeMs { get; set; }
        }

        public class SubmitOrderingLabelRequest
        {
            [JsonPropertyName("orderingLabel")]
            public string? Label { get; set; }
        }

        public class QuestionResponse
        {
            [JsonPropertyName("id")]
            public int Id { get; init; }

            [JsonPropertyName("item_id")]
            public string ItemId { get; init; } = string.Empty;

            [JsonPropertyName("text_ar")]
            public string TextAr { get; init; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; init; } = string.Empty;

            [JsonPropertyName("dimension_tags")]
            public string DimensionTags { get; init; } = string.Empty;

            [JsonPropertyName("difficulty")]
            public int Difficulty { get; init; }

            [JsonPropertyName("time_limit_seconds")]
            public int TimeLimitSeconds { get; init; }

            [JsonPropertyName("max_score")]
            public int MaxScore { get; init; }

            [JsonPropertyName("options")]
            public string? Options { get; init; }

            [JsonPropertyName("orderingChoices")]
            public string[]? OrderingChoices { get; init; }

            [JsonPropertyName("orderingLabels")]
            public string[]? OrderingLabels { get; init; }
        }

        public class SelectedOrderingLabel
        {
            [JsonPropertyName("orderingLabel")]
            public string Label { get; set; } = string.Empty;
        }


    }

    [ApiController]
    [Route("api/sessions")]
    public class SessionsController : ControllerBase
    {
        private static class TextUtils
        {
            public static string NormalizeArabicText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Normalize whitespace
            input = input.Trim();
            input = System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ");

            // Normalize Arabic characters
            var normalizedChars = input.Select(c => c switch
            {
                'أ' or 'إ' or 'آ' => 'ا',
                'ة' => 'ه',
                'ى' => 'ي',
                _ => c
            });

            return new string(normalizedChars.ToArray());
        }
    }

    // SDJ v2.0: Use numeric values (1-5) for scoring compatibility
    private static readonly IReadOnlyList<QuestionOption> LikertOptions = new List<QuestionOption>
    {
        new QuestionOption("1", "لا أوافق بشدة"),
        new QuestionOption("2", "لا أوافق"),
        new QuestionOption("3", "محايد"),
        new QuestionOption("4", "أوافق"),
        new QuestionOption("5", "أوافق بشدة")
    };

    private static readonly IReadOnlyList<QuestionOption> FrequencyOptions = new List<QuestionOption>
    {
        new QuestionOption("1", "أبدًا"),
        new QuestionOption("2", "نادرًا"),
        new QuestionOption("3", "أحيانًا"),
        new QuestionOption("4", "غالبًا"),
        new QuestionOption("5", "دائمًا")
    };        private readonly AppDbContext _context;
        private readonly ILogger<SessionsController> _logger;
        private readonly IScoringService _scoringService;
        private readonly ISdjScoringService? _sdjScoringService;
        private static readonly Dictionary<string, int> _rateLimiter = new();
        private static readonly object _rateLimiterLock = new();

        public SessionsController(
            AppDbContext context, 
            ILogger<SessionsController> logger, 
            IScoringService scoringService,
            ISdjScoringService? sdjScoringService = null)
        {
            _context = context;
            _logger = logger;
            _scoringService = scoringService;
            _sdjScoringService = sdjScoringService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartSession([FromBody] Models.StartSessionRequest request)
        {
            try
            {
                // Validate National ID format
                if (string.IsNullOrWhiteSpace(request.NationalId) || !System.Text.RegularExpressions.Regex.IsMatch(request.NationalId, @"^\d{10}$"))
                {
                    return BadRequest(new { code = "INVALID_NATIONAL_ID", message = "National ID must be exactly 10 digits" });
                }

                // Rate limiting check
                var clientKey = $"{request.ClientId ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
                lock (_rateLimiterLock)
                {
                    if (_rateLimiter.TryGetValue(clientKey, out var attempts) && attempts >= 10)
                    {
                        return StatusCode(429, new { code = "RATE_LIMIT_EXCEEDED", message = "Too many attempts" });
                    }
                }

                // Find active user by National ID
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.NationalId == request.NationalId);

                if (user == null)
                {
                    // Update rate limit counter
                    lock (_rateLimiterLock)
                    {
                        _rateLimiter[clientKey] = (_rateLimiter.TryGetValue(clientKey, out var count) ? count : 0) + 1;
                    }
                    
                    _logger.LogWarning("Login attempt with unauthorized National ID: ******{LastFourDigits}", 
                        request.NationalId.Length >= 4 ? request.NationalId.Substring(request.NationalId.Length - 4) : "????");
                    return StatusCode(401, new { code = "INVALID_NATIONAL_ID", message = "National ID not authorized" });
                }

                // Check for existing unfinished session
                var existingSession = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Status == "in_progress");

                if (existingSession != null)
                {
                    _logger.LogInformation("Resuming session {SessionId} for National ID ******{LastFourDigits}", 
                        existingSession.SessionId,
                        request.NationalId.Substring(request.NationalId.Length - 4));

                    // Get total questions for this session
                    var totalQuestionsForSession = await _context.SessionItems
                        .CountAsync(si => si.SessionId == existingSession.Id);

                    return Ok(new { 
                        sessionId = existingSession.SessionId, 
                        totalQuestions = totalQuestionsForSession,
                        user = new { username = user.Username, nationalId = user.NationalId },
                        resume = true,
                        currentIndex = existingSession.CurrentIndex
                    });
                }

                // Create a new session
                var session = new Session
                {
                    UserId = user.Id,
                    SessionId = Guid.NewGuid().ToString("N"),
                    StartedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow,
                    CurrentIndex = 0,
                    Status = "in_progress",
                    Payload = JsonSerializer.Serialize(new { 
                        startedAtUtc = DateTime.UtcNow,
                        answers = new List<object>()
                    })
                };
                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                // SDJ is now DEFAULT. Filter for SDJ items (those with Dimension/SubDimension).
                // Set USE_SDJ=0 to use legacy items.
                var useSdj = Environment.GetEnvironmentVariable("USE_SDJ") != "0";
                
                // Select items based on mode
                const int TOTAL_QUESTIONS = 80; // TODO: Make configurable via appsettings
                IQueryable<Item> itemsQuery = _context.Items;
                
                // Filter for SDJ or legacy items
                if (useSdj)
                {
                    // SDJ items have non-null Dimension and SubDimension
                    itemsQuery = itemsQuery.Where(i => i.Dimension != null && i.SubDimension != null);
                    _logger.LogInformation("Starting SDJ session - filtering for SDJ items with Dimension/SubDimension");
                }
                else
                {
                    // Legacy items might not have Dimension/SubDimension
                    _logger.LogInformation("Starting LEGACY session - using all items");
                }
                
                var provider = _context.Database.ProviderName ?? string.Empty;
                if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    itemsQuery = itemsQuery.OrderBy(i => EF.Functions.Random());
                }
                else
                {
                    itemsQuery = itemsQuery.OrderBy(i => Guid.NewGuid());
                }
                var items = await itemsQuery.Take(TOTAL_QUESTIONS).ToListAsync();

                // Insert them into SessionItems
                var sessionItems = items.Select(item => new SessionItem
                {
                    Session = session,
                    ItemId = item.Id,
                    Answer = null,
                    Score = null,
                    AnsweredAt = null,
                    Item = item
                }).ToList();
                
                _context.SessionItems.AddRange(sessionItems);
                await _context.SaveChangesAsync();

                var totalQuestions = items.Count;
                return Ok(new { 
                    sessionId = session.SessionId, 
                    totalQuestions = totalQuestions,
                    user = new { username = user.Username, nationalId = user.NationalId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting session");
                return StatusCode(500, new { error = "An error occurred while starting the session" });
            }
        }

        [HttpGet("{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            try
            {
                // Find session by unique session_id
                var session = await _context.Sessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.Status == "in_progress");

                if (session == null)
                {
                    return NotFound(new { error = "Session not found" });
                }

                // Update last activity timestamp
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new {
                    sessionId = session.SessionId,
                    userNationalId = session.User.NationalId,
                    currentIndex = session.CurrentIndex,
                    lastActivityAt = session.LastActivityAt,
                    remainingCount = await _context.SessionItems.CountAsync(si => 
                        si.SessionId == session.Id && si.Answer == null)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session {SessionId}", sessionId);
                return StatusCode(500, new { error = "An error occurred while getting the session" });
            }
        }

        [HttpGet("{sessionId}/next")]
        public async Task<IActionResult> GetNextQuestion(string sessionId)
        {
            try
            {
                // Check if session exists and is in progress
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.Status == "in_progress");

                if (session == null)
                {
                    return NotFound(new { error = "Session not found or not in progress" });
                }

                // Update last activity timestamp
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Get the next unanswered item, starting after the current item if any
                var currentIndex = session.CurrentIndex;
                var nextItem = await _context.SessionItems
                    .Include(si => si.Item)
                    .Include(si => si.Session)
                    .Where(si => si.Session.Id == session.Id && si.Answer == null)
                    .OrderBy(si => si.Id)
                    .FirstOrDefaultAsync();

                // If we found no unanswered items
                if (nextItem == null)
                {
                    return Ok(new { message = "completed" });
                }

                if (nextItem == null)
                {
                    return Ok(new { message = "completed" });
                }

                var item = nextItem.Item;
                var normalizedType = (item.Type ?? string.Empty).Trim();

                (string[]? orderingChoices, string[]? orderingLabels) = (null, null);
                if (normalizedType == "ORDERING")
                {
                    (orderingChoices, orderingLabels) = GenerateOrderingChoices(item);
                }

                var response = new Models.QuestionResponse
                {
                    Id = item.Id,
                    ItemId = item.ItemCode, // Maps to item_id in JSON
                    TextAr = item.TextAr, // Maps to text_ar in JSON
                    Type = normalizedType, // Maps to type in JSON
                    DimensionTags = item.DimensionTags, // Maps to dimension_tags in JSON
                    Difficulty = item.Difficulty, // Maps to difficulty in JSON
                    TimeLimitSeconds = item.TimeLimitSeconds, // Maps to time_limit_seconds in JSON
                    MaxScore = item.MaxScore, // Maps to max_score in JSON
                    Options = BuildOptionsString(item, normalizedType), // Maps to options in JSON
                    OrderingChoices = orderingChoices, // Maps to orderingChoices in JSON
                    OrderingLabels = orderingLabels // Maps to orderingLabels in JSON
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting next question for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "An error occurred while getting the next question" });
            }
        }

        [HttpPost("{sessionId}/answer")]
        public async Task<IActionResult> SubmitAnswer(string sessionId, [FromBody] Models.SubmitAnswerRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.ItemId))
                {
                    return BadRequest(new { error = "Item ID is required" });
                }
                var answerValue = request.Answer?.Trim();

                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.Status == "in_progress");

                if (session == null)
                {
                    return NotFound(new { error = "Session not found or not in progress" });
                }

                if (!TryParseItemId(request.ItemId, out var itemCode))
                {
                    return BadRequest(new { 
                        error = "رقم السؤال غير صالح. يجب أن يكون بصيغة I001 أو M001", // Invalid Item ID format. Must be in format I001 or M001
                        code = "INVALID_ITEM_ID",
                        format = "I001 or M001" 
                    });
                }

                // Find the requested item in the session
                var sessionItem = await _context.SessionItems
                    .Include(si => si.Item)
                    .Include(si => si.Session)
                    .FirstOrDefaultAsync(si => 
                        si.Session.SessionId == sessionId && 
                        si.Item.ItemCode == itemCode);

                if (sessionItem == null)
                {
                    return NotFound(new { error = "السؤال غير موجود في هذه الجلسة" }); // Question not found in this session
                }

                // If the item is already answered, just move to the next unanswered question
                if (sessionItem.Answer != null)
                {
                    session.CurrentIndex = sessionItem.Id;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Moving to next question", skipToNext = true });
                }

                var currentType = (sessionItem.Item.Type ?? string.Empty).Trim();
                switch (currentType)
                {
                    case "TIMED_NUMERIC":
                        // Prefer NumericAnswer over string Answer
                        // For TIMED_NUMERIC, try to get a valid numeric value from either NumericAnswer or Answer
                        string? numericAnswer = request.NumericAnswer.HasValue 
                            ? request.NumericAnswer.Value.ToString(CultureInfo.InvariantCulture)
                            : request.Answer?.Trim();

                        if (string.IsNullOrWhiteSpace(numericAnswer))
                        {
                            return BadRequest(new { 
                                error = GetNumericErrorMessage("NUMERIC_EMPTY"),
                                code = "NUMERIC_EMPTY",
                                type = "TIMED_NUMERIC"
                            });
                        }
                        
                        // Use enhanced validation considering difficulty and correct answer
                        var (isValid, value, errorCode) = NumericValidator.ValidateNumericForItem(
                            numericAnswer,
                            sessionItem.Item.CorrectAnswer,
                            sessionItem.Item.Difficulty,
                            allowNegative: false
                        );
                        
                        if (!isValid || !value.HasValue)
                        {
                            return BadRequest(new { 
                                error = GetNumericErrorMessage(errorCode ?? "NUMERIC_INVALID_FORMAT"),
                                code = errorCode ?? "NUMERIC_INVALID_FORMAT",
                                type = "TIMED_NUMERIC"
                            });
                        }
                        
                        sessionItem.Answer = value.Value.ToString(CultureInfo.InvariantCulture);
                        break;

                    case "MCQ":
                        if (string.IsNullOrWhiteSpace(answerValue))
                        {
                            return BadRequest(new { 
                                error = "يجب اختيار إجابة", // Must select an answer
                                code = "MCQ_ANSWER_REQUIRED" 
                            });
                        }

                        // First get the options and verify correct_answer is included
                        var mcqOptions = ExtractMcqOptions(sessionItem.Item.Options);
                        var correctAnswer = sessionItem.Item.CorrectAnswer?.Trim();

                        if (!string.IsNullOrEmpty(correctAnswer) && !mcqOptions.Any(opt => string.Equals(opt, correctAnswer, StringComparison.Ordinal)))
                        {
                            _logger.LogWarning("MCQ item {ItemId} has correct_answer '{CorrectAnswer}' that is not in options: {Options}", 
                                sessionItem.ItemId, correctAnswer, string.Join("|", mcqOptions));
                        }

                        // Exact match only - no normalization or fuzzy matching
                        var matchedOption = mcqOptions.FirstOrDefault(opt => string.Equals(opt, answerValue, StringComparison.Ordinal));
                        
                        if (matchedOption == null)
                        {
                            // Format options nicely for error message
                            var validOptions = string.Join("، ", mcqOptions);
                            return BadRequest(new { 
                                error = $"الرجاء اختيار أحد الخيارات التالية: {validOptions}", // Please select one of these options
                                code = "MCQ_INVALID_OPTION",
                                validOptions = mcqOptions 
                            });
                        }
                        
                        sessionItem.Answer = matchedOption; // Store the exact matched option
                        break;

                    case "Text":
                        if (string.IsNullOrWhiteSpace(answerValue))
                        {
                            return BadRequest(new { error = "Text answer cannot be empty" });
                        }
                        if (answerValue.Length > 1000) // Reasonable limit for text answers
                        {
                            return BadRequest(new { error = "Text answer exceeds maximum length of 1000 characters" });
                        }
                        sessionItem.Answer = answerValue;
                        break;

                    case "ORDERING":
                        if (string.IsNullOrWhiteSpace(answerValue))
                        {
                            return BadRequest(new { 
                                error = "يجب إدخال الترتيب", // Ordering answer is required
                                code = "ORDERING_EMPTY" 
                            });
                        }

                        // Try to parse as a SelectedOrderingLabel first (A, B, C, D format)
                        try 
                        {
                            var selectedLabel = JsonSerializer.Deserialize<Models.SubmitOrderingLabelRequest>(JsonSerializer.Serialize(new { orderingLabel = answerValue }));
                            if (!string.IsNullOrEmpty(selectedLabel?.Label))
                            {
                                // Support both letter (A,B,C,D) and numeric (1,2,3,4) labels
                                string normalizedLabel = selectedLabel.Label.ToUpper().Trim();
                                
                                // Generate choices to validate against
                                var (choices, labels) = GenerateOrderingChoices(sessionItem.Item);
                                if (choices != null && labels != null)
                                {
                                    int labelIndex = -1;
                                    
                                    // Check for letter label (A, B, C, D)
                                    if (normalizedLabel.Length == 1 && "ABCD".Contains(normalizedLabel))
                                    {
                                        labelIndex = Array.IndexOf(labels, normalizedLabel);
                                    }
                                    // Check for numeric label (1, 2, 3, 4)
                                    else if (int.TryParse(normalizedLabel, out var numLabel) && numLabel >= 1 && numLabel <= labels.Length)
                                    {
                                        labelIndex = numLabel - 1; // Convert to 0-based index
                                    }
                                    
                                    if (labelIndex >= 0 && labelIndex < choices.Length)
                                    {
                                        // Store the selected choice as the answer
                                        sessionItem.Answer = choices[labelIndex];
                                        break;
                                    }
                                    else
                                    {
                                        return BadRequest(new { 
                                            error = "تسمية الترتيب غير صحيحة", // Invalid ordering label
                                            code = "ORDERING_BAD_TOKEN",
                                            validLabels = labels
                                        });
                                    }
                                }
                            }
                        }
                        catch { /* Ignore deserialization errors */ }

                        // Fallback to direct sequence format validation (1|2|3|4)
                        if (answerValue.Contains(",") || answerValue.Contains(";"))
                        {
                            return BadRequest(new { 
                                error = "يجب أن يكون الترتيب مفصولاً بعلامة '|' (مثال: 1|2|3|4)", // Ordering must use pipe separator
                                code = "ORDERING_INVALID_SEPARATOR",
                                example = "1|2|3|4"
                            });
                        }
                        
                        // Parse and validate the sequence format
                        var orderParts = answerValue.Split('|', StringSplitOptions.RemoveEmptyEntries);
                        if (orderParts.Length == 0)
                        {
                            return BadRequest(new { 
                                error = "يجب إدخال الترتيب", // Ordering answer is required
                                code = "ORDERING_INCOMPLETE" 
                            });
                        }
                        
                        // Validate that all parts are numbers
                        var orderNumbers = new List<int>();
                        foreach (var part in orderParts)
                        {
                            if (!int.TryParse(part.Trim(), out var num))
                            {
                                return BadRequest(new { 
                                    error = "الترتيب يجب أن يتكون من أرقام مفصولة بعلامة '|' (مثال: 1|2|3|4)", // Must be numbers separated by pipes
                                    code = "ORDERING_INVALID_FORMAT",
                                    example = "1|2|3|4"
                                });
                            }
                            orderNumbers.Add(num);
                        }
                        
                        // Get the expected number of items from the question
                        var expectedItemCount = GetExpectedOrderingItemCount(sessionItem.Item);
                        if (expectedItemCount <= 0)
                        {
                            return BadRequest(new { 
                                error = "خطأ في تكوين السؤال", // Question configuration error
                                code = "ORDERING_QUESTION_ERROR"
                            });
                        }
                        
                        // Check if the sequence is incomplete
                        if (orderNumbers.Count != expectedItemCount)
                        {
                            return BadRequest(new { 
                                error = $"يجب ترتيب جميع العناصر ({expectedItemCount} عناصر)", // Must order all items
                                code = "ORDERING_INCOMPLETE",
                                expected = expectedItemCount,
                                received = orderNumbers.Count
                            });
                        }
                        
                        // Check if it's a valid permutation (contains all numbers from 1 to expectedItemCount exactly once)
                        var expectedSequence = Enumerable.Range(1, expectedItemCount).ToList();
                        var sortedNumbers = orderNumbers.OrderBy(x => x).ToList();
                        
                        if (!sortedNumbers.SequenceEqual(expectedSequence))
                        {
                            return BadRequest(new { 
                                error = $"يجب أن يحتوي الترتيب على جميع الأرقام من 1 إلى {expectedItemCount} بدون تكرار", // Must contain all numbers from 1 to N without repetition
                                code = "ORDERING_INVALID_PERM",
                                expected = expectedSequence,
                                received = orderNumbers
                            });
                        }
                        
                        sessionItem.Answer = string.Join("|", orderNumbers);
                        break;

                    case "LikertAgreement":
                    case "Frequency":
                        if (string.IsNullOrWhiteSpace(answerValue))
                        {
                            return BadRequest(new { 
                                error = currentType == "LikertAgreement" 
                                    ? "يرجى اختيار درجة موافقتك" 
                                    : "يرجى اختيار التكرار المناسب", 
                                code = "OPTION_REQUIRED",
                                validOptions = currentType == "LikertAgreement" 
                                    ? LikertOptions.Select(o => o.Label)
                                    : FrequencyOptions.Select(o => o.Label)
                            });
                        }
                        
                        var options = currentType == "LikertAgreement" ? LikertOptions : FrequencyOptions;

                        // Normalize input by removing extra whitespace and normalizing Arabic characters
                        var normalizedInput = TextUtils.NormalizeArabicText(answerValue);

                        // First try exact match by Arabic label
                        var validOption = options.FirstOrDefault(o => 
                            string.Equals(TextUtils.NormalizeArabicText(o.Label), normalizedInput, StringComparison.Ordinal));
                        
                        // If no exact match, try matching by value (1-5)
                        // For SDJ mode, we accept numeric values directly
                        if (validOption == null && int.TryParse(answerValue, out var numericValue))
                        {
                            validOption = options.FirstOrDefault(o => o.Value == numericValue.ToString());
                            // Accept numeric values silently for SDJ compatibility
                        }

                        if (validOption == null)
                        {
                            // No valid match found
                            var validLabels = string.Join("، ", options.Select(o => o.Label));
                            var typeAr = currentType == "LikertAgreement" ? "درجة الموافقة" : "التكرار";
                            return BadRequest(new { 
                                error = $"يجب اختيار {typeAr} من القائمة: {validLabels}", // Must select from list
                                code = "INVALID_OPTION",
                                validOptions = options.Select(o => o.Label)
                            });
                        }
                        
                        // Store numeric value (1-5) for scoring
                        sessionItem.Answer = validOption.Value;
                        break;

                    default:
                        if (string.IsNullOrWhiteSpace(answerValue))
                        {
                            return BadRequest(new { error = "Answer is required" });
                        }
                        sessionItem.Answer = answerValue;
                        break;
                }
                sessionItem.AnsweredAt = DateTime.UtcNow;
                if (request.ResponseTimeMs.HasValue)
                {
                    sessionItem.ResponseTimeMs = request.ResponseTimeMs;
                }

                // Update session state
                session.LastActivityAt = DateTime.UtcNow;
                session.CurrentIndex++;

                // Update session payload with new answer
                var payload = string.IsNullOrEmpty(session.Payload)
                    ? new SessionPayload { StartedAtUtc = session.StartedAt }
                    : JsonSerializer.Deserialize<SessionPayload>(session.Payload);

                if (payload != null)
                {
                    payload.Answers.Add(new SessionAnswer {
                        ItemId = request.ItemId,
                        Type = sessionItem.Item.Type,
                        Value = request.Answer,
                        Timestamp = DateTime.UtcNow
                    });
                    session.Payload = JsonSerializer.Serialize(payload);
                }

                _logger.LogDebug("Session {SessionId}, Item {ItemId}, ResponseTimeMs: {ResponseTimeMs}ms", 
                    session.SessionId, request.ItemId, request.ResponseTimeMs);

                // Calculate score using the dedicated calculator
                sessionItem.Score = ScoreCalculator.CalculateScore(sessionItem);
                
                // Log scoring results for debugging
                _logger.LogDebug("Scored item {ItemId} of type {Type}. Answer: '{Answer}', Score: {Score}/{MaxScore}", 
                    sessionItem.ItemId, sessionItem.Item?.Type, sessionItem.Answer, 
                    sessionItem.Score, sessionItem.Item?.MaxScore);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Answer submitted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer for session {SessionId}", sessionId);
                return StatusCode(500, new { error = "An error occurred while submitting the answer" });
            }
        }

        [HttpPost("{sessionId}/finish")]
        public async Task<IActionResult> FinishSession(string sessionId)
        {
            try
            {
                // Check if session exists and is in progress
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.Status == "in_progress");

                if (session == null)
                {
                    return NotFound(new { code = "SESSION_NOT_FOUND", error = "Session not found or not in progress" });
                }

                // Mark session as finished
                session.Status = "FINISHED";
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { finished = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing session {SessionId}", sessionId);
                return StatusCode(500, new { error = "An error occurred while finishing the session" });
            }
        }

        [HttpPost("{sessionId}/submit")]
        public async Task<IActionResult> SubmitSession(string sessionId)
        {
            try
            {
                // Check if session exists and can be submitted
                var session = await _context.Sessions
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && 
                        (s.Status == "in_progress" || s.Status == "FINISHED"));

                if (session == null)
                {
                    return NotFound(new { error = "Session not found or cannot be submitted" });
                }

                // Mark session as completed
                session.Status = "completed";
                session.EndedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // SDJ is now DEFAULT mode. Set USE_SDJ=0 to use legacy scoring.
                var useSdj = Environment.GetEnvironmentVariable("USE_SDJ") != "0" && _sdjScoringService != null;

                // Compute scores via appropriate scoring service
                var serviceSummary = useSdj 
                    ? await ComputeSdjScoresWrapper(session.Id)
                    : await _scoringService.ComputeSessionScores(session.Id);

                var modelSummary = serviceSummary; // Since we're using the same model now

                // Upsert result
                var result = await _context.Results.FirstOrDefaultAsync(r => r.SessionId == session.Id);
                if (result == null)
                {
                    result = new Result
                    {
                        SessionId = session.Id,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Results.Add(result);
                }

                result.TotalScore = (int)System.Math.Round(modelSummary.TotalScore);
                result.ScoringModelVersion = modelSummary.Version;

                var jsonOptions = new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    WriteIndented = false
                };

                // Store SDJ data if available
                if (useSdj)
                {
                    var sdjScores = await _sdjScoringService!.ComputeSdjScores(session.Id);
                    
                    // Store SDJ-specific data in JSON - INCLUDING 7-PATTERN SCORES (critical for new report)
                    result.DimensionScoresJson = JsonSerializer.Serialize(new
                    {
                        Dimensions = sdjScores.Dimensions,
                        SubDimensions = sdjScores.SubDimensions,
                        TrackFits = sdjScores.TrackFits,
                        SevenPatternScores = sdjScores.SevenPatternScores, // NEW: Required for ModernSdjSevenPatternReportService
                        Version = sdjScores.Version
                    }, jsonOptions);
                    
                    result.CompositeScoresJson = JsonSerializer.Serialize(new
                    {
                        TotalScore = sdjScores.TotalScore.T,
                        RawScore = sdjScores.TotalScore.Raw,
                        Percentile = sdjScores.TotalScore.Percentile
                    }, jsonOptions);
                    
                    // CRITICAL: Store sdjData in Session.Payload for AdminController to access
                    var payload = string.IsNullOrEmpty(session.Payload)
                        ? new SessionPayload { StartedAtUtc = session.StartedAt }
                        : JsonSerializer.Deserialize<SessionPayload>(session.Payload);
                    
                    if (payload != null)
                    {
                        payload.SdjData = new SdjDataPayload
                        {
                            Dimensions = sdjScores.Dimensions.Select(d => new SdjDimensionPayload
                            {
                                Dimension = d.Dimension,
                                Raw = d.Raw,
                                T = d.T,
                                Percentile = d.Percentile,
                                Band = d.Band
                            }).ToList(),
                            SubDimensions = sdjScores.SubDimensions.Select(sd => new SdjSubDimensionPayload
                            {
                                Dimension = sd.Dimension,
                                SubDimension = sd.SubDimension,
                                T = sd.T,
                                Band = sd.Band
                            }).ToList(),
                            TrackFits = sdjScores.TrackFits.Select(tf => new SdjTrackFitPayload
                            {
                                TrackNameAr = tf.TrackNameAr,
                                FitLevel = tf.FitLevel,
                                FitScore = tf.FitScore
                            }).ToList(),
                            SevenPatternScores = sdjScores.SevenPatternScores?.Select(sp => new SevenPatternPayload
                            {
                                PatternNameAr = sp.PatternNameAr,
                                PatternNameEn = sp.PatternNameEn,
                                TScore = sp.TScore,
                                Band = sp.Band,
                                SubDimensions = sp.SubDimensions.Select(sd => sd.SubDimension).ToList()
                            }).ToList(),
                            Version = sdjScores.Version
                        };
                        session.Payload = JsonSerializer.Serialize(payload, jsonOptions);
                    }
                }
                else
                {
                    result.DimensionScoresJson = JsonSerializer.Serialize(modelSummary.DimensionScores, jsonOptions);
                    result.CompositeScoresJson = JsonSerializer.Serialize(new { TotalScore = modelSummary.TotalScore }, jsonOptions);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "submitted", sessionId = session.SessionId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting session {SessionId}", sessionId);
                return StatusCode(500, new { error = "An error occurred while submitting the session" });
            }
        }

        private async Task<ScoreSummary> ComputeSdjScoresWrapper(int sessionId)
        {
            var sdjScores = await _sdjScoringService!.ComputeSdjScores(sessionId);
            
            // Convert SDJ scores to legacy ScoreSummary format for compatibility
            return new ScoreSummary
            {
                TotalScore = sdjScores.TotalScore.T,
                Version = sdjScores.Version,
                DimensionScores = sdjScores.Dimensions.Select(d => new DimensionScore
                {
                    Dimension = d.Dimension,
                    Raw = d.Raw,
                    T = d.T,
                    Percentile = d.Percentile,
                    Z = (d.T - 50) / 10 // Convert T-score back to Z-score
                }).ToList()
            };
        }

        private static string? BuildOptionsString(Item item, string normalizedType)
        {
            if (string.Equals(normalizedType, "LikertAgreement", StringComparison.Ordinal))
            {
                return string.Join("|", LikertOptions.Select(o => o.Label));
            }
            if (string.Equals(normalizedType, "Frequency", StringComparison.Ordinal))
            {
                return string.Join("|", FrequencyOptions.Select(o => o.Label));
            }
            if (string.Equals(normalizedType, "MCQ", StringComparison.Ordinal))
            {
                return item.Options;
            }
            return null;
        }

        private static List<string> ExtractOptions(string? raw)
        {
            var values = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return values;
            }

            var separators = new[] { "|", ";", "؟", "," };
            foreach (var separator in separators)
            {
                if (raw.Contains(separator, StringComparison.Ordinal))
                {
                    values = raw.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    break;
                }
            }

            return values;
        }

        private static (string[]? choices, string[]? labels) GenerateOrderingChoices(Item item)
        {
            try
            {
                // Extract items after colon ":"
                var colonIndex = item.TextAr.IndexOf(':');
                if (colonIndex == -1) return (null, null);

                var itemsText = item.TextAr.Substring(colonIndex + 1);
                var tokens = itemsText.Split('،', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                if (tokens.Count == 0) return (null, null);

                // Parse correct sequence from correct_answer
                if (string.IsNullOrWhiteSpace(item.CorrectAnswer)) return (null, null);
                var correctSequence = item.CorrectAnswer.Split('|')
                    .Select(s => int.TryParse(s, out var n) ? n - 1 : -1)
                    .Where(n => n >= 0 && n < tokens.Count)
                    .ToList();

                if (correctSequence.Count != tokens.Count) return (null, null);

                // Create correct ordering
                var correctOrdering = new List<string>();
                foreach (var index in correctSequence)
                {
                    correctOrdering.Add(tokens[index]);
                }

                // Generate 3 distractors
                var choices = new List<string> { string.Join(" | ", correctOrdering) };
                var rng = new Random();

                // Helper function to swap two items
                void Swap<T>(IList<T> list, int i, int j)
                {
                    var temp = list[i];
                    list[i] = list[j];
                    list[j] = temp;
                }

                // Create distractors with different permutations
                for (var i = 0; i < 3; i++)
                {
                    var distractor = correctOrdering.ToList();
                    switch (i)
                    {
                        case 0: // Swap adjacent
                            if (distractor.Count >= 2)
                            {
                                var pos = rng.Next(0, distractor.Count - 1);
                                Swap(distractor, pos, pos + 1);
                            }
                            break;
                        case 1: // Reverse a section
                            if (distractor.Count >= 3)
                            {
                                var start = rng.Next(0, distractor.Count - 2);
                                var length = rng.Next(2, Math.Min(4, distractor.Count - start));
                                distractor.Reverse(start, length);
                            }
                            break;
                        case 2: // Move one element
                            if (distractor.Count >= 3)
                            {
                                var from = rng.Next(0, distractor.Count);
                                var to = rng.Next(0, distractor.Count - 1);
                                if (to >= from) to++;
                                var element = distractor[from];
                                distractor.RemoveAt(from);
                                distractor.Insert(to, element);
                            }
                            break;
                    }
                    choices.Add(string.Join(" | ", distractor));
                }

                return (
                    choices.ToArray(),
                    new[] { "A", "B", "C", "D" }
                );
            }
            catch (Exception)
            {
                return (null, null);
            }
        }

        private static List<string> ExtractMcqOptions(string? raw)
        {
            var values = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return values;
            }

            var separators = new[] { "|", ";", "؟", "," };
            foreach (var separator in separators)
            {
                if (raw.Contains(separator, StringComparison.Ordinal))
                {
                    values = raw.Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    break;
                }
            }

            return values;
        }

        private static bool TryParseItemId(string? raw, out string itemCode)
        {
            itemCode = string.Empty;
            
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var trimmed = raw.Trim();
            
            // Accept I prefix (legacy) or M prefix (MCQ) with exactly 3 digits
            if (trimmed.Length == 4 && 
                (trimmed.StartsWith("I", StringComparison.OrdinalIgnoreCase) || 
                 trimmed.StartsWith("M", StringComparison.OrdinalIgnoreCase)))
            {
                var prefix = trimmed.Substring(0, 1).ToUpper();
                var numericPart = trimmed.Substring(1);
                if (int.TryParse(numericPart, out var itemId) && itemId > 0 && itemId <= 999)
                {
                    itemCode = $"{prefix}{itemId:D3}";
                    return true;
                }
            }
            
            return false;
        }

        private static bool RequestValidForTimedNumeric(Models.SubmitAnswerRequest request, out double? numeric, out string? error)
        {
            numeric = null;
            error = null;

            // Try NumericAnswer first if it has a value
            if (request.NumericAnswer.HasValue)
            {
                var rawValue = request.NumericAnswer.Value.ToString(CultureInfo.InvariantCulture);
                if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    numeric = value;
                    return true;
                }
                error = "NUMERIC_INVALID_FORMAT";
                return false;
            }

            // Fall back to Answer property if NumericAnswer is not provided
            if (!string.IsNullOrWhiteSpace(request.Answer))
            {
                if (double.TryParse(request.Answer, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    numeric = value;
                    return true;
                }
                error = "NUMERIC_INVALID_FORMAT";
                return false;
            }

            error = "NUMERIC_EMPTY";
            return false;
        }

        // Removed unused FormatItemCode method since we use stored ItemCode property

        private static string GetNumericErrorMessage(string code) => code switch
        {
            "NUMERIC_EMPTY" => "الرجاء إدخال قيمة رقمية.", // Please enter a numeric value
            "NUMERIC_NEGATIVE_NOT_ALLOWED" => "لا يسمح بالأرقام السالبة.", // Negative numbers are not allowed
            "NUMERIC_INVALID_RANGE" => "القيمة المدخلة خارج النطاق المسموح.", // The entered value is outside the allowed range
            "NUMERIC_INVALID_FORMAT" => "الرجاء إدخال رقم صحيح.", // Please enter a valid number
            "NUMERIC_INVALID_TOLERANCE" => "القيمة المدخلة بعيدة عن النطاق المتوقع.", // The entered value is far from the expected range
            _ => "قيمة رقمية غير صالحة." // Invalid numeric input
        };

        private static int GetExpectedOrderingItemCount(Item item)
        {
            try
            {
                // Extract items after colon ":"
                var colonIndex = item.TextAr.IndexOf(':');
                if (colonIndex == -1) return 0;

                var itemsText = item.TextAr.Substring(colonIndex + 1);
                var tokens = itemsText.Split('،', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToList();

                return tokens.Count;
            }
            catch
            {
                return 0;
            }
        }
    }

    }
