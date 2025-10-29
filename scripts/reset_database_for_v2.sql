-- SDJ V2 Database Reset Script for Neon PostgreSQL
-- Run this in Neon SQL Editor to prepare database for SDJ V2 migration
-- Date: October 29, 2025

-- Step 1: Clear all existing data
TRUNCATE TABLE "SessionItems" CASCADE;
TRUNCATE TABLE "Results" CASCADE;
TRUNCATE TABLE "Sessions" CASCADE;
TRUNCATE TABLE "Items" CASCADE;

-- Step 2: Remove old SDJ migrations (keep this to force re-migration)
DELETE FROM "__EFMigrationsHistory" 
WHERE "MigrationId" LIKE '%SDJ%' OR "MigrationId" >= '20251029000000';

-- Step 3: Verify cleanup
SELECT 
    'Items' as TableName, COUNT(*) as RowCount FROM "Items"
UNION ALL
SELECT 'Sessions', COUNT(*) FROM "Sessions"
UNION ALL
SELECT 'SessionItems', COUNT(*) FROM "SessionItems"
UNION ALL
SELECT 'Results', COUNT(*) FROM "Results";

-- Step 4: Check migration history
SELECT "MigrationId", "ProductVersion" 
FROM "__EFMigrationsHistory" 
ORDER BY "MigrationId" DESC 
LIMIT 5;

-- Expected result after running this script:
-- All tables should show 0 rows
-- Migration 20251029143657_SDJ_V2_SevenPatterns should NOT be in history
-- This will force Render to re-apply the migration on next deployment
