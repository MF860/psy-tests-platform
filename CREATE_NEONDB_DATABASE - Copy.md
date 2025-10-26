# How to Create neondb Database in Neon

## Problem
The `postgres` default database exists but `neondb_owner` doesn't have permission to create tables in it.

## Solution: Create neondb Database via Neon Dashboard

### Step 1: Go to Neon Dashboard
1. Visit: https://console.neon.tech
2. Select your project: `ep-holy-glitter-a40pexde`

### Step 2: Create Database
1. Click on **"Databases"** in the left sidebar (NOT "SQL Editor")
2. Click the **"+ New Database"** button (top right)
3. Fill in the form:
   - **Database name**: `neondb`
   - **Owner**: Select `neondb_owner` from dropdown
4. Click **"Create"**

### Step 3: Update Connection Strings
After creating the database, update both appsettings files:

**backend/PsyApi/appsettings.json** - Change line 3:
```json
"DefaultConnection": "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
```

**backend/PsyApi/appsettings.Development.json** - Change line 3:
```json
"DefaultConnection": "Host=ep-holy-glitter-a40pexde-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=npg_OUubznM7eZW0;SSL Mode=Require;Trust Server Certificate=true"
```

### Step 4: Test Backend
```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:USE_SQLITE="0"
$env:USE_SDJ="1"
dotnet run
```

Expected output:
```
[INFO] Auto-detected PostgreSQL
[INFO] Applying database migrations...
[INFO] Database migrations completed successfully
[INFO] Seeding 120 SDJ items...
[INFO] Successfully seeded 120 SDJ items
[INFO] Now listening on: http://localhost:5000
```

### Step 5: Verify Data
Visit: http://localhost:5000/health

Should return:
```json
{
  "status": "Healthy",
  "database": "Connected",
  "provider": "PostgreSQL",
  "useSdj": true,
  "totalItems": 120,
  "sdjItems": 120
}
```

## Alternative: Direct SQL (If Dashboard Method Fails)
If the Dashboard method doesn't work, you can try SQL Editor:

1. Go to SQL Editor in Neon Console
2. Select the **postgres** database from dropdown (top left)
3. Run this command:
```sql
CREATE DATABASE neondb OWNER neondb_owner;
GRANT ALL PRIVILEGES ON DATABASE neondb TO neondb_owner;
```

## Why This is Needed
- The `postgres` database is owned by system/admin users
- `neondb_owner` doesn't have schema creation permissions in postgres
- Creating a dedicated `neondb` database gives `neondb_owner` full control
- This follows PostgreSQL best practices (one database per application)

## Files to Update After Database Creation
1. ✅ backend/PsyApi/appsettings.json (change Database=postgres → Database=neondb)
2. ✅ backend/PsyApi/appsettings.Development.json (same change)
3. ✅ render.yaml (already has Database=neondb, no change needed)
4. ✅ docs/DEPLOY_NEON.md (update examples to use neondb)

## Next Steps
After successful startup:
1. Test POST /api/sessions/start (creates test session)
2. Test GET /api/sessions/{id} (retrieves session)
3. Test POST /api/sessions/{id}/answer (submits answers)
4. Deploy to Render following render.yaml
5. Deploy frontends to Vercel with REACT_APP_API_URL pointing to Render
