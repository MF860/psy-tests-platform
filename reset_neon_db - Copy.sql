-- Run this in Neon SQL Editor with 'neondb' database selected
-- This will clean all tables so migrations can run fresh

DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;
DROP TABLE IF EXISTS "AIJobs" CASCADE;
DROP TABLE IF EXISTS "AIChatSessions" CASCADE;
DROP TABLE IF EXISTS "AuditLogs" CASCADE;
DROP TABLE IF EXISTS "ItemParameters" CASCADE;
DROP TABLE IF EXISTS "SessionItems" CASCADE;
DROP TABLE IF EXISTS "Results" CASCADE;
DROP TABLE IF EXISTS "Sessions" CASCADE;
DROP TABLE IF EXISTS "Items" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;
DROP TABLE IF EXISTS "Admins" CASCADE;

-- Verify tables are gone
SELECT tablename FROM pg_tables WHERE schemaname = 'public';
-- Should return empty (0 rows)
