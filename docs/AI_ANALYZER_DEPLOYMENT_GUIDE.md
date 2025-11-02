# SDJ-7 AI Analyzer Deployment Guide

**Date:** November 2, 2025  
**Version:** 1.0  
**Status:** ✅ Ready for Deployment

---

## 🎯 Overview

The Admin AI Analyzer has been upgraded to work with **DeepSeek AI** and the **SDJ-7 patterns** framework, providing professional psychometric analysis of test results.

### Key Features
- ✅ **7-Pattern Analysis**: Comprehensive analysis across all SDJ patterns
- ✅ **Server-Side Security**: API key never exposed to frontend
- ✅ **Privacy-First**: No PII sent to AI service
- ✅ **Graceful Fallbacks**: Works even when AI service unavailable
- ✅ **Caching**: 24-hour cache reduces costs
- ✅ **Arabic-First**: Professional Arabic analysis with proper RTL support

---

## 📋 Pre-Deployment Checklist

### Backend Requirements
- [x] .NET 8 SDK installed
- [x] PostgreSQL database (Neon) accessible
- [x] SDJ scoring service functional
- [x] Admin authentication working

### Frontend Requirements
- [x] Node.js 18+ and npm installed
- [x] Admin UI built and deployable
- [x] API base URL configured

### API Key
- [ ] **DeepSeek API key obtained** (sk-9c6a11074bac4e598ffef48e9bad9380)
- [ ] Environment variable configured

---

## 🔧 Configuration Steps

### Step 1: Configure DeepSeek API Key

#### For Local Development:
```powershell
# Windows PowerShell
$env:DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"

# Verify
echo $env:DEEPSEEK_API_KEY
```

#### For Render.com Deployment:
1. Go to your Render service dashboard
2. Navigate to **Environment** tab
3. Add environment variable:
   - **Key**: `DEEPSEEK_API_KEY`
   - **Value**: `sk-9c6a11074bac4e598ffef48e9bad9380`
4. Click **Save Changes**
5. Render will automatically redeploy

#### For Docker:
```yaml
# docker-compose.yml
services:
  backend:
    environment:
      - DEEPSEEK_API_KEY=sk-9c6a11074bac4e598ffef48e9bad9380
```

### Step 2: Verify Backend Configuration

The backend automatically reads the API key from the environment. Check `Program.cs`:

```csharp
// DeepSeek configuration (lines 272-298 in Program.cs)
builder.Services.Configure<PsyApi.Services.AI.DeepSeekConfiguration>(config =>
{
    config.ApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY") ?? string.Empty;
    config.Model = "deepseek-chat";
    config.MaxTokens = 1500;
    config.Temperature = 0.2;
    config.BaseUrl = "https://api.deepseek.com/v1";
    config.TimeoutSeconds = 30;
    config.MaxRetries = 3;
    config.CacheDurationHours = 24;
});
```

### Step 3: Frontend Configuration

Ensure the Admin UI points to the correct backend:

```typescript
// frontend/admin-ui/.env
VITE_API_BASE_URL=https://your-backend.onrender.com/api
```

---

## 🚀 Deployment Process

### Option 1: Deploy to Render.com (Recommended)

#### Backend:
```powershell
# From project root
cd backend/PsyApi

# Build
dotnet build --configuration Release

# Push to Git
git add .
git commit -m "feat(ai-analyzer): Add SDJ-7 AI analyzer with DeepSeek integration"
git push origin develop

# Render auto-deploys on push if connected
```

#### Frontend:
```powershell
# From project root
cd frontend/admin-ui

# Build
npm run build

# Deploy to Vercel/Render
vercel --prod
# OR
# Push to git (if Render auto-deploy configured)
```

### Option 2: Local Testing

#### Terminal 1 - Backend:
```powershell
cd backend/PsyApi
$env:DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

#### Terminal 2 - Frontend:
```powershell
cd frontend/admin-ui
npm run dev
```

Access: `http://localhost:5173`

---

## 🧪 Testing the AI Analyzer

### Test 1: Health Check
```powershell
# Check AI service health
curl https://your-backend.onrender.com/api/admin/ai/health `
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

**Expected Response:**
```json
{
  "enabled": true,
  "provider": "DeepSeek",
  "model": "deepseek-chat",
  "hasKey": true,
  "status": "ready",
  "message": "خدمة الذكاء الاصطناعي جاهزة"
}
```

### Test 2: Analyze SDJ Result
```powershell
# Analyze a result (replace {resultId} with actual ID)
curl https://your-backend.onrender.com/api/admin/ai/analyze `
  -X POST `
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" `
  -H "Content-Type: application/json" `
  -d '{"resultId": 123}'
```

**Expected Response Structure:**
```json
{
  "summary": "النتائج تظهر أداءً جيداً...",
  "patterns": [
    {
      "key": "personality_patterns",
      "label": "الأنماط الشخصية",
      "tScore": 58.3,
      "band": "قوي",
      "insights": ["...", "..."],
      "risks": ["..."],
      "recommendations": ["...", "..."]
    }
    // ... 6 more patterns
  ],
  "subDimensions": [...],
  "charts": {
    "radar": {...},
    "bars": {...}
  },
  "model": "deepseek-chat",
  "usage": {
    "promptTokens": 450,
    "completionTokens": 320,
    "totalTokens": 770,
    "latencyMs": 1800
  },
  "generatedAt": "2025-11-02T10:30:00Z"
}
```

### Test 3: UI Integration
1. Login to Admin UI
2. Navigate to any SDJ result (120-question test)
3. Scroll to "التحليل الذكي المتقدم (SDJ-7)" section
4. Click "تشغيل التحليل" if not auto-generated
5. Verify:
   - ✓ Summary displays in Arabic
   - ✓ All 7 pattern cards appear
   - ✓ T-scores and bands are correct
   - ✓ Visual chart shows pattern bars
   - ✓ Model info and latency displayed

---

## 📊 Monitoring & Maintenance

### Logs to Monitor
```powershell
# Backend logs (Render)
# Check for these log patterns:
[SDJ-AI] Success resultId=123 latencyMs=1800 tokens=770
[SDJ-AI] Failed resultId=123 error=...
[SDJ-AI] Cache hit for resultId=123
```

### Cost Estimation (DeepSeek Pricing)
- **Input**: $0.14 per million tokens
- **Output**: $0.28 per million tokens
- **Typical Request**: ~450 input + 320 output tokens
- **Cost per Analysis**: ~$0.00015 (less than $0.001)
- **With 24h Cache**: ~$0.05 per 1000 unique analyses

### Cache Behavior
- **Duration**: 24 hours per resultId
- **Key Format**: `sdj7_ai_{resultId}_{patternsHash}`
- **Invalidation**: Automatic after 24h or server restart

---

## 🔒 Security Best Practices

### ✅ Implemented
1. **API Key Security**
   - Stored in environment variables only
   - Never logged in full (redacted to first 10 + last 4 chars)
   - Never sent to frontend

2. **PII Protection**
   - Only anonymized data sent to AI
   - No names, IDs, or personal info in prompts
   - Result IDs used only for caching

3. **Rate Limiting**
   - Admin endpoints protected with `[EnableRateLimiting("admin")]`
   - Retry logic with exponential backoff

4. **Input Validation**
   - Only SDJ results accepted
   - Item count limited to 120 max
   - JSON schema strictly validated

### 🚨 Never Do
- ❌ Commit API keys to Git
- ❌ Log full API responses (may contain sensitive analysis)
- ❌ Send PII (names, IDs, etc.) to AI service
- ❌ Expose AI endpoints without authentication

---

## 🐛 Troubleshooting

### Issue: "خدمة الذكاء الاصطناعي غير متوفرة"

**Cause**: API key not configured or invalid

**Solution**:
```powershell
# Check if key is set
echo $env:DEEPSEEK_API_KEY

# Set the key
$env:DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"

# Restart backend
dotnet run
```

### Issue: "هذه النتيجة ليست من نوع SDJ"

**Cause**: Trying to analyze a legacy 200-question test

**Solution**: Only SDJ results (120 questions) are supported. Check result type before analyzing.

### Issue: Timeout after 15-30 seconds

**Cause**: DeepSeek API slow or unavailable

**Solution**:
1. Check DeepSeek API status
2. System automatically falls back to rule-based analysis
3. User sees: "تحليل احتياطي" badge

### Issue: "Failed to parse JSON from AI response"

**Cause**: DeepSeek returned non-JSON or malformed JSON

**Solution**:
1. Check backend logs for raw response
2. System retries with exponential backoff (3 attempts)
3. If persistent, check DeepSeek API status

---

## 📈 Performance Optimization

### Current Configuration
- **Timeout**: 30 seconds
- **Max Retries**: 3
- **Cache Duration**: 24 hours
- **Max Tokens**: 1500
- **Temperature**: 0.2 (more deterministic)

### Tuning Options
```csharp
// In Program.cs, adjust these:
config.MaxTokens = 2000;        // Longer responses
config.Temperature = 0.1;       // More consistent
config.CacheDurationHours = 48; // Longer cache
config.TimeoutSeconds = 45;     // For slow networks
```

---

## 🔄 Upgrade Path

### Future Enhancements
1. **Multi-Language Support**: Add English analysis option
2. **Custom Prompts**: Admin-configurable analysis style
3. **Comparison Mode**: Compare multiple results
4. **PDF Export**: Export analysis to PDF report
5. **Charts Library**: Integrate Recharts/Chart.js for better visuals

### Migration Notes
- Current response schema is forward-compatible
- Add new fields to DTOs without breaking existing consumers
- Frontend gracefully handles missing fields

---

## 📞 Support & Resources

### Documentation
- API Reference: `/docs/API.md`
- SDJ Scoring: `/docs/SDJ_SCORING.md`
- Architecture: `/docs/ARCHITECTURE.md`

### External Links
- DeepSeek API Docs: https://platform.deepseek.com/api-docs/
- DeepSeek Pricing: https://platform.deepseek.com/pricing
- Render Docs: https://render.com/docs

---

## ✅ Deployment Sign-Off

### Pre-Production Checklist
- [ ] API key configured in production environment
- [ ] Backend deployed and healthy
- [ ] Frontend deployed and connected
- [ ] Health check returns `"enabled": true`
- [ ] Test analysis on sample SDJ result succeeds
- [ ] Admin can view analysis in UI
- [ ] Logs show successful analyses
- [ ] No errors in browser console
- [ ] Cache working (second request faster)

### Post-Deployment Monitoring (First 48h)
- [ ] Monitor backend logs for errors
- [ ] Track API usage and costs
- [ ] Collect user feedback
- [ ] Check latency metrics
- [ ] Verify cache hit rate

---

**Deployment Date:** _____________  
**Deployed By:** _____________  
**Production URL:** _____________  
**Status:** ✅ Ready / ⏳ Pending / ❌ Issues

---

**End of Guide**
