-- ============================================================
-- CLEANUP OLD DATA - Keep Migration, Remove Old Questions
-- ============================================================
-- Current State:
--   ✓ Migration registered in __EFMigrationsHistory
--   ✓ Columns exist (PatternId, SubId, etc.)
--   ✗ 125 old questions (SDJ V1) blocking new data
--   ✗ PatternId is NULL for all items
--
-- Solution: Delete old data, keep schema and migration record
-- ============================================================

-- STEP 1: Backup check (optional - just to verify current state)
SELECT 
    COUNT(*) as total_items,
    COUNT(CASE WHEN "PatternId" IS NOT NULL THEN 1 END) as v2_items,
    COUNT(CASE WHEN "PatternId" IS NULL THEN 1 END) as v1_items
FROM "Items";

-- Expected: total=125, v2=0, v1=125

-- ============================================================
-- STEP 2: Delete all related data (CASCADE will handle FK constraints)
-- ============================================================

-- Delete all session items (references Items)
TRUNCATE TABLE "SessionItems" CASCADE;

-- Delete all results (references Sessions)
TRUNCATE TABLE "Results" CASCADE;

-- Delete all sessions
TRUNCATE TABLE "Sessions" CASCADE;

-- Delete all items (125 old questions)
TRUNCATE TABLE "Items" CASCADE;

-- ============================================================
-- STEP 3: Verify cleanup
-- ============================================================

SELECT COUNT(*) as items_count FROM "Items";
-- Expected: 0

SELECT COUNT(*) as sessions_count FROM "Sessions";
-- Expected: 0

SELECT COUNT(*) as results_count FROM "Results";
-- Expected: 0

SELECT COUNT(*) as session_items_count FROM "SessionItems";
-- Expected: 0

-- ============================================================
-- STEP 4: Verify migration is still registered
-- ============================================================

SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%SDJ%'
ORDER BY "MigrationId";

-- Expected: Should show 20251029143657_SDJ_V2_SevenPatterns

-- ============================================================
-- STEP 5: Verify schema is intact
-- ============================================================

SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_name = 'Items'
  AND column_name IN ('PatternId', 'PatternKey', 'PatternNameAr', 'SubId', 'SubKey', 'SubNameAr')
ORDER BY ordinal_position;

-- Expected: All 6 columns should be listed

-- ============================================================
-- DONE! Now go to Render and trigger deploy
-- ============================================================
-- Next steps:
-- 1. This script cleaned the data ✓
-- 2. Migration is still registered ✓
-- 3. Schema is intact ✓
-- 4. Go to Render Dashboard
-- 5. Manual Deploy → "Clear build cache & deploy"
-- 6. Wait for logs to show:
--    "[SDJ V2] Items table is empty, seeding 210 items..."
--    "[SDJ V2] Post-seed verification passed: 210 items"
--    "Deploy succeeded!"
-- ============================================================
