-- Neon PostgreSQL Database Reset Script
-- WARNING: This will delete ALL data and recreate the schema fresh
-- Run this if you need to start with a clean database

-- Drop all tables (order matters due to foreign keys)
DROP TABLE IF EXISTS "AIJobs" CASCADE;
DROP TABLE IF EXISTS "ItemParameters" CASCADE;
DROP TABLE IF EXISTS "SessionItems" CASCADE;
DROP TABLE IF EXISTS "Results" CASCADE;
DROP TABLE IF EXISTS "Sessions" CASCADE;
DROP TABLE IF EXISTS "AuditLogs" CASCADE;
DROP TABLE IF EXISTS "Items" CASCADE;
DROP TABLE IF EXISTS "Users" CASCADE;
DROP TABLE IF EXISTS "Admins" CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;

-- Confirm all tables dropped
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- The backend will now recreate all tables on next startup via EnsureCreatedAsync()
