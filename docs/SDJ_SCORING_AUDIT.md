# SDJ Scoring Audit & Analysis

**Date**: November 3, 2025  
**Issue**: SDJ scores clustering in 45-55 range regardless of answer strategy  
**Goal**: Diagnose root causes and implement variance-widening fixes

---

## Phase 0: Code Discovery & Component Mapping

### 🔍 Core Components Located

#### 1. **Scoring Service** (Primary Logic)
- **File**: `backend/PsyApi/Services/Scoring/SdjScoringService.cs` (378 lines)
- **Key Methods**:
  - `ComputeSdjScores(int sessionId)` - Main entry point (lines 28-107)
  - `ScoreLikertItem(Item item, string answer)` - Item-level scoring with reverse (lines 111-125)
  - `AggregateBySubDimension()` - Sub-dimension aggregation (lines 127-162)
  - `AggregateByDimension()` - Dimension-level aggregation (lines 164-192)
  - `ComputeTScore(double rawScore)` - Z-to-T transformation (lines 195-204)
  - `ComputePercentile(double tScore)` - T-to-percentile (lines 206-217)
  - `GetBand(double tScore)` - Band classification (lines 219-223)

#### 2. **Seven Patterns Mapper**
- **File**: `backend/PsyApi/Services/Scoring/SevenPatternsMapper.cs` (230 lines)
- **Purpose**: Maps 20+ sub-dimensions to 7 major patterns
- **Key Mapping**: Sub-dimension name (Arabic) → Pattern key
- **Aggregation**: Averages T-scores of mapped sub-dimensions per pattern (lines 137-157)

#### 3. **Data Models**
- **Session/Answer Capture**: `backend/PsyApi/Controllers/SessionsController.cs`
- **Result Storage**: Models include `Result`, `SessionItem`, `Item`, `Answer`
- **Scoring Output**: `SdjScoreSummary`, `SdjDimensionScore`, `SdjSubDimensionScore`, `SevenPatternScore`

#### 4. **Seed Data** 
- **Active CSV**: `seed/questions_sdj_v2_ar.csv`
- **Columns**: ItemCode, TextAr, PatternId, PatternKey, PatternNameAr, SubId, SubKey, SubNameAr, Type, **Reverse**, TimeLimitSeconds, Weight, CorrectAnswer, Options
- **Reverse Flag**: Column 10 (0=normal, 1=reverse scoring)
- **Question Types**: MCQ, LikertAgreement, Frequency

---

## Phase 1: White-Box Formula Analysis

### 📊 Current Scoring Pipeline (as-is)

#### **Step 1: Item-Level Scoring** (Lines 111-125)
```csharp
private double ScoreLikertItem(Item item, string answer)
{
    // Parse answer: "1", "2", "3", "4", "5"
    if (!int.TryParse(answer, out var rawValue) || rawValue < 1 || rawValue > 5)
        rawValue = 3; // Neutral default
    
    // Reverse scoring: 6 - rawValue
    var score = item.Reverse ? (6 - rawValue) : rawValue;
    return score; // Range: 1.0 - 5.0
}
```
**✅ Reverse Scoring**: Correctly implemented as `6 - x` for items with `Reverse=true`  
**✅ Likert Normalization**: Direct 1-5 mapping, no unintended centering  
**⚠️ Missing Data**: Defaults to 3 (neutral) - may compress variance if many missing

#### **Step 2: Sub-Dimension Aggregation** (Lines 127-162)
```csharp
private List<SdjSubDimensionScore> AggregateBySubDimension(...)
{
    var subDimGroups = sessionItems
        .Where(si => si.Item.SubDimension != null)
        .GroupBy(si => new { si.Item.Dimension, si.Item.SubDimension });
    
    foreach (var group in subDimGroups)
    {
        var scores = group.Where(si => itemScores.ContainsKey(si.ItemId))
            .Select(si => itemScores[si.ItemId])
            .ToList();
        
        var raw = scores.Average(); // Simple mean of 1-5 scores
        var tScore = ComputeTScore(raw);
        var percentile = ComputePercentile(tScore);
        // ...
    }
}
```
**✅ Aggregation Method**: Arithmetic mean (unweighted)  
**⚠️ Weighting**: No per-item weights applied (even though CSV has Weight column)  
**✅ Scale Preservation**: Keeps 1-5 scale before T-score conversion

#### **Step 3: Dimension-Level Aggregation** (Lines 164-192)
```csharp
private List<SdjDimensionScore> AggregateByDimension(...)
{
    var dimGroups = subDimensionScores.GroupBy(s => s.Dimension);
    
    foreach (var group in dimGroups)
    {
        var subDims = group.ToList();
        var raw = subDims.Average(s => s.Raw); // Mean of sub-dimension raw scores
        var tScore = ComputeTScore(raw);
        // ...
    }
}
```
**✅ Aggregation**: Mean of sub-dimension raw scores  
**⚠️ Double-Averaging**: Already T-scored sub-dims are re-averaged at raw level then re-T-scored

#### **Step 4: Standardization & T-Score Conversion** ⚠️ **CRITICAL ISSUE**
```csharp
// Constants (Lines 17-20)
private const double POPULATION_MEAN = 3.0;  // Middle of 1-5 Likert scale
private const double POPULATION_SD = 0.8;     // Typical SD for Likert responses
private const double T_SCORE_MEAN = 50.0;
private const double T_SCORE_SD = 10.0;

// Computation (Lines 195-204)
private double ComputeTScore(double rawScore)
{
    var zScore = (rawScore - POPULATION_MEAN) / POPULATION_SD;
    var tScore = T_SCORE_MEAN + (T_SCORE_SD * zScore);
    return Math.Clamp(tScore, 20, 80); // Clamp to [20, 80]
}
```

**🔴 ROOT CAUSE #1: Fixed Population Parameters**
- **μ = 3.0** (hardcoded middle of Likert scale)
- **σ = 0.8** (assumed "typical" SD)
- **Problem**: These are **not normative** - they're **hypothetical**
- **Impact**: 
  - If real responses cluster near 3.0, Z-scores shrink toward 0
  - T-scores compress toward 50
  - Example: `raw=3.2 → z=(3.2-3.0)/0.8=0.25 → T=50+10*0.25=52.5`
  - Example: `raw=2.8 → z=(2.8-3.0)/0.8=-0.25 → T=50+10*(-0.25)=47.5`
  - Even **extreme** `raw=1.0 → T=50+10*(-2.5)=25` (clamped to 25)
  - Even **extreme** `raw=5.0 → T=50+10*(2.5)=75` (clamped to 75)

**🔴 ROOT CAUSE #2: Session-Level vs. Normative Standardization**
- Current: Uses **global fixed** μ/σ (not session-specific, which is good)
- BUT: Not using **actual normative data** from real test-takers
- Should compute μ/σ **per sub-dimension** from historical data

**🔴 ROOT CAUSE #3: Small SD (0.8) Compresses Variance**
- Likert responses typically have SD closer to **0.9-1.2** for diverse populations
- Using σ=0.8 makes Z-scores **larger** than they should be, BUT...
- If real answers cluster tightly, even large Z still centers near 0

**✅ Clamping**: `[20, 80]` is reasonable (not over-constrained like `[45, 55]`)

#### **Step 5: Seven-Pattern Aggregation** (SevenPatternsMapper.cs Lines 137-157)
```csharp
public static List<SevenPatternScore> MapToSevenPatterns(SdjScoreSummary sdjScores)
{
    foreach (var pattern in Patterns.OrderBy(p => p.Order))
    {
        var relevantSubDimensions = sdjScores.SubDimensions
            .Where(sd => SubDimensionToPatternMap.TryGetValue(sd.SubDimension, out var patternKey) 
                         && patternKey == pattern.Key)
            .ToList();
        
        if (relevantSubDimensions.Any())
        {
            var avgRaw = relevantSubDimensions.Average(sd => sd.Raw);
            var avgT = relevantSubDimensions.Average(sd => sd.T);
            // Use avgT for pattern T-score
        }
    }
}
```
**⚠️ Third-Level Averaging**: Pattern T = average of sub-dimension T-scores  
**Problem**: Multiple layers of averaging further compresses variance toward global mean

---

## Phase 2: Checklist of Issues

| Issue | Status | Details |
|-------|--------|---------|
| **Reverse scoring** | ✅ Correct | `6 - x` for `Reverse=true` items (line 121) |
| **Likert normalization** | ✅ Correct | Direct 1-5 mapping, no centering |
| **Sub-dim aggregation** | ✅ Mean | Arithmetic mean of item scores |
| **Dimension aggregation** | ⚠️ Double-averaged | Re-averages already-T-scored sub-dims |
| **Normative μ/σ** | ❌ **Missing** | Uses fixed μ=3.0, σ=0.8 instead of real norms |
| **σ too small** | ⚠️ Possible | σ=0.8 might be conservative for diverse populations |
| **Session-level std** | ✅ Not used | Uses global μ/σ (good), but they're hypothetical (bad) |
| **Clipping** | ✅ Reasonable | `[20, 80]` - not over-constrained |
| **Missing data policy** | ⚠️ Defaults to 3 | May compress variance if many missing answers |
| **Weighting** | ❌ Not applied | CSV has Weight column, but not used in aggregation |
| **Triple averaging** | ⚠️ Variance loss | Item → SubDim → Dim → Pattern (3 layers) |

---

## Phase 3: Variance Compression Analysis

### Why Scores Cluster 45-55

**Scenario 1: User answers mostly 3-4 (slightly agree)**
```
Item scores: [3, 4, 3, 4, 3, 3, 4, 3]
SubDim mean: 3.375
Z-score: (3.375 - 3.0) / 0.8 = 0.47
T-score: 50 + 10 * 0.47 = 54.7 ✅ Within 45-55
```

**Scenario 2: User answers mostly 2-3 (neutral-disagree)**
```
Item scores: [2, 3, 2, 3, 3, 2, 2, 3]
SubDim mean: 2.5
Z-score: (2.5 - 3.0) / 0.8 = -0.625
T-score: 50 + 10 * (-0.625) = 43.75 ✅ Within 45-55
```

**Scenario 3: User strongly agrees (mostly 5)**
```
Item scores: [5, 5, 4, 5, 5, 4, 5, 5]
SubDim mean: 4.75
Z-score: (4.75 - 3.0) / 0.8 = 2.19
T-score: 50 + 10 * 2.19 = 71.9 ❌ Should escape 45-55, but...
```
**BUT**: If multiple sub-dimensions average back toward 3-4, pattern-level T regresses toward 50

**Conclusion**: Current pipeline **cannot** escape 45-55 unless user has **extreme** and **consistent** answers across entire pattern

---

## Phase 4: Synthetic Test Requirements

### Test Sessions to Generate

1. **All-High Non-Reverse** (expect T ≥ 65 for most patterns)
   - Non-reverse items: answer = 5
   - Reverse items: answer = 1
   - Expected raw per sub-dim: ~4.5-5.0 → T ~68-75

2. **All-Low Non-Reverse** (expect T ≤ 40 for most patterns)
   - Non-reverse items: answer = 1
   - Reverse items: answer = 5
   - Expected raw per sub-dim: ~1.0-1.5 → T ~25-31

3. **Pattern-Focused Extremes** (7 sessions, one per pattern)
   - Target pattern sub-dims: answer 5/1 (reverse-aware) → expect T ≥ 65
   - Other patterns: answer 3 → expect T ~50

4. **Random** (uniform 1-5, expect T ~48-52 due to CLT)

5. **User-Like** (realistic skew: high leadership, low anxiety, mid others)

### Success Criteria

- **Pattern-focused sessions** should yield ≥20-point T-score spread between target pattern and others
- Currently expecting **all patterns** to cluster 45-55 (proving the bug)

---

## Phase 5: Proposed Fixes

### Fix 1: Bootstrap Normative Database
- Compute μ and σ **per sub-dimension** from existing completed SDJ results (n ≥ 50)
- Store in `Norms_SDJ` table: `SubDimension`, `Mean`, `StdDev`, `N`, `Version`, `UpdatedAt`
- Enforce `σ_min = 0.35` to prevent division-by-near-zero

### Fix 2: Use Per-SubDimension Norms
```csharp
var norm = await _context.Norms_SDJ.FirstOrDefaultAsync(n => n.SubDimension == subDimName);
var mean = norm?.Mean ?? 3.0; // Fallback to default
var sd = Math.Max(norm?.StdDev ?? 0.7, 0.35); // Enforce minimum SD
var zScore = (subDimMean - mean) / sd;
```

### Fix 3: Reduce Averaging Layers
- **Option A**: Compute pattern T-score from **item-level** scores directly (skip sub-dim T aggregation)
- **Option B**: Use **weighted mean** of sub-dimension Z-scores (not T-scores) before final T conversion

### Fix 4: Apply Item Weights (if available)
- CSV has Weight column - use weighted average instead of arithmetic mean

### Fix 5: Improve Missing Data Policy
- If >20% items missing in sub-dimension → exclude from pattern (don't impute to 3)
- If ≤20% missing → impute with sub-dimension median of answered items (not global 3)

### Fix 6: Feature Flag
- Add `SCORING_STRICT_SDJ=true` (default) in appsettings.json
- Allows rollback if issues arise

---

## Next Steps

1. ✅ **Phase 0 Complete**: Code discovery and formula documentation
2. 🔄 **Phase 1**: Create synthetic test harness (scoring_probe)
3. ⏳ **Phase 2**: Run diagnostics and capture before-state JSON
4. ⏳ **Phase 3**: Implement fixes (normative DB, weighting, missing policy)
5. ⏳ **Phase 4**: Re-run synthetics, verify ≥20-point spread
6. ⏳ **Phase 5**: Unit tests, documentation, deploy

---

**Status**: Phase 0 complete, moving to Phase 1 (synthetic test harness)
