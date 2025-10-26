# 🔧 Neon Database Recreation Guide

## Current Situation
- ✅ Backend connects to Neon successfully
- ✅ Credentials are valid
- ❌ Database `neondb` was deleted during reset
- ❌ Need to recreate it manually

## Solution (Choose One)

### ⭐ Option 1: Create Database via Neon Console (EASIEST)

1. Open: https://console.neon.tech
2. Login and select your project
3. Click **"SQL Editor"** (left sidebar)
4. Copy and run this SQL:

```sql
CREATE DATABASE neondb OWNER neondb_owner;
```

5. Click **"Run"**
6. You should see: `CREATE DATABASE`
7. Done! Backend will now connect.

---

### Option 2: Use Neon's Default Database Name

Neon auto-creates a database when you create a project. Check your Neon dashboard for the actual database name. It's usually one of:
- `neondb` (if you named it that)
- `postgres` (PostgreSQL default, always exists)
- Same name as your project

**To find your database name:**
1. Go to https://console.neon.tech
2. Select your project
3. Look at the **"Connection Details"** section
4. The database name is shown in the connection string

**Update the connection string in:**
- `backend/PsyApi/appsettings.json`
- `backend/PsyApi/appsettings.Development.json`

Replace `Database=neondb` with the actual database name.

---

### Option 3: Use `postgres` Database (Always Available)

The `postgres` database always exists. You can use it directly:

**Update these files:**

`backend/PsyApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=postgres;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

`backend/PsyApi/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=postgres;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

---

## After Fixing - Test Connection

```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi

# Set environment variables
$env:ConnectionStrings__DefaultConnection = "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
$env:USE_SQLITE = "0"
$env:USE_SDJ = "1"

# DON'T use NEON_RESET_DB anymore (it deleted the database!)
# $env:NEON_RESET_DB = "1"  # ← Remove this

# Run backend
dotnet run
```

**Look for these SUCCESS logs:**
```
[INFO] Auto-detected PostgreSQL from connection string format
[INFO] Using PostgreSQL with connection string: Host=...
[INFO] Database schema ready
[INFO] Seeding 120 SDJ items...
```

---

## Why Did This Happen?

The `NEON_RESET_DB=1` flag calls `EnsureDeletedAsync()` which executes:
```sql
DROP DATABASE neondb WITH (FORCE);
```

This **permanently deletes** the database. EF Core's `EnsureCreatedAsync()` can create **tables/schema** but **cannot create databases** in PostgreSQL.

**Solution:** Never use `NEON_RESET_DB=1` in production or with cloud databases. Use migrations instead.

---

## Recommended Approach Going Forward

### For Development (Local Testing)
```powershell
# Don't use NEON_RESET_DB
# Instead, to reset schema only, connect and drop tables manually
```

### For Production (Render Deployment)
```yaml
# In render.yaml, don't set NEON_RESET_DB
# Use migrations:
await appDb.Database.MigrateAsync();
```

### To Reset Schema (Without Deleting Database)
```sql
-- Run in Neon SQL Editor
DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO neondb_owner;
GRANT ALL ON SCHEMA public TO public;
```

Then backend's `EnsureCreatedAsync()` will recreate all tables.

---

## Quick Fix Now

**Run this in Neon SQL Editor RIGHT NOW:**

```sql
CREATE DATABASE neondb OWNER neondb_owner;
```

Then start your backend:
```powershell
cd backend/PsyApi
dotnet run
```

✅ **That's it!** The backend will connect and create all tables automatically.
