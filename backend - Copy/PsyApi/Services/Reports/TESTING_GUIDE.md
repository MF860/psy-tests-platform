# 🧪 دليل الاختبار السريع - Testing Guide

## اختبار نظام التقارير النفسية

---

## 🚀 **اختبار سريع (5 دقائق)**

### 1. تشغيل المشروع

```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi
dotnet run
```

انتظر حتى ترى:
```
Now listening on: https://localhost:5001
```

---

### 2. الحصول على Token (إذا لزم الأمر)

```powershell
# تسجيل الدخول
$loginResponse = Invoke-RestMethod -Uri "https://localhost:5001/api/auth/login" `
    -Method POST `
    -ContentType "application/json" `
    -Body '{"nationalId": "1234567890", "password": "yourpassword"}' `
    -SkipCertificateCheck

$token = $loginResponse.token
```

---

### 3. الحصول على قائمة النتائج

```powershell
# قائمة النتائج المتاحة
$results = Invoke-RestMethod -Uri "https://localhost:5001/api/results" `
    -Headers @{ Authorization = "Bearer $token" } `
    -SkipCertificateCheck

# عرض أول 3 نتائج
$results | Select-Object -First 3 | Format-Table ResultId, NationalId, FullName, TotalScore
```

---

### 4. توليد التقرير

```powershell
# اختر ResultId من القائمة السابقة
$resultId = 1  # غيّر هذا الرقم

# توليد وتحميل PDF
Invoke-RestMethod -Uri "https://localhost:5001/api/results/$resultId/pdf" `
    -Headers @{ Authorization = "Bearer $token" } `
    -OutFile "test_report.pdf" `
    -SkipCertificateCheck

# فتح الملف
start test_report.pdf
```

---

## ✅ **ماذا تتوقع أن ترى**

### في الـLogs (Console)

```
═══════════════════════════════════════════
🎯 ULTIMATE ARABIC PSYCHOMETRIC REPORT
═══════════════════════════════════════════
👤 User: أحمد محمد (ID: 1234567890)
📋 Session: abc-123-def-456
🕐 Time: 2025-10-14 15:30:45

📊 STATISTICS:
   Dimensions: 15
   Excellent (≥65): 3
   Good (55-64): 5
   Average (40-54): 5
   Weak (<40): 2
   Avg T-Score: 54.7
   Avg Percentile: 58
   Total Score: 821

[UltimatePdfService] ✓ Noto Naskh Arabic Regular loaded
[UltimatePdfService] ✓ Noto Naskh Arabic Bold loaded
[UltimatePdfService] ℹ Logo not found (optional)
[RadarChartRenderer] ✓ Arabic font loaded
[BarChartRenderer] ✓ Arabic font loaded
[DonutChartRenderer] ✓ Arabic font loaded

───────────────────────────────────────────
✅ PDF GENERATION COMPLETE
───────────────────────────────────────────
📄 Pages: 5
📏 Size: 245.3 KB
⏱ Duration: 1247ms
🎯 Font: Noto Naskh Arabic with HarfBuzz
📊 Charts: Radar + Bar + Donut (100% Vector)
🔢 Numbers: Western digits (no � glyphs)
═══════════════════════════════════════════
```

---

### في الـPDF

#### **صفحة 1: الغلاف** ✓
- [ ] الشعار يظهر في الأعلى (إن وُجد)
- [ ] "منصة التحليل النفسي المتقدم" واضح
- [ ] معلومات المستخدم (الاسم، الرقم الوطني، الجلسة، التاريخ)
- [ ] KPIs في صناديق ملونة
- [ ] أقوى 3 أبعاد (chips خضراء)
- [ ] أضعف 3 أبعاد (chips ملونة)

#### **صفحة 2: الرسوم** ✓
- [ ] Radar Chart واضح ومتوازن
- [ ] Bar Chart مرتب تصاعدياً
- [ ] Donut Chart يظهر المحاور الأربعة
- [ ] الألوان صحيحة (أخضر/برتقالي/أحمر)
- [ ] النصوص عربية صحيحة

#### **صفحة 3: التحليل** ✓
- [ ] رؤى عامة مفيدة
- [ ] تحليل كل محور (COG/EMO/SOC/ORG)
- [ ] مؤشرات المخاطرة (إن وجدت)
- [ ] النصوص عربية غنية

#### **صفحة 4: خطة التطوير** ✓
- [ ] 5-7 إجراءات عملية
- [ ] كل إجراء له: عنوان، أولوية، وصف، KPI، إطار زمني
- [ ] نصائح للنجاح
- [ ] كل شيء بالعربية

#### **صفحة 5: الدورات (اختيارية)** ✓
- [ ] 3-6 دورات مقترحة
- [ ] كل دورة لها: اسم، مدة، وصف، أبعاد مستهدفة
- [ ] الدورات مرتبطة بالأبعاد الضعيفة

---

## ❌ **ماذا لا يجب أن ترى**

### مشاكل شائعة

#### 1. رموز �
```
❌ T=٥٤.٧ أو T=�
✅ T=54.7
```
**إذا ظهرت**: مشكلة في `FormatNum()` - تحقق من الـLogs

#### 2. خطوط مكسورة
```
❌ نص عربي يظهر كمربعات □□□
✅ نص عربي واضح: التحليل النفسي
```
**إذا ظهرت**: الخطوط لم تُحمّل - تحقق من `Resources/Fonts/`

#### 3. رسوم ناقصة
```
❌ صفحة 2 فارغة أو بدون رسوم
✅ 3 رسوم واضحة
```
**إذا ظهرت**: exception في Renderers - راجع الـLogs

#### 4. نصوص LTR بدل RTL
```
❌ النص يبدأ من اليسار (خطأ)
✅ النص يبدأ من اليمين (صحيح)
```
**إذا ظهرت**: مشكلة في RTL - تحقق من `ArabicTextStyle`

---

## 🔍 **اختبارات متقدمة**

### اختبار 1: عدد أبعاد مختلف

```powershell
# اختبر مع نتائج لها أعداد مختلفة من الأبعاد
# 5 أبعاد، 15 بُعد، 27 بُعد
```

**المتوقع**: يجب أن يعمل بسلاسة مع جميع الأعداد

---

### اختبار 2: قيم T متطرفة

```sql
-- ابحث عن نتائج بقيم متطرفة
SELECT * FROM Results 
WHERE DimensionScoresJson LIKE '%"T":85%' 
   OR DimensionScoresJson LIKE '%"T":15%'
```

**المتوقع**: يجب أن تُعرض مؤشرات مخاطرة

---

### اختبار 3: بدون شعار

```powershell
# احذف/أعد تسمية الشعار مؤقتاً
Rename-Item "Resources\Brand\SITES-ICON.png" "SITES-ICON.png.bak"

# ولّد التقرير
# يجب أن يعمل بدون مشاكل

# أعد الشعار
Rename-Item "Resources\Brand\SITES-ICON.png.bak" "SITES-ICON.png"
```

**المتوقع**: Log يقول `ℹ Logo not found (optional)` والتقرير يعمل

---

### اختبار 4: Performance Test

```powershell
# قِس الوقت
Measure-Command {
    Invoke-RestMethod -Uri "https://localhost:5001/api/results/1/pdf" `
        -Headers @{ Authorization = "Bearer $token" } `
        -OutFile "test_report.pdf" `
        -SkipCertificateCheck
}
```

**المتوقع**: 
- 15 بُعد: ~1-2 ثانية
- 27 بُعد: ~2-3 ثانية

---

## 📊 **قياس الجودة**

### Checklist للتقرير المثالي

#### التصميم
- [ ] الهوامش متساوية (16mm)
- [ ] الفراغات متناسقة
- [ ] الألوان واضحة ومريحة
- [ ] الخطوط واضحة وسهلة القراءة

#### المحتوى
- [ ] جميع البيانات صحيحة
- [ ] الحسابات دقيقة (T-scores, Percentiles)
- [ ] التحليل منطقي ومفيد
- [ ] التوصيات عملية وقابلة للتطبيق

#### التقنية
- [ ] لا أخطاء في Console
- [ ] الملف < 500KB لـ15 بُعد
- [ ] الوقت < 2 ثانية
- [ ] يعمل على Windows/Mac/Linux

---

## 🐛 **إذا فشل الاختبار**

### خطوات التشخيص

1. **تحقق من الـLogs**
```powershell
# ابحث عن أخطاء
Select-String -Path "logs\*.log" -Pattern "ERROR|Exception"
```

2. **تحقق من الخطوط**
```powershell
dir backend\PsyApi\Resources\Fonts\
# يجب أن ترى: NotoNaskhArabic-Regular.ttf و Bold.ttf
```

3. **تحقق من البيانات**
```csharp
// في ResultsController، أضف logging
_logger.LogInformation($"Dimensions count: {dimensions.Count}");
foreach (var d in dimensions)
{
    _logger.LogInformation($"{d.Dimension}: T={d.T}, P={d.Percentile}");
}
```

4. **اتصل بالدعم**
راجع `DEPLOYMENT_GUIDE.md` قسم Troubleshooting

---

## ✅ **الخلاصة**

إذا نجحت جميع الاختبارات:
- ✅ النظام جاهز للإنتاج
- ✅ يمكن استخدامه بثقة
- ✅ جودة احترافية

إذا فشل أي اختبار:
- 🔧 راجع `DEPLOYMENT_GUIDE.md`
- 📖 راجع `README_ULTIMATE.md`
- 🐛 افحص الـLogs بعناية

---

**نصيحة أخيرة**: اختبر مع **بيانات حقيقية متنوعة** قبل النشر للإنتاج!

**Happy Testing!** 🎉
