# إعداد تحليل الذكاء الاصطناعي (DeepSeek) - دليل سريع

## الخطوة 1: إعداد مفتاح API

### في Windows (PowerShell)

```powershell
# ضبط المفتاح للجلسة الحالية فقط
$env:DEEPSEEK_API_KEY = "sk-9c6a11074bac4e598ffef48e9bad9380"

# أو ضبط المفتاح بشكل دائم (يتطلب إعادة تشغيل)
setx DEEPSEEK_API_KEY "sk-9c6a11074bac4e598ffef48e9bad9380"
```

### في Linux/macOS

```bash
# ضبط المفتاح للجلسة الحالية
export DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"

# أو إضافته لملف ~/.bashrc أو ~/.zshrc للإعداد الدائم
echo 'export DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"' >> ~/.bashrc
source ~/.bashrc
```

## الخطوة 2: إعادة تشغيل Backend

```powershell
cd backend\PsyApi
dotnet run
```

**يجب أن ترى في السجلات:**
```
DeepSeek AI configured - Model: deepseek-chat, MaxTokens: 1500, Temperature: 0.2, API Key: ✓ Set
```

## الخطوة 3: اختبار التحليل

1. افتح واجهة الإدارة: `http://localhost:5173`
2. سجّل دخول كمدير
3. افتح أي نتيجة SDJ
4. انتقل إلى قسم "التحليل الذكي بالذكاء الاصطناعي"
5. انقر "تشغيل التحليل"

**النتيجة المتوقعة:**
- ملخص بالعربية
- نقاط القوة (3-6)
- مجالات التطوير (3-6)
- التوصيات (3-6)
- جدول الأبعاد السبعة مع T-Scores

## الخطوة 4: التحقق من الأمان

✅ **ما يجب أن يحدث:**
- المفتاح موجود في البيئة فقط
- لا يظهر المفتاح في الـ logs (مُخفى)
- لا يظهر المفتاح في Network tab بالمتصفح
- النتائج تُحفظ في الذاكرة (cache) لمدة 24 ساعة

❌ **ما يجب تجنبه:**
- لا تضع المفتاح في appsettings.json
- لا تُضف المفتاح للـ Git
- لا تطبع المفتاح في الكود

## استكشاف الأخطاء

### الخطأ: "خدمة الذكاء الاصطناعي غير متوفرة حالياً"

**السبب:** المفتاح غير مضبوط أو خاطئ

**الحل:**
1. تأكد من ضبط المفتاح: `echo $env:DEEPSEEK_API_KEY` (Windows) أو `echo $DEEPSEEK_API_KEY` (Linux)
2. أعد تشغيل الـ backend
3. راجع السجلات للتأكد من "API Key: ✓ Set"

### الخطأ: يظهر "تحليل احتياطي" (Fallback)

**السبب:** فشل استدعاء DeepSeek API

**الحل:**
1. تحقق من الاتصال بالإنترنت
2. تحقق من صلاحية المفتاح
3. راجع السجلات (logs) للأخطاء التفصيلية

### الخطأ: لا يظهر جدول الأبعاد

**السبب:** النتيجة ليست من نوع SDJ

**الحل:** تأكد أن النتيجة من جلسة SDJ (120 سؤال)

## الأداء المتوقع

- **زمن التحليل:** 2-4 ثوانٍ (أول مرة)
- **زمن التحليل:** <100 ميلي ثانية (من الذاكرة - cache)
- **معدل النجاح:** 90%+ (مع مفتاح صالح)
- **التكلفة:** ~0.001$ لكل تحليل (500-1500 token)

## التحليل الاحتياطي (Fallback)

إذا لم يكن المفتاح مضبوطاً، يستخدم النظام تحليلاً قائماً على القواعد:

- نقاط القوة: T-Score ≥ 55
- مجالات التطوير: T-Score < 45
- توصيات عامة قابلة للتنفيذ
- ملاحظات تلقائية لكل بُعد

**مثال:**
```
T ≥ 65 → "أداء استثنائي - استثمر في هذا المجال"
55 ≤ T < 65 → "أداء جيد جداً - حافظ على هذا المستوى"
45 ≤ T < 55 → "أداء متوسط - مجال للتحسين التدريجي"
```

## للإنتاج (Production)

### Azure App Service

```bash
az webapp config appsettings set \
  --resource-group myResourceGroup \
  --name myAppName \
  --settings DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380"
```

### Docker

```dockerfile
# في Dockerfile أو docker-compose.yml
environment:
  - DEEPSEEK_API_KEY=${DEEPSEEK_API_KEY}
```

ثم:
```bash
docker run -e DEEPSEEK_API_KEY="sk-9c6a11074bac4e598ffef48e9bad9380" ...
```

### Kubernetes

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: deepseek-secret
type: Opaque
stringData:
  api-key: sk-9c6a11074bac4e598ffef48e9bad9380
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: api
        env:
        - name: DEEPSEEK_API_KEY
          valueFrom:
            secretKeyRef:
              name: deepseek-secret
              key: api-key
```

## دعم فني

للمزيد من التفاصيل، راجع:
- `docs/AI_ANALYZER_SDJ.md` - توثيق تقني كامل
- `README.md` - دليل النظام الشامل

---

**تم التحديث:** 2025-10-26  
**الإصدار:** 1.0
