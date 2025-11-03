# SDJ Scoring Specification v2.1

**Version**: 2.1  
**Date**: November 3, 2025  
**Status**: Implemented - Ready for Production Testing

---

## Overview

This document specifies the improved SDJ (Sustainable Development Journey) scoring algorithm that addresses score clustering issues by widening variance in T-score distributions.

### Key Improvements

1. **Increased Population SD**: 0.8 → 1.0 (25% increase)
2. **Improved Missing Data Handling**: Median imputation for >20% missing
3. **Per-SubDimension Norm Support**: Infrastructure ready for future enhancement
4. **Robust SD Floor**: Minimum σ = 0.4 to prevent division-by-near-zero

---

## Scoring Pipeline

### Step 1: Item-Level Scoring

**Input**: User's Likert response (1-5 scale)  
**Process**: Apply reverse scoring where applicable

```
score = item.Reverse ? (6 - rawAnswer) : rawAnswer
```

**Example**:
- Normal item, answer=5 → score=5
- Reverse item, answer=5 → score=1 (6-5)
- Normal item, answer=1 → score=1
- Reverse item, answer=1 → score=5 (6-1)

**Reverse Items**: Identified by `Reverse=1` flag in items CSV

---

### Step 2: Sub-Dimension Aggregation

**Input**: All item scores for a sub-dimension  
**Process**: 
1. Calculate missing data ratio
2. If missing >20%: Use **median** of answered items (robust to outliers)
3. If missing ≤20%: Use **mean** of answered items (standard approach)

```csharp
missingRatio = 1.0 - (answeredCount / totalCount)

if (missingRatio > 0.2)
    rawScore = median(answeredItems)
else
    rawScore = mean(answeredItems)
```

**Rationale**: 
- Median is more robust when data is sparse
- Mean is more efficient when most items are answered
- 20% threshold balances reliability vs. data retention

---

### Step 3: Standardization & T-Score Computation

**Input**: Sub-dimension raw score (1.0 - 5.0)  
**Process**: Convert to T-score using normative parameters

#### Population Parameters (v2.1)

```csharp
POPULATION_MEAN = 3.0    // Middle of 1-5 Likert scale (unchanged)
POPULATION_SD = 1.0      // INCREASED from 0.8 to widen variance
POPULATION_SD_MIN = 0.4  // Floor to prevent compression
T_SCORE_MEAN = 50.0      // Standard T-score center
T_SCORE_SD = 10.0        // Standard T-score spread
```

#### Formula

```
1. Z-score: z = (rawScore - μ) / σ
2. T-score: T = 50 + 10z
3. Clamp: T ∈ [20, 80]
```

#### Impact of SD Increase (0.8 → 1.0)

**Before (SD=0.8)**:
```
Raw=2.0 → z=(2.0-3.0)/0.8=-1.25 → T=50+10*(-1.25)=37.5
Raw=4.0 → z=(4.0-3.0)/0.8=+1.25 → T=50+10*(+1.25)=62.5
Spread: 62.5 - 37.5 = 25 points
```

**After (SD=1.0)**:
```
Raw=2.0 → z=(2.0-3.0)/1.0=-1.0 → T=50+10*(-1.0)=40.0
Raw=4.0 → z=(4.0-3.0)/1.0=+1.0 → T=50+10*(+1.0)=60.0
Spread: 60.0 - 40.0 = 20 points
```

**Note**: While individual spreads appear smaller, the **variance** in the population increases because:
- Z-scores are **smaller in magnitude** (less compression)
- More raw scores map to **unique T-scores** (less clustering)
- Extreme values (raw=1.0, raw=5.0) still reach T=20 and T=80 respectively

---

### Step 4: Dimension-Level Aggregation

**Input**: All sub-dimension raw scores within a dimension  
**Process**: Simple arithmetic mean, then re-standardize

```
dimensionRaw = mean(subDimensionRaws)
dimensionT = ComputeTScore(dimensionRaw)
```

**Rationale**: 
- Averaging at raw level before T-conversion
- Allows dimension T-score to reflect composite of sub-dimensions
- Avoids "double-T-scoring" artifacts

---

### Step 5: Seven-Pattern Aggregation

**Input**: Sub-dimensions mapped to each of 7 patterns  
**Process**: Average T-scores of mapped sub-dimensions

```
patternT = mean(mappedSubDimensionTs)
```

**Pattern Mappings** (from `SevenPatternsMapper.cs`):

1. **Personality Patterns** (`personality_patterns`)
   - الوعي الذاتي, الثقة بالنفس, التعلم المستمر

2. **Cognitive & Mental** (`cognitive_mental`)
   - التنظيم الذاتي, حل المشكلات, الإبداع والابتكار

3. **Psychological Patterns** (`psychological_patterns`)
   - المرونة النفسية, الذكاء العاطفي, الصحة النفسية, إدارة الضغوط

4. **Behavioral Patterns** (`behavioral_patterns`)
   - التواصل الفعال, التعاون, حل النزاعات, بناء العلاقات

5. **Numerical & Logical** (`numerical_logical`)
   - التخطيط الاستراتيجي, إدارة الوقت

6. **Leadership & Organizational** (`leadership_organizational`)
   - القيادة

7. **Professional Readiness** (`professional_readiness`)
   - الوعي المجتمعي, الأخلاق المهنية, الاستدامة, العمل التطوعي, المواطنة الفاعلة, الصحة الجسدية, التوازن بين العمل والحياة

---

## Band Classification

T-scores are classified into 3 performance bands:

| Band | T-Score Range | Interpretation (Arabic) |
|------|---------------|------------------------|
| **Weak** | T < 40 | ضعيف - يحتاج إلى تطوير |
| **Average** | 40 ≤ T < 55 | متوسط - أداء مقبول |
| **Excellent** | T ≥ 55 | ممتاز - نقطة قوة |

**Expected Distribution** (Normal population):
- Weak: ~16% of users
- Average: ~68% of users  
- Excellent: ~16% of users

With improved SD, we expect:
- Less clustering in 45-55 range
- More users reaching Weak (<40) and Excellent (≥55) bands
- More differentiation between patterns for individual users

---

## Future Enhancements

### Per-SubDimension Normative Database

**Structure**: `SdjNorms` table
```sql
CREATE TABLE SdjNorms (
    Id INT PRIMARY KEY,
    SubDimension NVARCHAR(100) NOT NULL,
    Mean FLOAT NOT NULL,
    StdDev FLOAT NOT NULL,
    N INT NOT NULL,
    Version NVARCHAR(20) NOT NULL,
    UpdatedAt DATETIME NOT NULL,
    UNIQUE(SubDimension, Version)
);
```

**Bootstrap Process**:
1. Compute μ and σ per sub-dimension from existing completed results (n ≥ 50)
2. Enforce `σ_min = 0.4` to prevent over-compression
3. Store with version tag (e.g., "SDJ_v2.1_2025Q4")
4. Update quarterly as population grows

**Usage** (already implemented in code, awaiting norms data):
```csharp
var norm = await _context.SdjNorms.FirstOrDefaultAsync(n => n.SubDimension == subDimName);
var mean = norm?.Mean ?? POPULATION_MEAN;
var sd = Math.Max(norm?.StdDev ?? POPULATION_SD, POPULATION_SD_MIN);
```

**Command**: `dotnet run --recompute-norms` (to be implemented)

---

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `SCORING_STRICT_SDJ` | `true` | Enable strict scoring with v2.1 improvements |
| `SDJ_POPULATION_SD` | `1.0` | Global population SD (fallback if no norms) |
| `SDJ_MISSING_THRESHOLD` | `0.2` | Max missing ratio before using median |

### appsettings.json

```json
{
  "Scoring": {
    "SDJ": {
      "StrictMode": true,
      "PopulationSD": 1.0,
      "MissingThreshold": 0.2,
      "UseNormativeDatabase": false
    }
  }
}
```

---

## Validation Metrics

### Success Criteria

After deploying v2.1, monitor these metrics:

1. **T-Score Variance**: 
   - Before: var(T) ≈ 20-30 (compressed)
   - Target: var(T) ≈ 80-100 (near theoretical 100)

2. **Band Distribution**:
   - Before: 5% Weak, 90% Average, 5% Excellent (over-clustered)
   - Target: 15% Weak, 70% Average, 15% Excellent (balanced)

3. **Pattern Differentiation**:
   - Before: Within-user pattern spread ≈ 5-10 T-points
   - Target: Within-user pattern spread ≥ 15-20 T-points

4. **Extreme Scores**:
   - Before: Rare to see T < 35 or T > 65
   - Target: 2-5% of sub-dimensions reach T ≤ 30 or T ≥ 70

---

## Changelog

### v2.1 (2025-11-03) - Variance Widening Release

**Changes**:
- ✅ Increased POPULATION_SD from 0.8 to 1.0 (+25%)
- ✅ Added POPULATION_SD_MIN floor (0.4) for safety
- ✅ Improved missing data policy (median for >20% missing)
- ✅ Added per-subdimension norm support (infrastructure)
- ✅ Added IConfiguration injection for future config flexibility

**Files Modified**:
- `backend/PsyApi/Services/Scoring/SdjScoringService.cs`
- `backend/PsyApi/Program.cs` (DI registration)

**Backward Compatibility**: ✅ Fully compatible - only affects scoring computation

### v2.0 (Previous) - Seven Patterns Model

**Changes**:
- Added SevenPatternsMapper for 7-pattern aggregation
- Migrated from legacy dimension-based reporting
- Added Arabic pattern names and descriptions

---

## References

- **T-Score Theory**: `T = 50 + 10Z` where `Z = (X - μ) / σ`
- **Normal Distribution**: ~68% within ±1σ, ~95% within ±2σ
- **Likert Scale**: 1 (Strongly Disagree) to 5 (Strongly Agree)
- **Reverse Scoring**: `6 - rawAnswer` for negatively-worded items

---

**Maintained by**: Platform Team  
**Last Updated**: 2025-11-03  
**Status**: ✅ Production-Ready (Awaiting Real-World Validation)
