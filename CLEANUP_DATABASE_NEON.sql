-- ═══════════════════════════════════════════════════════════════
-- SDJ V2 Database Cleanup Script for Neon PostgreSQL
-- Copy and paste this ENTIRE script into Neon SQL Editor
-- ═══════════════════════════════════════════════════════════════

-- STEP 1: Delete all existing data
-- This is safe - we will reseed everything from CSV
TRUNCATE TABLE "SessionItems" CASCADE;
TRUNCATE TABLE "Results" CASCADE;
TRUNCATE TABLE "Sessions" CASCADE;
TRUNCATE TABLE "Items" CASCADE;

-- STEP 2: Remove SDJ migration history
-- This forces Render to re-apply the V2 migration
DELETE FROM "__EFMigrationsHistory" 
WHERE "MigrationId" LIKE '%SDJ%' 
   OR "MigrationId" >= '20251029000000';

-- STEP 3: Verification Query
-- All counts should be 0
SELECT 
    'Items' as "Table", 
    COUNT(*) as "Count",
    CASE WHEN COUNT(*) = 0 THEN '✓ Ready' ELSE '✗ Not empty' END as "Status"
FROM "Items"
UNION ALL
SELECT 'Sessions', COUNT(*), CASE WHEN COUNT(*) = 0 THEN '✓ Ready' ELSE '✗ Not empty' END FROM "Sessions"
UNION ALL
SELECT 'SessionItems', COUNT(*), CASE WHEN COUNT(*) = 0 THEN '✓ Ready' ELSE '✗ Not empty' END FROM "SessionItems"
UNION ALL
SELECT 'Results', COUNT(*), CASE WHEN COUNT(*) = 0 THEN '✓ Ready' ELSE '✗ Not empty' END FROM "Results";

-- EXPECTED OUTPUT:
-- Table         | Count | Status
-- --------------|-------|--------
-- Items         |   0   | ✓ Ready
-- Sessions      |   0   | ✓ Ready
-- SessionItems  |   0   | ✓ Ready
-- Results       |   0   | ✓ Ready

-- If you see "✓ Ready" for all tables, database is cleaned!
-- Now go to Render and trigger "Manual Deploy"
