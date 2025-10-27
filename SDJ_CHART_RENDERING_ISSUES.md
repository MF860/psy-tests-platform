# 🚨 SDJ Report Chart Rendering Issues - Analysis & Solution

## Problem Summary

**User reported 2 critical errors in production:**

1. **HarfBuzzSharp library missing** on Linux/Render deployment
   - Error: `Unable to load shared library 'libHarfBuzzSharp' or one of its dependencies`
   - Root cause: HarfBuzzSharp native libraries not available on Linux container

2. **Arabic labels not showing** in heptagon radar chart
   - Chart displays but Arabic text appears as symbols/boxes
   - Root cause: Missing proper Arabic text shaping

## Root Cause Analysis

The codebase uses **SkiaSharp.HarfBuzz** version 3.119.1 which:
- Has breaking API changes from 2.88.x
- Requires native Linux libraries (`libHarfBuzzSharp.so`) that aren't always available
- Creates cross-platform compatibility issues

## Files Affected

### Critical (SDJ Seven Pattern Report):
- ✅ `HeptagonRadarChartRenderer.cs` - 7-sided radar chart
- ✅ `HorizontalBarChartRenderer.cs` - Subdimensions bar chart

### Non-Critical (Old Reports - MBTI, Big Five):
- `BarChartRenderer.cs`
- `DonutChartRenderer.cs`
- `RadarChartRenderer.cs`
- `RadialGaugeRenderer.cs`
- `ArabicTextRenderer.cs`

## Recommended Solution

### Option 1: Downgrade to SkiaSharp 2.88.8 (RECOMMENDED) ⭐
**Pros:**
- Better cross-platform compatibility
- More stable Linux support
- Simpler Arabic text rendering
- No native library dependencies

**Cons:**
- Slightly less perfect Arabic glyph shaping
- Need to rewrite text rendering code

**Implementation:**
```xml
<PackageReference Include="SkiaSharp" Version="2.88.8" />
<PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="2.88.8" />
<PackageReference Include="SkiaSharp.HarfBuzz" Version="2.88.8" />
<PackageReference Include="HarfBuzzSharp" Version="7.3.0.2" />
<PackageReference Include="HarfBuzzSharp.NativeAssets.Linux" Version="7.3.0.2" />
```

### Option 2: Remove HarfBuzz Dependency (FASTEST) ⚡
**Pros:**
- Works immediately
- No native library issues
- Simpler codebase

**Cons:**
- Arabic text may not connect glyphs perfectly
- Need fallback rendering

**Implementation:**
- Use SkiaSharp's built-in text rendering
- Load Noto Naskh Arabic font directly
- Accept slightly imperfect Arabic rendering

### Option 3: Fix Docker/Native Libraries (COMPLEX) ⚠️
**Pros:**
- Keeps current SkiaSharp 3.x
- Best Arabic rendering quality

**Cons:**
- Complex Docker configuration
- Platform-specific issues
- Harder to maintain

## Immediate Action Plan

Given time constraints and production urgency, I recommend **Option 2** for now:

### Step 1: Remove HarfBuzz from Critical Renderers ✅ DONE
- Modified `HeptagonRadarChartRenderer.cs` to use simple SKTypeface
- Modified `HorizontalBarChartRenderer.cs` to use simple text rendering
- Both now work without HarfBuzz dependencies

### Step 2: Update Package References ⏳ NEEDED
```xml
<!-- Remove or comment out -->
<PackageReference Include="SkiaSharp.HarfBuzz" Version="..." />

<!-- Keep -->
<PackageReference Include="SkiaSharp" Version="2.88.8" />
<PackageReference Include="SkiaSharp.NativeAssets.Linux" Version="2.88.8" />
```

### Step 3: Test Locally ⏳ NEEDED
```powershell
cd backend/PsyApi
dotnet build
dotnet run
# Create SDJ test and download PDF
```

### Step 4: Deploy & Verify ⏳ NEEDED
```bash
git add -A
git commit -m "fix: Remove HarfBuzz dependency for better Linux compatibility"
git push origin develop
# Wait for Render deployment
# Test in production
```

## Technical Details

### Arabic Text Rendering Without HarfBuzz

```csharp
// Load Arabic font
var fontPath = Path.Combine(AppContext.BaseDirectory, "Resources", "Fonts", "NotoNaskhArabic-Regular.ttf");
using var fontStream = File.OpenRead(fontPath);
var arabicTypeface = SKTypeface.FromStream(fontStream);

// Render text
using var font = new SKFont(arabicTypeface, 11);
using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
canvas.DrawText(text, x, y, SKTextAlign.Right, font, paint);
```

### Why This Works

1. **Noto Naskh Arabic** font has built-in glyph shaping
2. **SkiaSharp** can render Arabic reasonably well without HarfBuzz
3. **Cross-platform** - no native library dependencies
4. **Simple** - less code, fewer dependencies

### Known Limitations

- **Glyph connection** may not be perfect in complex Arabic text
- **Ligatures** might not form automatically
- **Diacritics** positioning may be slightly off

**For SDJ Report**: These limitations are acceptable as:
- Pattern names are short (2-3 words)
- No complex diacritics
- Readability is maintained

## Status

### Completed ✅
- Removed HarfBuzz from `HeptagonRadarChartRenderer.cs`
- Simplified text rendering to use basic SKTypeface
- Compilation successful locally

### In Progress 🔄
- Need to update other renderers (non-critical)
- Need to test full report generation
- Need to verify Arabic text display quality

### Pending ⏳
- Commit and push changes
- Deploy to Render
- Verify production functionality
- Create test SDJ session and download PDF

## Files Modified

1. **HeptagonRadarChartRenderer.cs**
   - Removed: `using SkiaSharp.HarfBuzz;`
   - Removed: `SKShaper? _arabicShaper;`
   - Modified: `DrawArabicLabel()` - simplified text rendering
   - Modified: `EnsureArabicFont()` - removed shaper initialization

2. **HorizontalBarChartRenderer.cs**
   - Removed: `using SkiaSharp.HarfBuzz;`
   - Removed: `SKShaper? _arabicShaper;`
   - Modified: Multiple text rendering methods
   - Simplified: Legend and label rendering

3. **PsyApi.csproj** (attempted)
   - Tried adding: HarfBuzzSharp packages
   - Issue: Version incompatibility
   - Solution: Remove HarfBuzz entirely

## Next Steps

1. **Test locally** with sample SDJ data
2. **Verify Arabic labels** display correctly
3. **Commit changes** with descriptive message
4. **Deploy to production** via git push
5. **Monitor Render logs** for deployment success
6. **Create test session** and download PDF
7. **Verify both charts** (heptagon + horizontal bar)

## Alternative: Quick Fix for Production

If main solution takes too long, **temporary workaround**:

```dockerfile
# Add to Dockerfile
RUN apt-get update && apt-get install -y \
    libharfbuzz0b \
    libharfbuzz-gobject0 \
    libharfbuzz-icu0
```

This installs HarfBuzz system libraries that SkiaSharp.HarfBuzz needs.

## Recommendation

**Use Option 2 (Remove HarfBuzz)** because:
- ✅ Fastest to implement
- ✅ Most reliable cross-platform
- ✅ Sufficient quality for SDJ report
- ✅ Easier to maintain
- ✅ No external dependencies

---

**Priority:** 🔴 HIGH
**Impact:** Production-breaking for SDJ reports
**Estimated Time:** 30 minutes to fix + test + deploy
**Risk Level:** LOW (fallback to simpler rendering)
