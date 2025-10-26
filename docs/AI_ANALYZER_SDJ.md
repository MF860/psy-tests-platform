# AI Analyzer for SDJ Results - Technical Documentation

## Overview

This document describes the AI-powered analysis system for SDJ (Sustainable Development Journey) test results, now using **DeepSeek API** directly.

## Architecture

### Backend Components

1. **DeepSeekClient** (`Services/AI/DeepSeekClient.cs`)
   - Direct integration with DeepSeek API
   - Handles HTTP communication, retries, caching
   - Parses SDJ dimension data into structured prompts

2. **AiAnalyzerService** (`Services/AI/AiAnalyzerService.cs`)
   - Orchestrates analysis requests
   - Provides fallback to rule-based analysis if API unavailable

3. **AdminAiController** (`Controllers/AdminAiController.cs`)
   - REST endpoint: `POST /api/admin/ai/analyze`
   - Validates result data, calls analyzer service
   - Rate-limited to prevent abuse

### Frontend Components

1. **ResultDetail.tsx** - Displays AI analysis in admin panel
2. **adminContract.ts** - TypeScript types matching backend schema

## Configuration

### Environment Variables (Server-Side Only)

**Critical**: API keys must **NEVER** be stored in code or config files.

```bash
# Windows PowerShell
setx DEEPSEEK_API_KEY "sk-9c6a11074bac4e598ffef48e9bad9380"

# Linux/Mac
export DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"
```

### Optional Configuration

```bash
# Model selection (default: deepseek-chat)
DEEPSEEK_MODEL="deepseek-chat"

# Max tokens for response (default: 1500)
DEEPSEEK_MAX_TOKENS=1500

# Temperature for creativity (default: 0.2)
DEEPSEEK_TEMPERATURE=0.2

# API base URL (default: https://api.deepseek.com/v1)
DEEPSEEK_BASE_URL="https://api.deepseek.com/v1"
```

### Legacy Compatibility

The system still reads `OPENROUTER_API_KEY` for backward compatibility, but now uses DeepSeek API directly.

## Data Flow

```
User Request (Admin Panel)
    ↓
AdminAiController.Analyze(resultId)
    ↓
Validate Result & Parse SDJ Dimensions
    ↓
AiAnalyzerService.AnalyzeAsync()
    ↓
DeepSeekClient.AnalyzeAsync()
    ↓
Build SDJ-specific prompt (Arabic)
    ↓
Call DeepSeek API /chat/completions
    ↓
Parse JSON response
    ↓
Cache result (24 hours)
    ↓
Return AiAnalysisResponse
```

## SDJ Prompt Structure

The system builds prompts specifically for SDJ's **7 core dimensions**:

### System Prompt (Arabic)

```
أنت محلّل سيكومتري محترف متخصص في تحليل نتائج إطار SDJ (الأبعاد السبعة للتنمية المستدامة).

القواعد الصارمة:
1. أعد الإجابة بصيغة JSON فقط
2. استخدم العربية الفصحى الواضحة
3. لا تذكر أي بيانات شخصية (اسم، رقم وطني)
4. اجعل كل توصية عملية (4-6 أسابيع)
5. استخدم معايير T-Score: >60 = قوة، <40 = ضعف
```

### User Prompt

Contains:
- Result ID (anonymized)
- Total raw score
- Average T-Score
- Top 5 strengths (T ≥ 55)
- Top 5 weaknesses (T < 45)
- Full dimension breakdown

**Example:**
```
**تحليل نتائج SDJ (مجهول الهوية):**

معرّف النتيجة: 123
مجموع الدرجات الخام: 245
متوسط T-Score: 52.3
عدد الأبعاد: 7

**أعلى الأبعاد (نقاط القوة):**
• الأنماط الشخصية: T=62.1
• القدرات المعرفية والعقلية: T=58.4

**أضعف الأبعاد (مجالات التطوير):**
• الصحة النفسية والانفعالية: T=38.7

**جميع الأبعاد السبعة:**
• الأنماط الشخصية: Raw=42, T=62.1
• القدرات المعرفية والعقلية: Raw=38, T=58.4
...
```

## Response Schema

```json
{
  "analysis": {
    "strengths": [
      "قوة استثنائية في التفكير المنطقي والتحليلي",
      "قدرة عالية على حل المشكلات المعقدة"
    ],
    "weaknesses": [
      "يحتاج إلى تطوير مهارات إدارة الضغط",
      "الثقة بالنفس في المواقف الاجتماعية تحتاج دعم"
    ],
    "recommendations": [
      "مارس تقنيات الاسترخاء يومياً لمدة 15 دقيقة",
      "انضم لمجموعة تطوير الذات الأسبوعية"
    ],
    "summary": "النتائج تظهر أداءً جيداً بمتوسط T-Score قدره 52.3...",
    "methodology": "تحليل مبني على SDJ T-scores بدون بيانات شخصية",
    "categories": [
      {
        "name": "الأنماط الشخصية",
        "t": 62.1,
        "note": "أداء ممتاز - استثمر في هذا المجال"
      },
      {
        "name": "القدرات المعرفية والعقلية",
        "t": 58.4,
        "note": "أداء جيد جداً - حافظ على هذا المستوى"
      }
    ]
  },
  "model": "deepseek-chat",
  "usage": {
    "promptTokens": 523,
    "completionTokens": 412,
    "totalTokens": 935,
    "latencyMs": 2341.5
  },
  "generatedAt": "2025-10-26T14:30:00Z"
}
```

## Security Measures

### 1. API Key Protection
- ✅ Keys read from environment variables ONLY
- ✅ Never logged in full (redacted to first 10 + last 4 chars)
- ✅ Not stored in appsettings.json
- ✅ Not exposed to frontend

### 2. PII (Personally Identifiable Information) Protection
- ✅ No names, national IDs sent to AI
- ✅ Only anonymized T-Scores and dimension names
- ✅ Result ID used for tracking, not personal data

### 3. Logging Best Practices
```csharp
// ✅ Good - length only
_logger.LogInformation("Prompt length={Length}", prompt.Length);

// ✅ Good - redacted key
var redacted = key[..10] + "..." + key[^4..];

// ❌ Bad - full prompt (may contain data)
_logger.LogInformation("Prompt={Prompt}", prompt);

// ❌ Bad - full API key
_logger.LogInformation("Key={Key}", apiKey);
```

### 4. Rate Limiting
- Admin endpoint has rate limiting via `[EnableRateLimiting("admin")]`
- Configured in `Program.cs`

### 5. Caching
- Results cached for 24 hours by `resultId + dimensionsHash`
- Reduces API costs and improves performance
- Cache key: `ai_sdj_analysis_{resultId}_{hash}`

## Error Handling & Fallback

### Fallback Strategy

When DeepSeek API is unavailable, the system uses **rule-based analysis**:

1. **Strengths**: T-Scores ≥ 55
2. **Weaknesses**: T-Scores < 45
3. **Recommendations**: Generic but practical advice
4. **Categories**: Automatic notes based on T-Score ranges

```csharp
T ≥ 65 → "أداء استثنائي - استثمر في هذا المجال"
55 ≤ T < 65 → "أداء جيد جداً - حافظ على هذا المستوى"
45 ≤ T < 55 → "أداء متوسط - مجال للتحسين التدريجي"
35 ≤ T < 45 → "أداء أقل من المتوسط - يحتاج تطوير مركز"
T < 35 → "أداء ضعيف - يحتاج تدخل عاجل"
```

### Error Types

| Error | Status | Response | Action |
|-------|--------|----------|--------|
| No API Key | 503 | "خدمة الذكاء الاصطناعي غير متوفرة" | Uses fallback |
| API Rate Limited | 503 | Retries with exponential backoff | 2s, 4s, 8s |
| Invalid Response | 503 | "حدث خطأ في توليد التحليل" | Uses fallback |
| No SDJ Data | 400 | "لا توجد بيانات أبعاد SDJ" | Returns error |

## Frontend Display

### Components

1. **Summary Card** (indigo) - Brief overview
2. **Three Columns** - Strengths, Weaknesses, Recommendations
3. **SDJ Categories Table** - 7 dimensions with T-Scores and notes
4. **Methodology Card** - Explains analysis basis
5. **Performance Metrics** - Token usage, latency

### RTL Support

All text sections use `dir="rtl"` for proper Arabic display.

### Color Coding

- **Green** (T ≥ 60): Excellent performance
- **Yellow** (45 ≤ T < 60): Average performance
- **Red** (T < 45): Needs development

## Testing

### Manual Testing

1. **Set API Key**
   ```bash
   setx DEEPSEEK_API_KEY "your-key-here"
   ```

2. **Restart Backend**
   ```bash
   cd backend/PsyApi
   dotnet run
   ```

3. **Open Admin Panel**
   - Navigate to a result detail page
   - Click "تشغيل التحليل" (Start Analysis)
   - Verify Arabic output with proper RTL

4. **Test Fallback**
   - Unset API key: `setx DEEPSEEK_API_KEY ""`
   - Restart backend
   - Should see "تحليل احتياطي" (Fallback analysis)

### Integration Tests

```csharp
[Fact]
public async Task AnalyzeAsync_WithValidSDJData_ReturnsAnalysis()
{
    // Arrange
    var result = new Result { Id = 1, DimensionScoresJson = "..." };
    var dimensions = ParseDimensions(result);
    
    // Act
    var response = await _analyzer.AnalyzeAsync(result, dimensions);
    
    // Assert
    Assert.NotNull(response.Analysis);
    Assert.NotEmpty(response.Analysis.Strengths);
    Assert.NotEmpty(response.Analysis.Categories);
}
```

## Performance Metrics

- **Cache Hit Rate**: ~70% (depends on usage pattern)
- **Average Latency**: 2-4 seconds (DeepSeek API)
- **Fallback Latency**: <100ms (rule-based)
- **Token Usage**: 500-1500 tokens per analysis

## Troubleshooting

### Issue: "خدمة الذكاء الاصطناعي غير متوفرة"

**Cause**: API key not set or invalid

**Solution**:
```bash
setx DEEPSEEK_API_KEY "sk-9c6a11074bac4e598ffef48e9bad9380"
```
Then restart backend.

### Issue: Analysis shows "تحليل احتياطي"

**Cause**: DeepSeek API call failed

**Check**:
1. Verify API key is correct
2. Check network connectivity
3. Review backend logs for errors

### Issue: Empty categories array

**Cause**: Frontend type mismatch or backend didn't return categories

**Solution**: Ensure backend `AiAnalysisResult` includes `Categories` property and frontend types match.

## Deployment Checklist

- [ ] Set `DEEPSEEK_API_KEY` in production environment
- [ ] Remove any hardcoded keys from code
- [ ] Verify rate limiting is enabled
- [ ] Test fallback behavior
- [ ] Monitor cache hit rate
- [ ] Set up log monitoring (errors, latency)
- [ ] Document API key rotation process

## API Cost Optimization

1. **Caching**: 24-hour cache reduces duplicate requests
2. **Lazy Loading**: Analysis only runs when requested
3. **Efficient Prompts**: Concise prompts reduce token usage
4. **Fallback**: Rule-based analysis avoids API costs

## Future Enhancements

- [ ] Multi-language support (English, French)
- [ ] Custom prompt templates per organization
- [ ] A/B testing for prompt variations
- [ ] Historical trend analysis
- [ ] Batch analysis for multiple results

---

**Last Updated**: 2025-10-26  
**Version**: 1.0  
**Contact**: Development Team
