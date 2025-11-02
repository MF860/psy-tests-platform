# SDJ-7 AI Analyzer - Quick Start

**Status:** ✅ Ready for Testing  
**Last Updated:** November 2, 2025

---

## ⚡ Quick Setup (5 Minutes)

### 1. Set API Key
```powershell
$env:DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"
```

### 2. Start Backend
```powershell
cd "c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi"
dotnet run
```

### 3. Start Frontend
```powershell
cd "c:\Users\ASUS\Desktop\saitest\psy-tests-platform\frontend\admin-ui"
npm run dev
```

### 4. Test It
- Navigate to Admin UI: http://localhost:5173
- Login as admin
- Open any SDJ result (120 questions)
- Scroll to "التحليل الذكي المتقدم (SDJ-7)"
- Click "تشغيل التحليل"
- Wait 15-30 seconds
- Verify 7-pattern cards appear

---

## 🔍 Health Check API

```powershell
# Check if AI service is ready
Invoke-RestMethod -Uri "http://localhost:5000/api/admin/ai/health" `
  -Headers @{ Authorization = "Bearer YOUR_ADMIN_TOKEN" }
```

**Expected:**
```json
{
  "enabled": true,
  "provider": "DeepSeek",
  "model": "deepseek-chat",
  "hasKey": true,
  "status": "ready"
}
```

---

## 🎯 API Endpoint

**POST** `/api/admin/ai/analyze`

```json
{
  "resultId": 123
}
```

**Response (200 OK):**
```json
{
  "summary": "النتائج تظهر...",
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
  "charts": { "radar": {...}, "bars": {...} },
  "model": "deepseek-chat",
  "usage": { "totalTokens": 770, "latencyMs": 1800 },
  "generatedAt": "2025-11-02T..."
}
```

---

## 📊 The 7 SDJ Patterns

1. **personality_patterns** - الأنماط الشخصية
2. **cognitive_mental** - الإدراك العقلي
3. **psychological_patterns** - الأنماط النفسية
4. **behavioral_patterns** - الأنماط السلوكية
5. **numerical_logical** - المنطق العددي
6. **leadership_organizational** - القيادة التنظيمية
7. **professional_readiness** - الاستعداد المهني

---

## 🛠️ Troubleshooting

### Issue: "خدمة الذكاء الاصطناعي غير متوفرة"
**Fix:** API key not set. Run:
```powershell
$env:DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"
```

### Issue: "Failed to parse JSON"
**Fix:** DeepSeek API issue. Check logs for raw response. System will retry 3x.

### Issue: Timeout after 30s
**Fix:** Normal for first request. DeepSeek can be slow. Try again (cache will speed it up).

### Issue: "هذه النتيجة ليست من نوع SDJ"
**Fix:** Only SDJ (120-question) results are supported. Legacy 200-question tests won't work.

---

## 📁 Key Files

- **Backend Service**: `backend/PsyApi/Services/AI/SdjDeepSeekService.cs`
- **Controller**: `backend/PsyApi/Controllers/AdminAiController.cs`
- **DTOs**: `backend/PsyApi/Models/SdjAiAnalysisDtos.cs`
- **Frontend**: `frontend/admin-ui/src/pages/ResultDetail.tsx`
- **API Contract**: `frontend/admin-ui/src/lib/adminContract.ts`
- **API Client**: `frontend/admin-ui/src/lib/apiAdmin.ts`

---

## 🚀 Production Deployment

**Render (Backend):**
1. Set environment variable: `DEEPSEEK_API_KEY=sk-9c6a11074bac4e598ffef48e9bad9380`
2. Push code
3. Auto-deploys

**Vercel (Frontend):**
1. Set `VITE_API_BASE_URL` to backend URL
2. `vercel --prod`

See: `AI_ANALYZER_DEPLOYMENT_GUIDE.md` for details

---

## 💰 Cost Estimate

- **Per Analysis**: ~$0.00015 (770 tokens avg)
- **With 24h Cache**: ~$0.05 per 1000 unique analyses
- **Monthly (1000 analyses)**: ~$0.05

DeepSeek is **100x cheaper** than GPT-4!

---

## ✅ Build Status

```
Backend: ✅ Build succeeded (0 errors, 6 warnings)
Frontend: ✅ Compilation clean (0 errors)
API Key: ✅ Configured locally
Tests: ⏳ Ready for E2E testing
```

---

**Next Step:** Run end-to-end test with real SDJ result and document evidence.

See: `AI_ANALYZER_DEPLOYMENT_GUIDE.md` for full instructions.
