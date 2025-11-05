# Baseline Report Structure (Pre-v6.1)

## Current Report (UltraHiFiPdfReportService v4.0)

**Pages**: 5-6 pages  
**Charts**: Radar + Horizontal Bars + **Donut** (to be removed)  
**Theme**: Mixed legacy (Aurora Glass / Noir Executive remnants)  
**DPI**: 300-450

### Page Structure:
1. **Page 1**: Cover & Executive Summary
   - Header with logo
   - Overall status badge
   - 4 KPI cards (Avg T-Score, Percentile, Variance, Advanced Patterns)
   - Top 3 Strengths
   - Growth Areas (bottom 3)
   - Band distribution summary

2. **Page 2**: Visual Analytics
   - Radar chart (12 dimensions)
   - Horizontal bar chart (12 dimensions)
   - **Donut chart** (cluster distribution) ← **TO BE REMOVED**
   - Statistical summary

3. **Page 3**: Data Story & Analysis
   - General insights (5 items)
   - Cluster analysis with top dimensions
   - Risk flags

4. **Page 4**: Development Plan
   - Action plan (6 items)
   - Tips for success (5 tips)

5. **Page 5**: Training Courses (conditional)
   - Course cards (6 courses)
   - Course note

6. **Page 6**: Quality & Methodology
   - Performance bands interpretation table
   - Methodology description
   - Report statistics
   - Confidentiality notice

## v6.1 Target: 14-Page Report with Content Parity

### Missing Content to Add (8 additional pages):
- **Page 7-8**: Detailed Dimension Breakdown (all dimensions with interpretations)
- **Page 9**: Percentile Rankings & Comparative Analysis
- **Page 10**: Risk Matrix & Mitigation Strategies
- **Page 11**: Extended Recommendations & Resources
- **Page 12**: Development Tracking Template
- **Page 13**: Technical Appendix (Scoring formulas, norms)
- **Page 14**: Glossary & References

### Key Changes:
1. ✅ Replace Donut with additional Bar charts
2. ✅ Upgrade charts to Vector/600+ DPI
3. ✅ Apply AuroraNeo design tokens
4. ✅ Add NormalizeArabic() to all text
5. ✅ Expand from 6 to 14 pages
6. ✅ Add 8 pages of detailed content

### Files to Modify:
- `DesignTokens.cs` ✅ (completed)
- `UltraHiFiPdfReportService.cs` (expand to 14 pages)
- `RadarChartRenderer.cs` (verify 600 DPI, add node dots)
- `HorizontalBarChartRenderer.cs` (verify 600 DPI, improve RTL)
- Remove all `DonutChartRenderer` calls

### Validation Criteria:
- `pages >= 14`
- `donutDetected == false`
- `hasGarbled == false`
- `status == "PASS"` in parity.json
