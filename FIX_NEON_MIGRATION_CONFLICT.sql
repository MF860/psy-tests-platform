-- ============================================================
-- FIX NEON MIGRATION CONFLICT - SDJ V2
-- ============================================================
-- Problem: Migration 20251029143657_SDJ_V2_SevenPatterns failed 
-- because columns already exist but migration not recorded
-- 
-- Solution: Mark migration as completed in history table
-- ============================================================

-- STEP 1: Check current state
SELECT 
    column_name, 
    data_type,
    is_nullable
FROM information_schema.columns
WHERE table_name = 'Items' 
  AND column_name IN ('PatternId', 'PatternKey', 'PatternNameAr', 'SubId', 'SubKey', 'SubNameAr')
ORDER BY ordinal_position;

-- Expected result: Should show 6 columns already exist
-- If columns DON'T exist, DO NOT run this script - run CLEANUP_DATABASE_NEON.sql instead

-- STEP 2: Check if migration is already recorded
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%SDJ%'
ORDER BY "MigrationId";

-- Expected: Should NOT show 20251029143657_SDJ_V2_SevenPatterns

-- ============================================================
-- SOLUTION: Manually register the migration as completed
-- ============================================================
-- This tells EF Core that the migration was already applied
-- and prevents it from trying to add columns again

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251029143657_SDJ_V2_SevenPatterns', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- STEP 3: Verify the fix
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%SDJ%'
ORDER BY "MigrationId";

-- Expected: Should now show 20251029143657_SDJ_V2_SevenPatterns

-- ============================================================
-- VERIFICATION: Check Items table structure
-- ============================================================
SELECT COUNT(*) as total_items FROM "Items";

-- If count = 0: Render will seed 210 new items on next deploy
-- If count > 0: Old data exists, may need cleanup

-- Check if any items have V2 fields populated
SELECT COUNT(*) as v2_items_count
FROM "Items"
WHERE "PatternId" IS NOT NULL;

-- ============================================================
-- NEXT STEPS AFTER RUNNING THIS SCRIPT:
-- ============================================================
-- 1. Run this script in Neon SQL Editor
-- 2. Verify migration now appears in __EFMigrationsHistory
-- 3. Go to Render Dashboard
-- 4. Click "Manual Deploy" → "Clear build cache & deploy"
-- 5. Monitor logs - should see:
--    ✓ No migration errors
--    ✓ "[SDJ V2] Using CSV file: questions_sdj_v2_ar.csv"
--    ✓ "Post-seed verification passed: 210 items"
-- ============================================================
