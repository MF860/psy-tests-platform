# 🎨 Visual Redesign & Modern Component Mockups

## Overview

This document provides comprehensive textual descriptions of modern, professional UI mockups for the Arabic Psychometric Testing Platform. These mockups incorporate the new design system (colors, typography, spacing) and demonstrate how the platform should look after Phase 5 implementation.

---

## Section 1: Login & Onboarding Flow

### 1.1 Modern Login Page

**Current State:**  
Plain white background, basic form, minimal branding

**Proposed Design:**

```
╔══════════════════════════════════════════════════════════════╗
║                    PSYCHOMETRIC PLATFORM                     ║  Header: 28pt Bold, Primary Blue
║              نظام التقييم النفسي الحديث                      ║  Subheader: 16pt Medium, Gray-600
║                                                              ║
║ ┌────────────────────────────────────────────────────────┐   ║  Main Card: White bg, 12px shadow
║ │                                                        │   ║  Padding: 32px (2xl)
║ │  ┌──────────────────────────────────────────────────┐  │   ║  Form Section
║ │  │ مرحباً بك في نظام التقييم النفسي             │  │   ║  Subheading: 20pt SemiBold
║ │  ├──────────────────────────────────────────────────┤  │   ║
║ │  │                                                  │  │   ║  Intro text
║ │  │  يرجى إدخال رقمك الوطني لبدء الاختبار         │  │   ║
║ │  │  (البطاقة الذاتية أو جواز السفر)                │  │   ║
║ │  │                                                  │  │   ║
║ │  └──────────────────────────────────────────────────┘  │   ║
║ │                                                        │   ║
║ │  ┌──────────────────────────────────────────────────┐  │   ║  Input Group
║ │  │ * الرقم الوطني                                  │  │   ║  Label: 14pt Medium
║ │  ├──────────────────────────────────────────────────┤  │   ║  Input: 16pt, 48px height
║ │  │                                                  │  │   ║  Placeholder: "123456789012"
║ │  │ _ _ _ _ _ _ _ _ _ _ _ _ _                        │  │   ║
║ │  │                                                  │  │   ║
║ │  └──────────────────────────────────────────────────┘  │   ║
║ │                                                        │   ║
║ │  ┌──────────────────────────────────────────────────┐  │   ║  Checkbox
║ │  │ ☑ أوافق على شروط الخصوصية والاستخدام         │  │   ║  14pt, check in primary blue
║ │  └──────────────────────────────────────────────────┘  │   ║
║ │                                                        │   ║
║ │  ┌──────────────────────────────────────────────────┐  │   ║  Action Buttons
║ │  │        [ ابدأ الاختبار الآن ]                  │  │   ║  Primary: Blue-600, 16pt, 48px
║ │  └──────────────────────────────────────────────────┘  │   ║  Secondary: Gray border, RTL
║ │                                                        │   ║
║ │  ┌──────────────────────────────────────────────────┐  │   ║
║ │  │      [ شروط الخصوصية و التفاصيل ]             │  │   ║
║ │  └──────────────────────────────────────────────────┘  │   ║
║ │                                                        │   ║
║ └────────────────────────────────────────────────────────┘   ║
║                                                              ║
║  © 2025 جميع الحقوق محفوظة • الدعم: support@platform.com  ║  Footer: 12pt, Gray-500
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

**Design Principles:**
- ✅ Centered card with soft shadow (12px)
- ✅ Clear visual hierarchy (header → form → action)
- ✅ Large input field (48px height) for mobile touch
- ✅ RTL-native layout (field edges, button alignment)
- ✅ Accessibility: High contrast, large text, clear labels
- ✅ Color: Primary blue (#3B82F6) for CTA, gray for secondary

**Component Specs:**
- Card background: White (#FFFFFF)
- Card border-radius: 12px
- Card shadow: 0 4px 12px rgba(0,0,0,0.1)
- Button primary: Background #3B82F6, text white, hover #2563EB
- Input: Border gray-200, focus ring blue-500
- Spacing: 32px padding, 16px gap between elements

---

### 1.2 Privacy & Instructions Page

```
╔══════════════════════════════════════════════════════════════╗
║                    الخصوصية والشروط                          ║  Header: 24pt Bold
║                                                              ║
║ [← رجوع]                                                     ║  Back link: 14pt, blue-600
║                                                              ║
║ ┌────────────────────────────────────────────────────────┐   ║  Scrollable Content Area
║ │                                                        │   ║
║ │  🛡️ سياسة الخصوصية                                   │   ║  Section with icon
║ │  ─────────────────────────────────────────────────   │   ║
║ │                                                        │   ║
║ │  نحن نلتزم بحماية بيانات المشاركين...               │   ║  Body: 16pt, line-height 1.6
║ │  • البيانات الشخصية لا تُشارك مع أطراف ثالثة       │   ║  Bullet points: 14pt
║ │  • جميع المعلومات مشفرة وآمنة                        │   ║
║ │  • يمكنك طلب حذف بيانات الاختبار                    │   ║
║ │                                                        │   ║
║ │  📋 الشروط والأحكام                                   │   ║  Next section
║ │  ─────────────────────────────────────────────────   │   ║
║ │                                                        │   ║
║ │  بالموافقة على هذه الشروط:                           │   ║
║ │  ☑ أنت توافق على المشاركة الطوعية                  │   ║
║ │  ☑ تفهم أن النتائج لأغراض التقييم فقط             │   ║
║ │  ☑ لا توجد إجابات صحيحة أو خاطئة                   │   ║
║ │                                                        │   ║
║ │  ⏱️ معلومات الاختبار                                 │   ║
║ │  ─────────────────────────────────────────────────   │   ║
║ │                                                        │   ║
║ │  المدة: 60 دقيقة بالضبط                             │   ║
║ │  عدد الأسئلة: ~120 سؤال متنوع                        │   ║
║ │  الأنواع: خيارات متعددة، ليكرت، ترتيب، نصي          │   ║
║ │  الاستراحات: يمكنك الاستراحة فقط بين الأسئلة        │   ║
║ │                                                        │   ║
║ └────────────────────────────────────────────────────────┘   ║
║                                                              ║
║ ┌────────────────────────────────────────────────────────┐   ║  Action Buttons at bottom
║ │  ☑ أوافق على جميع الشروط                            │   ║
║ │                                                        │   ║
║ │ [ ابدأ الاختبار ]  [ ← رجوع للتعديل ]             │   ║
║ └────────────────────────────────────────────────────────┘   ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

**Design Improvements:**
- ✅ Icons (🛡️ 🛡️ 📋 ⏱️) for visual scanning
- ✅ Clear section breaks with thin dividers (gray-200)
- ✅ Checkbox for consent agreement (required)
- ✅ Scrollable content area for longer text
- ✅ Sticky action buttons at bottom
- ✅ RTL-aware text justification

---

## Section 2: Exam Interface

### 2.1 Modern Exam Screen (Main Layout)

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║ الاختبار: السؤال 42 من 120    │  ⏱ 24:18  │  ⋮  📊  🔔                    ║  Sticky Header
╟─────────────────────────────────────────────────────────────────────────────────╢
║  Progress bar: ████████████░░░░░ 35%                                          ║
╟═════════════════════════════════════════╦═════════════════════════════════════╢
║                                        ║                                     ║
║  في رأيك الشخصي، ما مدى توافقك       ║   Select one of the following:     ║
║  مع العبارة التالية؟                   ║                                     ║
║                                        ║   ☐ موافق جدا (Strongly Agree)    ║
║  "أنا شخص منظم وملتزم"                ║                                     ║
║                                        ║   ☐ موافق (Agree)                 ║
║                                        ║   ☑ محايد (Neutral)                ║
║  Dimension: الانتظام                   ║                                     ║
║  Type: Likert Scale                     ║   ☐ غير موافق (Disagree)         ║
║  Difficulty: ⭐⭐ (Medium)             ║                                     ║
║                                        ║   ☐ غير موافق جدا (Strongly...)  ║
║                                        ║                                     ║
║                                        ║  Time Limit: 45 seconds remaining  ║
║                                        ║  (if applicable)                   ║
║                                        ║                                     ║
║                                        ║   [ ← السابق ]   [ التالي → ]     ║
║                                        ║                                     ║
╠════════════════════════════════════════╩═════════════════════════════════════╣
║  Auto-saving...  ✓ Saved at 14:32                                            ║  Footer: Auto-save indicator
╚═════════════════════════════════════════════════════════════════════════════════╝
```

**Layout Details:**

| Section | Design Spec |
|---------|------------|
| **Header** | Sticky top, gray-50 bg, 12px shadow below |
| **Timer** | Font-mono, 20pt, red if < 5min, blue if normal |
| **Progress** | Thin bar (6px height), animated width change |
| **Question Panel** (Left) | max-width 50%, padding 24px |
| **Answer Panel** (Right) | max-width 50%, padding 24px, white bg, subtle shadow |
| **Radio Options** | 48px height each, hover: bg-blue-50, selected: blue-600 border |
| **Buttons** | 16pt, 48px height, 16px padding |
| **Footer** | Auto-save status, timestamp |

**Responsive Breakpoints:**
- **Mobile (< 640px):** Single column, stacked layout, full-width inputs
- **Tablet (640–1024px):** Single column with side-by-side sections below
- **Desktop (> 1024px):** 2-column grid as shown

---

### 2.2 Question Type Variations

#### Likert Scale Question
```
  Options displayed as:
  ☐ ☐ ☐ ☑ ☐  (Radio buttons, equal spacing)
  Labels: شدة الموافقة
         (Strongly Disagree ... Neutral ... Strongly Agree)
```

#### Multiple Choice Question
```
  ☐ الخيار الأول
  ☐ الخيار الثاني (selected: ☑)
  ☐ الخيار الثالث
  ☐ الخيار الرابع
```

#### Ordering Question
```
  ترتيب العبارات من الأقل إلى الأكثر:
  
  Drag-and-drop or numbered input:
  1. ← ترتيب الخيار الأول
  2. ← ترتيب الخيار الثاني (highlighted blue)
  3. ← ترتيب الخيار الثالث
```

#### Numeric Input
```
  Please enter a number: [____]
  Validation: Integer only, range 0-100
  Help text: "أدخل رقماً صحيحاً"
```

---

## Section 3: Results & Dashboard

### 3.1 Modern Results Summary Page

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║  تم إنجاز الاختبار بنجاح! ✓                                 [تحميل النتيجة]   ║
║  شكراً لمشاركتك!                                                             ║
║                                                                               ║
║ ┌────────────────────────────────────────────────────────────────────────┐   ║
║ │                                                                        │   ║
║ │  🎉 تحليل نتائجك                                                    │   ║
║ │  ────────────────────────────────────────────────────────────────   │   ║
║ │                                                                        │   ║
║ │  النتيجة الإجمالية: 52 T                                            │   ║
║ │  (متوسط، بحاجة إلى تطوير في بعض المجالات)                        │   ║
║ │                                                                        │   ║
║ │  ┌──────────────┬──────────────┬──────────────┐                      │   ║
║ │  │ ممتاز        │ متوسط        │ ضعيف         │                      │   ║
║ │  │ 3 أبعاد      │ 4 أبعاد      │ 2 بُعد       │                      │   ║
║ │  │ (Excellent)  │ (Average)    │ (Weak)       │                      │   ║
║ │  └──────────────┴──────────────┴──────────────┘                      │   ║
║ │                                                                        │   ║
║ │  💪 نقاط القوة:                                                     │   ║
║ │  ─────────────────                                                    │   ║
║ │  • الابتكار (65 T) — ممتاز في التفكير الخلاق                      │   ║
║ │  • القيادة (61 T) — قدرة جيدة على التوجيه                          │   ║
║ │  • الذكاء الاجتماعي (58 T) — مهارات تواصل قوية                    │   ║
║ │                                                                        │   ║
║ │  🌱 مجالات النمو:                                                   │   ║
║ │  ───────────────                                                      │   ║
║ │  • الانتظام (38 T) — يحتاج تطوير في التنظيم والالتزام             │   ║
║ │  • صبر (32 T) — العمل على الهدوء والتحمل                          │   ║
║ │                                                                        │   ║
║ │  📊 توزيع الأبعاد:                                                  │   ║
║ │  ─────────────────                                                    │   ║
║ │  [Chart placeholder — see section 3.2]                               │   ║
║ │                                                                        │   ║
║ └────────────────────────────────────────────────────────────────────────┘   ║
║                                                                               ║
║ [ تحميل التقرير الكامل PDF ]  [ ← رجوع للصفحة الرئيسية ]                ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

**Design Features:**
- ✅ Success message with emoji (🎉)
- ✅ KPI cards: 3 columns showing score distribution
- ✅ Bullet-point layout for strengths/weaknesses
- ✅ Clear section dividers (────)
- ✅ Color coding: Green for strengths, orange for growth
- ✅ Large CTA button for PDF download

---

### 3.2 Admin Dashboard — Horizontal Bar Chart (Recommended)

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║  نتائج الاختبار #1024                                     [ تحميل PDF ]      ║
║                                                            [ ← رجوع ]         ║
║ ┌───────────────────────────────────────────────────────────────────────┐    ║
║ │                                                                       │    ║
║ │  📊 توزيع الأبعاد حسب T-Score (من الضعيف إلى القوي)              │    ║
║ │  ─────────────────────────────────────────────────────────────────  │    ║
║ │                                                                       │    ║
║ │  الصبر           ██████           28 T   🔴 ضعيف                    │    ║
║ │                 (████░░░░░░░░░░░░░░░░)                            │    ║
║ │                                                                       │    ║
║ │  الانتظام        ███████████       42 T   🟠 متوسط                │    ║
║ │                 (███████░░░░░░░░░░░░░)                            │    ║
║ │                                                                       │    ║
║ │  الذكاء الاجتماعي ████████████████ 58 T   🟢 ممتاز               │    ║
║ │                 (████████████░░░░░░░░)                            │    ║
║ │                                                                       │    ║
║ │  الابتكار        ██████████████████ 65 T   🟢 ممتاز               │    ║
║ │                 (█████████████░░░░░░░░░░)                          │    ║
║ │                                                                       │    ║
║ │  القيادة         ███████████████  61 T   🟢 ممتاز               │    ║
║ │                 (███████████░░░░░░░░░░░░)                          │    ║
║ │                                                                       │    ║
║ │  ┌─────────────────────────────────────────────────────────────┐  │    ║
║ │  │ 20        40        60        80                            │  │    ║
║ │  │ T-Score Scale                                              │  │    ║
║ │  └─────────────────────────────────────────────────────────────┘  │    ║
║ │                                                                       │    ║
║ │  Legend:                                                             │    ║
║ │  🔴 Weak (< 40)  |  🟠 Average (40-54.9)  |  🟢 Excellent (≥ 55)  │    ║
║ │                                                                       │    ║
║ └───────────────────────────────────────────────────────────────────────┘    ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

**Chart Improvements Over Current:**

| Aspect | Current (Vertical) | Recommended (Horizontal) | Benefit |
|--------|-------------------|--------------------------|---------|
| **Layout** | Vertical bars | Horizontal bars | Easier to read, no crowded labels |
| **Sort** | Random or by ID | Ascending T-score (weakest first) | Quick visual scan of performance |
| **Colors** | All blue | Red/Orange/Green bands | Immediate performance status |
| **Fonts** | 9-12pt | 12-14pt | Professional, readable in print |
| **Width** | Fixed 700px | 600–800px scalable | Better use of page space |
| **Bars** | Single color | Gradient + percentage | Visual hierarchy, clarity |

---

### 3.3 KPI Cards Section

```
┌──────────────────┬──────────────────┬──────────────────┐
│ 3 ممتاز          │ 4 متوسط          │ 2 ضعيف           │
│ ═════════════    │ ═════════════    │ ═════════════    │
│ Excellent        │ Average          │ Weak             │
│                  │                  │                  │
│ 50%              │ 66.7%            │ 33.3%            │
│ النسبة من 6      │ النسبة من 6      │ النسبة من 6      │
│                  │                  │                  │
│ ↗ +2 من الاختبار│ ↗ +1 من الاختبار│ ↘ -1 من الاختبار│
│   الماضي         │   الماضي         │   الماضي         │
└──────────────────┴──────────────────┴──────────────────┘
```

**KPI Card Specs:**
- **Height:** 120px
- **Width:** Equal thirds (33% each)
- **Background:** Subtle gradient (green-50, orange-50, red-50)
- **Border:** 2px left border (green-600, amber-600, red-600)
- **Number:** 32pt bold, primary color
- **Label:** 14pt medium, gray-700
- **Trend:** 12pt small, with ↗ or ↘ icon
- **Shadow:** 0 2px 4px rgba(0,0,0,0.08)

---

## Section 4: Design System Components

### 4.1 Button Variants

```
PRIMARY (Action):
┌─────────────────────────┐
│    ابدأ الاختبار الآن   │  Background: #3B82F6
└─────────────────────────┘  Text: White
  Hover: #2563EB, shadow lifted
  Disabled: Gray-400

SECONDARY (Alternative):
┌─────────────────────────┐
│     ← رجوع للخلف        │  Border: 2px #E5E7EB
└─────────────────────────┘  Background: Transparent
  Text: Gray-700
  Hover: bg-gray-50

GHOST (Low Priority):
┌─────────────────────────┐
│   شروط الخصوصية        │  Border: None
└─────────────────────────┘  Background: Transparent
  Text: Blue-600 (underlined)
  Hover: Text-blue-700, underline
```

**Universal Specs:**
- Height: 48px (mobile touch-friendly)
- Padding: 12px (left/right)
- Border-radius: 8px
- Font-size: 16pt
- Font-weight: 600
- Transition: all 200ms ease
- Focus: Ring 2px offset 2px (blue-500)

---

### 4.2 Form Input Variants

```
DEFAULT (Text Input):
┌─────────────────────────────────────────┐
│ الرقم الوطني                            │  Label: 14pt Medium, gray-700
├─────────────────────────────────────────┤
│ _____ _____ _____ _____                 │  Input: 16pt Regular
│                                         │  Height: 44px
│ مثال: 123456789012                     │  Placeholder: gray-400
└─────────────────────────────────────────┘  Helper text: 12pt, gray-600
  Focus: Border 2px blue-500, shadow

DISABLED:
┌─────────────────────────────────────────┐
│ _____ _____ _____ _____                 │  Opacity: 50%
│                                         │  Cursor: not-allowed
└─────────────────────────────────────────┘

ERROR:
┌─────────────────────────────────────────┐
│ ⚠️ الرقم الوطني                         │  Border: 2px red-500
├─────────────────────────────────────────┤  Background: red-50
│ _____ _____ _____ _____                 │
│                                         │
│ ❌ الرقم غير صحيح. جاري التحقق...      │  Error message: red-600, 12pt
└─────────────────────────────────────────┘
```

---

### 4.3 Color Palette Reference

```
PRIMARY BLUE:
  Hex: #3B82F6
  RGB: 59, 130, 246
  Usage: CTAs, links, active states, focus rings
  Shades:
    50:  #EFF6FF (Lightest — backgrounds)
    100: #DBEAFE
    500: #3B82F6 (Main)
    600: #2563EB (Hover)
    700: #1D4ED8 (Active)
    800: #1E40AF (Darkest — text)

SECONDARY VIOLET:
  Hex: #8B5CF6
  RGB: 139, 92, 246
  Usage: Accents, secondary actions, decorative
  Shades: 50/100/500/700/900

SUCCESS GREEN:
  Hex: #10B981
  RGB: 16, 185, 129
  Usage: Scores ≥ 55, confirmations, success states

WARNING AMBER:
  Hex: #F59E0B
  RGB: 245, 158, 11
  Usage: Scores 40–54.9, cautions, medium importance

DANGER RED:
  Hex: #EF4444
  RGB: 239, 68, 68
  Usage: Scores < 40, errors, destructive actions

NEUTRAL GRAYS:
  50:  #F9FAFB (Lightest)
  100: #F3F4F6
  200: #E5E7EB
  400: #9CA3AF
  500: #6B7280
  600: #4B5563
  700: #374151
  800: #1F2937
  900: #111827 (Darkest)
```

---

### 4.4 Typography Scale

```
DISPLAY (28pt):
  Font: IBM Plex Sans Arabic Bold
  Line-height: 32pt (1.14)
  Letter-spacing: -0.5px
  Usage: Page titles

TITLE (24pt):
  Font: IBM Plex Sans Arabic Bold
  Line-height: 28pt (1.17)
  Letter-spacing: 0px
  Usage: Section headings

HEADING 1 (20pt):
  Font: IBM Plex Sans Arabic SemiBold
  Line-height: 24pt (1.2)
  Usage: Card titles, subsections

HEADING 2 (18pt):
  Font: IBM Plex Sans Arabic SemiBold
  Line-height: 22pt (1.22)
  Usage: Nested headings

BODY (16pt):
  Font: Inter Regular / Noto Naskh Arabic
  Line-height: 24pt (1.5)
  Letter-spacing: 0.3px
  Usage: Main content, descriptions

SMALL (14pt):
  Font: Inter Regular
  Line-height: 20pt (1.43)
  Usage: Secondary labels, UI text

TINY (12pt):
  Font: Inter Regular
  Line-height: 16pt (1.33)
  Usage: Captions, timestamps, helper text
```

---

## Section 5: Spacing Scale

```
XS:  4px   (0.25rem)   — Micro spacing
SM:  8px   (0.5rem)    — Tight spacing
MD:  12px  (0.75rem)   — Normal spacing
LG:  16px  (1rem)      — Comfortable spacing
XL:  24px  (1.5rem)    — Section spacing
2XL: 32px  (2rem)      — Large section
3XL: 48px  (3rem)      — Page-level margins

COMPONENT GUIDELINES:
  Card padding:     16px (LG)
  Card gap:         12px (MD)
  Page margin:      24–32px (responsive)
  Form field gap:   16px (LG)
  List item gap:    12px (MD)
  Button height:    48px
  Input height:     44px
```

---

## Section 6: Micro-Interactions

### 6.1 Loading States

```
SKELETON LOADER (Progressive):
┌────────────────────────────────────────┐
│ ▓▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░ │  Gray gradient pulse
│                                        │  Simulates content structure
│ ▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
│                                        │
│ ▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░ │
└────────────────────────────────────────┘
  Animation: 2s pulse, ease-in-out, infinite

SPINNER (Async):
     ⟲                                     Smooth rotation
   ⟲   ⟳  (24px icon)                   Duration: 1s, linear
     ⟳                                     Color: Primary blue
```

### 6.2 Transitions

```
PAGE ENTRANCE:
  Fade in + Slide up
  Duration: 300ms
  Easing: cubic-bezier(0.4, 0, 0.2, 1)
  Use: Page navigation, modal open

BUTTON CLICK:
  Scale down 95% → 100%
  Duration: 200ms
  Easing: cubic-bezier(0.34, 1.56, 0.64, 1)
  Use: All interactive elements

HOVER STATE:
  Lift shadow + subtle scale (105%)
  Duration: 200ms
  Use: Cards, links, buttons
```

---

## Section 7: Dark Mode (Optional Future)

If dark mode is implemented later:

```
Dark Palette:
  Background: #0F172A (slate-950)
  Surface:    #1E293B (slate-900)
  Border:     #334155 (slate-700)
  Text:       #F1F5F9 (slate-100)
  Text Light: #CBD5E1 (slate-400)

Primary (Adjusted):
  Main:   #60A5FA (blue-400)
  Hover:  #3B82F6 (blue-500)
  Darkest: #1E3A8A (blue-900, text)

Success: #34D399 (emerald-400)
Warning: #FBBF24 (amber-400)
Danger:  #F87171 (red-400)
```

---

## Implementation Checklist

- [ ] **Colors:** Define CSS variables in Tailwind config
- [ ] **Typography:** Load fonts, define sizing scale
- [ ] **Spacing:** Implement spacing tokens
- [ ] **Components:** Build shadcn/ui components
- [ ] **Animations:** Configure Framer Motion presets
- [ ] **Testing:** Verify on mobile, tablet, desktop
- [ ] **Accessibility:** WCAG AA compliance audit
- [ ] **Documentation:** Update component library docs
- [ ] **Performance:** Verify bundle size, Core Web Vitals

---

**Report Version:** 1.0  
**Status:** Ready for Designer Implementation  
**Next Step:** Create Figma Design System File

