# دليل نشر SDJ V2 الكامل - خطوة بخطوة

## 🎯 الهدف
نشر SDJ V2 Seven Patterns على Production (Render + Neon + Vercel)

---

## ✅ الخطوة 1: التحقق من Git Push (تم ✓)

```bash
✓ Commit f515bbb تم دفعه إلى GitHub
✓ Commit 322bda9 (deployment trigger) تم دفعه
```

---

## ⚠️ الخطوة 2: مسح قاعدة البيانات (Neon PostgreSQL)

**السبب**: قاعدة البيانات الحالية تحتوي على بيانات قديمة (V1 أو Legacy)، يجب مسحها لتطبيق Migration الجديد.

### 2.1 افتح Neon SQL Editor

1. اذهب إلى: https://console.neon.tech
2. اختر المشروع: `psy-tests-platform`
3. اختر Database: `psy_tests`
4. افتح SQL Editor

### 2.2 شغل سكربت المسح

انسخ والصق الكود التالي في SQL Editor:

```sql
-- SDJ V2 Database Reset
-- IMPORTANT: This will delete ALL existing data!

-- Step 1: Clear all data
TRUNCATE TABLE "SessionItems" CASCADE;
TRUNCATE TABLE "Results" CASCADE;
TRUNCATE TABLE "Sessions" CASCADE;
TRUNCATE TABLE "Items" CASCADE;

-- Step 2: Remove SDJ migrations (force re-migration)
DELETE FROM "__EFMigrationsHistory" 
WHERE "MigrationId" LIKE '%SDJ%' 
   OR "MigrationId" >= '20251029000000';

-- Step 3: Verify (should all show 0)
SELECT 'Items' as TableName, COUNT(*) as RowCount FROM "Items"
UNION ALL
SELECT 'Sessions', COUNT(*) FROM "Sessions"
UNION ALL
SELECT 'SessionItems', COUNT(*) FROM "SessionItems"
UNION ALL
SELECT 'Results', COUNT(*) FROM "Results";
```

### 2.3 تحقق من النتيجة

يجب أن ترى:
```
TableName     | RowCount
--------------|---------
Items         | 0
Sessions      | 0
SessionItems  | 0
Results       | 0
```

✅ إذا رأيت هذه النتيجة، قاعدة البيانات جاهزة!

---

## 🚀 الخطوة 3: إعادة بناء Render (Force Rebuild)

### 3.1 اذهب إلى Render Dashboard

1. افتح: https://dashboard.render.com
2. اختر Service: `psy-tests-backend`

### 3.2 شغل Manual Deploy

1. اضغط على زر **"Manual Deploy"** في الأعلى
2. اختر **"Deploy latest commit"**
3. تأكد من Branch: **develop**
4. اضغط **"Deploy"**

### 3.3 راقب اللوجات (Logs)

انتظر 5-10 دقائق وراقب اللوجات. يجب أن ترى:

```
✓ Build started...
✓ Restoring packages...
✓ Building application...
✓ Applying migration '20251029143657_SDJ_V2_SevenPatterns'
✓ Executed DbCommand: ALTER TABLE "Items" ADD "PatternId" TEXT
✓ Executed DbCommand: ALTER TABLE "Items" ADD "PatternKey" TEXT
✓ Executed DbCommand: ALTER TABLE "Items" ADD "PatternNameAr" TEXT
✓ Executed DbCommand: ALTER TABLE "Items" ADD "SubId" TEXT
✓ Executed DbCommand: ALTER TABLE "Items" ADD "SubKey" TEXT
✓ Executed DbCommand: ALTER TABLE "Items" ADD "SubNameAr" TEXT
✓ Database migrations completed successfully
✓ [SDJ V2] Using CSV file: questions_sdj_v2_ar.csv
✓ CSV total lines (incl header): 211
✓ Total rows parsed (excluding header): 210
✓ Post-seed verification passed: 210 items
✓ [SDJ V2] Seven Patterns mode detected
✓ [SDJ V2] Pattern distribution:
    P1: 30 items (الأنماط الشخصية)
    P2: 30 items (القدرات المعرفية والعقلية)
    P3: 30 items (الأنماط النفسية)
    P4: 30 items (الأنماط السلوكية)
    P5: 30 items (الأنماط العددية والمنطقية)
    P6: 30 items (الأنماط القيادية والتنظيمية)
    P7: 30 items (الاستعدادات المهنية العامة)
✓ Deploy succeeded!
```

---

## 🧪 الخطوة 4: اختبار التطبيق

### 4.1 اختبار API مباشرة

افتح PowerShell وشغل:

```powershell
# Test 1: Health Check
$response = Invoke-RestMethod -Uri "https://psy-tests-backend.onrender.com/api/health"
$response | ConvertTo-Json

# يجب أن ترى:
# {
#   "status": "healthy",
#   "database": "connected"
# }

# Test 2: Create Test Session
$session = Invoke-RestMethod -Uri "https://psy-tests-backend.onrender.com/api/sessions" `
  -Method POST -Body '{"examId":1,"userId":"1000000001"}' `
  -ContentType "application/json"

# تحقق من عدد الأسئلة
$session.sessionItems.Count
# يجب أن يكون: 210

# تحقق من البنية الجديدة
$session.sessionItems[0].item | Select-Object itemCode, patternId, patternNameAr, subId
# يجب أن ترى: PatternId, PatternNameAr, SubId (العمود الجديدة)
```

### 4.2 اختبار من واجهة المستخدم

1. افتح: https://psy-tests-user.vercel.app
2. سجل دخول أو أنشئ حساب جديد
3. ابدأ اختبار SDJ جديد
4. **تحقق**: يجب أن ترى **210 سؤال** (وليس 125 أو 200)
5. بعض الأسئلة يجب أن تكون **Multiple Choice** (A, B, C, D)
6. **أكمل الاختبار**

### 4.3 تحقق من النتائج - Admin UI

1. افتح: https://psy-tests-admin.vercel.app
2. اذهب إلى Results
3. افتح النتيجة الجديدة
4. **تحقق**:
   - ✓ Badge "SDJ v2.1" ظاهر
   - ✓ **7 أنماط** في الرسم البياني (وليس 5)
   - ✓ Pattern Cards تظهر 3 subdimensions لكل pattern
   - ✓ الأسماء بالعربية صحيحة

### 4.4 تحقق من تقرير PDF

1. من صفحة النتيجة في Admin UI
2. اضغط "Download PDF"
3. افتح الملف
4. **تحقق**:
   - ✓ شعار STEST.png في الأعلى
   - ✓ العنوان: "التقرير النفسي الشامل — نتائج القياس والتحليل"
   - ✓ Horizontal Bar Chart للأبعاد الفرعية
   - ✓ Radar Chart السباعي للأنماط الرئيسية
   - ✓ ملخص الأنماط السبعة موجود
   - ✓ الدورات المقترحة لكل مسار (7 مسارات)
   - ✓ الجملة الختامية: "ابدأ رحلتك التدريبية المخصصة عبر منصة استدامة"

---

## ❌ استكشاف الأخطاء

### مشكلة 1: ما زلت أرى 125 سؤال

**السبب**: Migration لم يتم تطبيقه أو DataSeeder ما زال يستخدم V1

**الحل**:
1. تحقق من Render Logs
2. ابحث عن: `[SDJ V2] Using CSV file: questions_sdj_v2_ar.csv`
3. إذا لم تجدها، امسح قاعدة البيانات وأعد Deploy

### مشكلة 2: Database Connection Error

**السبب**: Connection string خاطئ في Render

**الحل**:
1. اذهب إلى Render Settings → Environment
2. تحقق من: `DATABASE_URL` (يجب أن يشير إلى Neon)
3. Format: `postgresql://user:pass@ep-xxx.us-east-1.aws.neon.tech/psy_tests?sslmode=require`

### مشكلة 3: PDF لا يظهر الأنماط السبعة

**السبب**: Result لم يُحسب بـ SdjV2ScoringService

**الحل**:
1. أنشئ اختبار جديد (الاختبارات القديمة ستبقى V1)
2. تحقق من أن الأسئلة 210 (وليس 125)
3. أكمل الاختبار وتحقق من النتيجة

---

## ✅ معايير النجاح

يعتبر النشر ناجحاً إذا:

- [x] Render Build نجح بدون أخطاء
- [x] Migration `20251029143657_SDJ_V2_SevenPatterns` تم تطبيقه
- [x] 210 سؤال تم seed-ها في قاعدة البيانات
- [x] API يُرجع items مع `PatternId` و `SubId`
- [x] واجهة المستخدم تعرض 210 سؤال
- [x] Admin UI تعرض 7 أنماط
- [x] PDF يحتوي على Radar Chart سباعي

---

## 📞 الدعم

إذا واجهت مشاكل:

1. **تحقق من Render Logs أولاً**
2. **شغل سكربت التحقق**:
   ```bash
   cd scripts
   ./verify_sdj_v2_production.ps1
   ```
3. **افحص Neon SQL Editor**:
   ```sql
   SELECT COUNT(*) FROM "Items";
   -- يجب أن يكون: 210
   
   SELECT DISTINCT "PatternId", "PatternNameAr" FROM "Items" WHERE "PatternId" IS NOT NULL;
   -- يجب أن يكون: 7 rows
   ```

---

## 🎉 بعد النجاح

بعد التأكد من نجاح النشر:

1. **احذف ملف الـ marker**:
   ```bash
   git rm DEPLOY_SDJ_V2.txt
   git commit -m "chore: remove deployment marker"
   git push origin develop
   ```

2. **وثق النشر**:
   - تاريخ النشر
   - Commit SHA: f515bbb
   - عدد الأسئلة: 210
   - الأنماط: 7

3. **شارك مع الفريق** ✅

---

**تم إنشاؤه**: October 29, 2025  
**الإصدار**: SDJ V2 Seven Patterns  
**الحالة**: Ready for Production
