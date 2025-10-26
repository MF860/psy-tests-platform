# Production Deployment Guide - Render + Vercel + Neon

## 🎯 Overview

This guide covers deploying:
- **Backend API**: .NET 8 → Render Web Service
- **User UI**: Vite/React → Vercel
- **Admin UI**: Vite/React → Vercel
- **Database**: Neon PostgreSQL (SSL required)
- **Features**: SDJ enabled (USE_SDJ=1), Arabic RTL, No Demo Mode

---

## 📋 Prerequisites Checklist

- ✅ Neon PostgreSQL database created with connection details
- ✅ Render account (free tier available)
- ✅ Vercel account (free tier available)
- ✅ Git repository pushed to GitHub/GitLab/Bitbucket
- ✅ Backend migrations up-to-date (`dotnet ef migrations list`)

---

## 🚀 Part 1: Deploy Backend to Render

### Step 1.1: Create Render Web Service

1. Go to [Render Dashboard](https://dashboard.render.com/)
2. Click **"New +"** → **"Web Service"**
3. Connect your Git repository
4. Configure:
   - **Name**: `psy-api` (or your choice)
   - **Region**: Choose closest to your users
   - **Branch**: `main` (or your production branch)
   - **Root Directory**: `backend/PsyApi`
   - **Runtime**: `.NET`
   - **Build Command**:
     ```bash
     dotnet restore && dotnet build -c Release
     ```
   - **Start Command**:
     ```bash
     dotnet run --configuration Release --urls http://0.0.0.0:$PORT
     ```
   - **Instance Type**: Free (or upgrade as needed)

### Step 1.2: Configure Environment Variables

In Render service settings, add these environment variables:

```bash
# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:10000

# Database Configuration - Neon PostgreSQL
# Replace with YOUR Neon connection details
PGHOST=ep-hidden-hat-a432u8az-pooler.us-east-1.aws.neon.tech
PGDATABASE=neondb
PGUSER=neondb_owner
PGPASSWORD=<your-neon-password>
PGSSLMODE=require

# Connection String (Render will substitute $PGHOST, $PGDATABASE, etc.)
ConnectionStrings__DefaultConnection=Host=${PGHOST};Database=${PGDATABASE};Username=${PGUSER};Password=${PGPASSWORD};Ssl Mode=Require;Trust Server Certificate=true

# Alternative: Single connection string (choose ONE approach)
# ConnectionStrings__DefaultConnection=Host=ep-xxx.neon.tech;Database=neondb;Username=neondb_owner;Password=xxx;Ssl Mode=Require;Trust Server Certificate=true

# Feature Flags
USE_SDJ=1
USE_SQLITE=0

# CORS Origins (UPDATE after deploying frontends)
CORS_ALLOWED_ORIGINS=https://user-ui-xxx.vercel.app,https://admin-ui-xxx.vercel.app

# PDF Configuration (Optional - if using Arabic fonts)
PDF_FONT_FALLBACK=Noto Naskh Arabic

# OpenRouter AI (Optional - only if you have credits)
# OPENROUTER_API_KEY=sk-or-v1-xxxxx
# OPENROUTER_MODEL=deepseek/deepseek-chat
```

### Step 1.3: Deploy Backend

1. Click **"Create Web Service"**
2. Wait for initial deployment (~5-10 minutes)
3. Note your Render URL: `https://psy-api-xxx.onrender.com`

### Step 1.4: Verify Backend Health

Test the health endpoint:
```bash
curl https://psy-api-xxx.onrender.com/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2025-10-26T13:30:00Z",
  "environment": "Production",
  "useSdj": true
}
```

---

## 🌐 Part 2: Deploy User UI to Vercel

### Step 2.1: Create Vercel Project

1. Go to [Vercel Dashboard](https://vercel.com/dashboard)
2. Click **"Add New..."** → **"Project"**
3. Import your Git repository
4. Configure:
   - **Project Name**: `psy-user-ui` (or your choice)
   - **Framework Preset**: Vite
   - **Root Directory**: `frontend/user-ui`
   - **Build Command**: `npm run build` (Vercel auto-detects)
   - **Output Directory**: `dist` (Vercel auto-detects)
   - **Install Command**: `npm install`

### Step 2.2: Configure Environment Variables

In Vercel project settings → Environment Variables, add:

```bash
# API Configuration - REPLACE with your Render URL
VITE_API_BASE_URL=https://psy-api-xxx.onrender.com/api

# Mode Configuration
VITE_DEMO_MODE=false
VITE_APP_ENV=prod
VITE_STRICT_CONTRACTS=true

# OpenRouter AI (Optional - only if enabled)
# VITE_OPENROUTER_KEY=sk-or-v1-xxxxx
# VITE_OPENROUTER_MODEL=deepseek/deepseek-chat
```

**Important**: Ensure variable is `VITE_API_BASE_URL` (matches the code)

### Step 2.3: Deploy User UI

1. Click **"Deploy"**
2. Wait for deployment (~2-3 minutes)
3. Note your Vercel URL: `https://psy-user-ui-xxx.vercel.app`

### Step 2.4: Test User UI

1. Open `https://psy-user-ui-xxx.vercel.app`
2. Verify:
   - ✅ Arabic RTL interface loads
   - ✅ Login form accepts 10-digit national ID
   - ✅ No demo mode banner

---

## 👨‍💼 Part 3: Deploy Admin UI to Vercel

### Step 3.1: Create Second Vercel Project

1. In Vercel Dashboard, click **"Add New..."** → **"Project"**
2. Import the same Git repository
3. Configure:
   - **Project Name**: `psy-admin-ui` (or your choice)
   - **Framework Preset**: Vite
   - **Root Directory**: `frontend/admin-ui`
   - **Build Command**: `npm run build`
   - **Output Directory**: `dist`
   - **Install Command**: `npm install`

### Step 3.2: Configure Environment Variables

In Vercel project settings, add:

```bash
# API Configuration - REPLACE with your Render URL
VITE_API_BASE=https://psy-api-xxx.onrender.com/api

# Mode Configuration
VITE_DEMO_MODE=false
VITE_APP_ENV=prod
VITE_STRICT_CONTRACTS=true
```

**Note**: Admin UI uses `VITE_API_BASE` (without `_URL` suffix)

### Step 3.3: Deploy Admin UI

1. Click **"Deploy"**
2. Wait for deployment
3. Note your Vercel URL: `https://psy-admin-ui-xxx.vercel.app`

### Step 3.4: Test Admin UI

1. Open `https://psy-admin-ui-xxx.vercel.app`
2. Verify login page loads with Arabic text

---

## 🔐 Part 4: Update CORS Configuration

Now that you have both Vercel URLs, update the backend CORS:

### Step 4.1: Update Render Environment Variable

1. Go to Render Dashboard → Your service
2. Navigate to **Environment** tab
3. Update `CORS_ALLOWED_ORIGINS` variable:
   ```
   CORS_ALLOWED_ORIGINS=https://psy-user-ui-xxx.vercel.app,https://psy-admin-ui-xxx.vercel.app
   ```
   **Replace with your actual Vercel URLs** (no trailing slashes, comma-separated)

### Step 4.2: Redeploy Backend

1. Click **"Manual Deploy"** → **"Deploy latest commit"**
2. Wait for redeployment (~3-5 minutes)

### Step 4.3: Verify CORS

Test from browser console on User UI:
```javascript
fetch('https://psy-api-xxx.onrender.com/health')
  .then(r => r.json())
  .then(console.log)
```

Should succeed without CORS errors.

---

## ✅ Part 5: End-to-End Verification

### 5.1 Backend API Tests

```bash
# Health check
curl https://psy-api-xxx.onrender.com/health

# Start session
curl -X POST https://psy-api-xxx.onrender.com/api/sessions/start \
  -H "Content-Type: application/json" \
  -d '{"nationalId":"1000000001"}'

# Expected: {"sessionId":"...", "totalQuestions":80, "resume":false}
```

### 5.2 User UI E2E Flow

1. Open User UI URL
2. Login with national ID: `1000000001`
3. Verify:
   - ✅ Timer shows 60:00 and counts down
   - ✅ Questions are Likert scale (SDJ items)
   - ✅ Arabic text renders correctly (no boxes/gibberish)
   - ✅ Progress bar updates
   - ✅ RTL layout (buttons on left, text aligned right)
4. Answer ~5 questions with "محايد" (neutral)
5. Click "إنهاء الاختبار" (Finish Test)
6. Verify redirect to Thank You page

### 5.3 Admin UI Dashboard Tests

1. Open Admin UI URL
2. Login with admin credentials
3. Verify:
   - ✅ Dashboard loads without errors
   - ✅ Results list displays
   - ✅ Latest result appears (the one you just submitted)
4. Click result to view details:
   - ✅ SDJ horizontal bar chart (5 dimensions, sorted by T-score)
   - ✅ Radar chart renders
   - ✅ 5 donut charts (180px diameter)
   - ✅ All Arabic labels visible
5. Click "Generate PDF" button:
   - ✅ PDF downloads
   - ✅ Logo appears
   - ✅ Arabic text in PDF (not boxes)
   - ✅ Charts render correctly
   - ✅ Numbers formatted as Arabic numerals

### 5.4 Browser Console Checks

Open DevTools (F12) → Console:
- ✅ No 401/403/404/500 errors
- ✅ No CORS errors
- ✅ API requests go to Render URL (not localhost)

Open DevTools → Network tab:
- ✅ All API calls return 200 (except expected 429 rate limits)
- ✅ `/api/sessions/start` returns proper session structure
- ✅ `/api/sessions/{id}/next` returns Arabic question text

### 5.5 Render Logs Check

In Render Dashboard → Logs:
- ✅ No serialization errors
- ✅ "Now listening on: http://0.0.0.0:10000"
- ✅ Database queries execute successfully
- ✅ No connection refused errors

---

## 🔄 Part 6: Rollback Procedures

### Rollback User UI or Admin UI

1. Go to Vercel Dashboard → Your project
2. Navigate to **Deployments** tab
3. Find previous successful deployment
4. Click **"⋯"** menu → **"Promote to Production"**

### Rollback Backend API

1. Go to Render Dashboard → Your service
2. Navigate to **Events** tab
3. Find previous successful deployment
4. Click **"Rollback to this deploy"**

### Emergency: Disable SDJ

If SDJ causes issues, temporarily revert to legacy items:

1. Render → Environment → Set `USE_SDJ=0`
2. Manual Deploy → Deploy latest commit
3. Clear any in-progress sessions in Admin UI

---

## 📊 Part 7: Monitoring & Maintenance

### Health Monitoring

Set up external monitoring (e.g., UptimeRobot, Pingdom):
- Endpoint: `https://psy-api-xxx.onrender.com/health`
- Check interval: 5 minutes
- Alert on non-200 response

### Database Backups

Neon provides automatic backups:
1. Log into Neon Console
2. Navigate to Backups tab
3. Verify daily backups are enabled

### Performance Considerations

**Render Free Tier**: Spins down after 15 minutes of inactivity
- First request after sleep takes ~30 seconds
- Consider upgrading to Starter ($7/month) for always-on

**Vercel Free Tier**: Excellent performance, no sleep
- 100GB bandwidth/month
- Serverless function limits: 10s timeout, 1GB RAM

### Log Management

**Render Logs**:
- Available in Dashboard → Logs
- Retention: 7 days on free tier
- Consider log aggregation (Datadog, Logtail) for longer retention

**Vercel Logs**:
- Available in Dashboard → Deployments → Function Logs
- Real-time logs during development

---

## 🐛 Troubleshooting

### Issue: CORS errors in browser

**Symptom**: "Access to fetch blocked by CORS policy"

**Solution**:
1. Verify `CORS_ALLOWED_ORIGINS` in Render matches exact Vercel URLs
2. Check for trailing slashes (URLs should NOT have `/` at end)
3. Ensure protocol matches (`https://`, not `http://`)
4. Redeploy backend after changing CORS

### Issue: API returns 502 Bad Gateway

**Symptom**: Render URL returns 502

**Solutions**:
1. Check Render logs for startup errors
2. Verify connection string format (common mistake: missing SSL mode)
3. Test Neon connection:
   ```bash
   psql "postgresql://user:pass@host/db?sslmode=require"
   ```
4. Ensure `$PORT` variable used in start command

### Issue: Arabic text shows as boxes

**Symptom**: □□□□ instead of Arabic characters

**Solutions**:
1. Verify `<html lang="ar" dir="rtl">` in index.html
2. Check font-family includes Arabic support
3. For PDFs: Ensure `PDF_FONT_FALLBACK` set to Arabic-capable font

### Issue: "Rate limit exceeded" on session start

**Symptom**: 429 Too Many Requests

**Solution**:
- Backend has aggressive rate limiting for security
- Wait 60 seconds between session creation attempts
- For testing, increase limits in Program.cs rate limiter config

### Issue: Vercel deployment fails

**Symptom**: Build errors during Vercel deployment

**Solutions**:
1. Check Node.js version matches local (usually 18+)
2. Verify `package.json` has all dependencies
3. Clear Vercel cache: Settings → General → Clear Build Cache
4. Check build logs for specific errors

### Issue: Database migrations not applied

**Symptom**: SQL errors about missing tables

**Solution**:
```bash
# Locally, create migration bundle
cd backend/PsyApi
dotnet ef migrations bundle -o migrations-bundle

# Upload migrations-bundle to Render and run
./migrations-bundle --connection "Host=...;Database=..."
```

---

## 📝 Deployment Checklist

Use this checklist for each deployment:

### Pre-Deployment
- [ ] All tests pass locally
- [ ] Migrations created and tested
- [ ] Environment variables documented
- [ ] CORS origins list ready
- [ ] Neon database accessible

### Render Backend
- [ ] Service created with correct root directory
- [ ] Build/start commands configured
- [ ] All environment variables set
- [ ] Connection string format verified
- [ ] Health endpoint returns 200

### Vercel User UI
- [ ] Project created with correct root
- [ ] `VITE_API_BASE_URL` points to Render
- [ ] `VITE_DEMO_MODE=false`
- [ ] Deployment successful
- [ ] UI loads and is functional

### Vercel Admin UI
- [ ] Project created with correct root
- [ ] `VITE_API_BASE` points to Render (note: different variable name)
- [ ] `VITE_DEMO_MODE=false`
- [ ] Deployment successful
- [ ] Login page loads

### Post-Deployment
- [ ] CORS updated with both Vercel URLs
- [ ] Backend redeployed with new CORS
- [ ] E2E flow tested (login → exam → results → PDF)
- [ ] No console errors in browser
- [ ] Arabic text renders correctly
- [ ] Charts display properly
- [ ] PDF generation works

---

## 🔗 Quick Reference

### URLs Template

**Your Production URLs** (replace with actual):
```
Backend API:  https://psy-api-xxx.onrender.com
User UI:      https://psy-user-ui-xxx.vercel.app
Admin UI:     https://psy-admin-ui-xxx.vercel.app
Database:     ep-xxx-pooler.us-east-1.aws.neon.tech
```

### Key Environment Variables

| Variable | Backend (Render) | User UI (Vercel) | Admin UI (Vercel) |
|----------|------------------|------------------|-------------------|
| API URL | N/A | `VITE_API_BASE_URL` | `VITE_API_BASE` |
| Demo Mode | N/A | `VITE_DEMO_MODE=false` | `VITE_DEMO_MODE=false` |
| Environment | `ASPNETCORE_ENVIRONMENT=Production` | `VITE_APP_ENV=prod` | `VITE_APP_ENV=prod` |
| SDJ Flag | `USE_SDJ=1` | N/A | N/A |
| Database | `ConnectionStrings__DefaultConnection` | N/A | N/A |
| CORS | `CORS_ALLOWED_ORIGINS` | N/A | N/A |

### Support Resources

- [Render Documentation](https://render.com/docs)
- [Vercel Documentation](https://vercel.com/docs)
- [Neon Documentation](https://neon.tech/docs)
- [.NET 8 Deployment Guide](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/)

---

## ✨ Success Criteria

Your deployment is successful when:

1. ✅ Backend health endpoint returns 200 with `useSdj: true`
2. ✅ User UI loads and allows login
3. ✅ Exam flow completes (80 SDJ questions)
4. ✅ Admin UI displays results with charts
5. ✅ PDF generates with correct Arabic text and charts
6. ✅ No CORS errors in browser console
7. ✅ All API calls succeed (200/201 responses)
8. ✅ Arabic text renders correctly across all UIs
9. ✅ RTL layout works properly
10. ✅ No demo mode indicators visible

---

**Deployment Date**: _______________________

**Deployed By**: _______________________

**Verified By**: _______________________

**Notes**: 
_____________________________________________________________
_____________________________________________________________
_____________________________________________________________
