# AI Analyzer Integration

This document describes the DeepSeek AI analyzer integration through OpenRouter, which replaces the mock AI analyzer in the Admin Result Details page with real, structured AI analysis.

## 🎯 Overview

The AI analyzer provides:
- **Strengths**: AI-identified strong points based on T-scores ≥60
- **Weaknesses**: Areas for improvement with T-scores <40
- **Recommendations**: Actionable suggestions for development
- **Rationale**: Brief reasoning behind the analysis

## 🔐 Security Features

- ✅ **No API Key in Frontend**: OpenRouter key stored server-side only
- ✅ **Privacy Protection**: No PII sent to AI service (only aggregated scores)
- ✅ **Rate Limiting**: Built-in throttling for admin endpoints
- ✅ **Error Handling**: Graceful fallbacks when AI service unavailable
- ✅ **Caching**: Results cached for 24 hours to reduce API calls

## 🏗️ Architecture

```
Frontend (Admin UI) → Backend Proxy → OpenRouter API → DeepSeek
      ↓                    ↓              ↓            ↓
   UI State           Secure Config    HTTP Client    AI Model
```

### Backend Components

1. **OpenRouterConfiguration**: Service configuration
2. **OpenRouterClient**: HTTP client with retry logic
3. **AiAnalyzerService**: Main service with fallback
4. **AdminAiController**: REST endpoint

### Frontend Components

1. **AIAnalyzer**: React component with loading states
2. **AdminApi.aiAnalyze()**: API client method
3. **AiAnalysisResponse**: TypeScript contracts

## 🛠️ Setup Instructions

### 1. Environment Configuration

Set the OpenRouter API key as an environment variable:

```bash
# Windows PowerShell
$env:OPENROUTER_API_KEY="your-openrouter-key-here"

# Linux/macOS
export OPENROUTER_API_KEY="your-openrouter-key-here"

# Docker
OPENROUTER_API_KEY="your-openrouter-key-here"
```

### 2. Configuration Files

The system reads configuration from multiple sources (environment variables take precedence):

**appsettings.Development.json:**
```json
{
  "OpenRouter": {
    "ApiKey": "REPLACE_WITH_ENV_VAR",
    "Model": "deepseek/deepseek-chat",
    "BaseUrl": "https://openrouter.ai/api/v1",
    "MaxTokens": 800,
    "Temperature": 0.2,
    "MaxRetries": 3,
    "TimeoutSeconds": 15,
    "CacheDurationHours": 24
  }
}
```

**Supported Environment Variables:**
- `OPENROUTER_API_KEY`: Your OpenRouter API key (required)
- `OPENROUTER_MODEL`: AI model to use (default: deepseek/deepseek-chat)
- `OPENROUTER_MAX_TOKENS`: Max response tokens (default: 800)
- `OPENROUTER_TEMPERATURE`: AI temperature (default: 0.2)
- `OPENROUTER_BASE_URL`: OpenRouter base URL (default: https://openrouter.ai/api/v1)

### 3. Model Options

**Recommended Models:**

| Model | Speed | Cost | Reasoning Quality |
|-------|-------|------|-------------------|
| `deepseek/deepseek-chat` | Fast | Low | Good |
| `deepseek/deepseek-reasoner` | Slower | Higher | Excellent |

### 4. Running the Services

**Backend:**
```bash
cd backend/PsyApi
dotnet run
# Listens on http://localhost:5019
```

**Frontend Admin UI:**
```bash
cd frontend/admin-ui
npm run dev
# Listens on http://localhost:5173
```

## 📡 API Reference

### Endpoint: `POST /api/admin/ai/analyze`

**Request:**
```json
{
  "resultId": 123
}
```

**Response:**
```json
{
  "analysis": {
    "strengths": [
      "قوة متميزة في الذكاء العام",
      "مهارات تحليلية عالية"
    ],
    "weaknesses": [
      "مجال للتطوير في الاستقرار العاطفي"
    ],
    "recommendations": [
      "ركز على تطوير نقاط القوة في مجالات العمل",
      "استمر في التدريب والتطوير المستمر"
    ],
    "rationale": "التحليل مبني على درجات T-Score والأنماط الإحصائية"
  },
  "model": "deepseek/deepseek-chat",
  "usage": {
    "promptTokens": 120,
    "completionTokens": 85,
    "totalTokens": 205,
    "latencyMs": 1250
  },
  "generatedAt": "2025-01-10T15:30:45Z"
}
```

**Error Codes:**
- `400`: Invalid request (missing resultId)
- `404`: Result not found
- `502`: AI service unavailable
- `429`: Rate limit exceeded

## 🎨 User Interface

### Features
- **Loading State**: Animated spinner during AI processing
- **Three-Column Layout**: Clean display of strengths, weaknesses, recommendations
- **Error Handling**: User-friendly Arabic error messages with retry option
- **Performance Metrics**: Token usage and latency information
- **Caching Indicator**: Shows when results are served from cache
- **Fallback Mode**: Basic analysis when AI service is down

### Arabic Language Support
- All AI responses are in formal Arabic (الفصحى)
- RTL-compatible layout
- Localized date/time formatting
- Arabic number formatting

## 🔧 Troubleshooting

### Common Issues

**1. "خدمة الذكاء الاصطناعي غير متوفرة حالياً"**
- Check OpenRouter API key is set correctly
- Verify internet connectivity to OpenRouter
- Check backend logs for detailed errors

**2. Rate Limiting**
- Built-in exponential backoff handles temporary limits
- For persistent issues, consider upgrading OpenRouter plan

**3. Slow Response Times**
- Normal latency: 1-3 seconds
- Consider switching to `deepseek-chat` for faster responses
- Check network connectivity

**4. Arabic Text Issues**
- Ensure proper UTF-8 encoding
- Check browser font support for Arabic

### Backend Logs

The system logs all AI operations:
```
[AI] Analyze resultId=123 model=deepseek/deepseek-chat latencyMs=1250 status=OK
[AI] Cache hit for resultId=123
[AI] Analyze resultId=456 model=deepseek/deepseek-chat latencyMs=2100 status=FAIL
```

### Monitoring

Track these metrics:
- Response times (target: <3s)
- Error rates (target: <5%)
- Cache hit rates (target: >70%)
- Token usage (for cost control)

## 📊 Performance Optimization

### Caching Strategy
- Results cached for 24 hours by default
- Cache key includes dimension scores hash
- Automatic cache invalidation
- Memory cache with size limits

### Token Management
- Prompts optimized to ~120 tokens
- Response limited to 800 tokens max
- Automatic text truncation for safety
- Privacy-aware data filtering

## 🚀 Deployment Considerations

### Production Setup
1. Use proper secret management (Azure Key Vault, etc.)
2. Set up monitoring and alerting
3. Configure proper CORS for production domains
4. Enable HTTPS for all communications
5. Set up log aggregation

### Scaling
- The service is stateless and scales horizontally
- Consider connection pooling for high volume
- Monitor OpenRouter usage limits
- Implement circuit breaker pattern for reliability

## 📈 Future Enhancements

### Planned Features
- Multi-language support (English option)
- Custom prompt templates
- Advanced reasoning models
- Batch processing for multiple results
- Integration with other AI providers

### Analytics
- Track analysis quality metrics
- A/B testing different models
- User satisfaction scoring
- Performance benchmarking

## 🔗 Related Documentation

- [OpenRouter API Documentation](https://openrouter.ai/docs)
- [DeepSeek Model Information](https://platform.deepseek.com/api-docs)
- [Admin UI Component Guide](./ADMIN_UI_GUIDE.md)
- [Backend API Reference](./API_REFERENCE.md)

---

## 📞 Support

For issues or questions:
1. Check backend logs for error details
2. Verify environment configuration
3. Test with demo mode enabled
4. Review OpenRouter service status

**Demo Mode**: Set `VITE_DEMO_MODE=true` to test without OpenRouter API calls.