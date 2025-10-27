# 🚀 RENDER + VERCEL DEPLOYMENT GUIDE
## Docker Backend with Arabic Fonts + SDJ Mode

---

## 📋 **SUMMARY OF CHANGES**

### ✅ Files Created/Modified:
1. **`backend/PsyApi/Dockerfile`** - NEW
   - Multi-stage build (SDK → Runtime)
   - Includes Arabic fonts: `fonts-noto`, `fonts-noto-extra`, `fonts-dejavu`
   - Includes text shaping libraries: `libharfbuzz0b`, `libicu72`
   - Binds to Render's `$PORT` variable
   - Sets `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` for ICU support

2. **`backend/PsyApi/.dockerignore`** - NEW
   - Excludes `bin/`, `obj/`, `*.db`, test files
   - Optimizes Docker build context

3. **`render.yaml`** - UPDATED
   - Changed runtime: `dotnet` → `docker`
   - Set `dockerfilePath: backend/PsyApi/Dockerfile`
   - Set `dockerContext: backend/PsyApi`
   - All env vars documented

4. **`frontend/admin-ui/src/lib/apiAdmin.ts`** - UPDATED
   - Added `getApiBaseUrl()` helper
   - Supports both `VITE_API_BASE_URL` and `VITE_API_BASE`
   - Fallback: `http://localhost:5019/api`

5. **`frontend/admin-ui/src/api/psyAdmin.ts`** - UPDATED
   - Same API base URL fallback logic

6. **`.gitignore`** - UPDATED
   - Commented out `.dockerignore` exclusion
   - Docker files now tracked in repo

---

## 🎯 **DEPLOYMENT STEPS**

### **STEP 1: Deploy Backend to Render (Docker)**

#### Option A: Using Blueprint (Recommended)
1. Go to [Render Dashboard](https://dashboard.render.com/)
2. Click **"New +"** → **"Blueprint"**
3. Connect your GitHub account if not connected
4. Select repository: **`MF860/psy-tests-platform`**
5. Select branch: **`develop`**
6. Render will detect `render.yaml` and create the service
7. Click **"Apply"**

#### Option B: Manual Web Service Creation
1. Go to [Render Dashboard](https://dashboard.render.com/)
2. Click **"New +"** → **"Web Service"**
3. Connect repository: **`MF860/psy-tests-platform`**
4. Configure:
   - **Name**: `psy-api-backend`
   - **Region**: Oregon (or closest to you)
   - **Branch**: `develop`
   - **Runtime**: **Docker**
   - **Dockerfile Path**: `backend/PsyApi/Dockerfile`
   - **Docker Context**: `backend/PsyApi`
   - **Plan**: Free (or Starter for $7/mo)

---

### **STEP 2: Set Environment Variables in Render**

After service is created, go to **Environment** tab and add:

```bash
# ASP.NET Core Configuration
ASPNETCORE_ENVIRONMENT=Production

# SDJ Mode (REQUIRED - must be "1")
USE_SDJ=1

# Database Mode
USE_SQLITE=0

# Neon PostgreSQL Connection String
# Format: Host=ep-xxx.neon.tech;Database=neondb;Username=neondb_owner;Password=xxx;SSL Mode=Require;Trust Server Certificate=true
ConnectionStrings__DefaultConnection=<YOUR_NEON_CONNECTION_STRING>

# CORS Origins (UPDATE after deploying frontends to Vercel)
# Example: https://user-ui-abc123.vercel.app,https://admin-ui-def456.vercel.app
CORS_ALLOWED_ORIGINS=<YOUR_VERCEL_FRONTEND_URLS>

# PDF Configuration
PDF_FONT_FALLBACK=Noto Naskh Arabic

# AI Configuration (OPTIONAL - only if using DeepSeek/OpenRouter)
DEEPSEEK_API_KEY=<YOUR_DEEPSEEK_API_KEY>
OPENROUTER_API_KEY=<YOUR_OPENROUTER_API_KEY>
OPENROUTER_MODEL=deepseek/deepseek-chat

# JWT Configuration (will be auto-generated if using Blueprint)
Jwt__Secret=<AUTO_GENERATED_OR_SET_MANUALLY>
Jwt__Issuer=PsyApi
Jwt__Audience=PsyApiClients
Jwt__ExpiryMinutes=1440
```

#### 🔐 **Getting Your Secrets:**

**Neon Connection String:**
- Go to [Neon Console](https://console.neon.tech/)
- Select your project: `psy-tests-platform`
- Go to **Connection Details**
- Copy **Connection string** (select "Pooled connection")
- Format should be: `postgresql://user:pass@ep-xxx.neon.tech/dbname?sslmode=require`
- Convert to .NET format:
  ```
  Host=ep-xxx.neon.tech;Database=neondb;Username=neondb_owner;Password=xxx;SSL Mode=Require;Trust Server Certificate=true
  ```

**DeepSeek API Key (Optional):**
- Sign up at [platform.deepseek.com](https://platform.deepseek.com/)
- Go to API Keys section
- Create new key
- Format: `sk-xxxxxxxxxxxxxxxx`

---

### **STEP 3: Deploy & Monitor**

1. Click **"Manual Deploy"** → **"Deploy latest commit"**
2. Watch logs in Render dashboard
3. Wait for build to complete (~3-5 minutes)
4. Service will show **"Live"** status when ready
5. Note your backend URL: `https://psy-api-backend.onrender.com`

#### ✅ **Expected Log Output:**
```
==> Building Dockerfile
==> Downloading base image...
==> Installing fonts-noto, libharfbuzz0b, libicu72...
==> Copying published app...
==> Starting container
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:10000
info: Application started. Press Ctrl+C to shut down.
info: Hosting environment: Production
info: Database migrations completed successfully
info: Seeded 125 SDJ items + 125 parameters + 10 users
```

---

### **STEP 4: Deploy Frontends to Vercel**

#### **Deploy User UI:**

1. Go to [Vercel Dashboard](https://vercel.com/dashboard)
2. Click **"Add New..."** → **"Project"**
3. Import `MF860/psy-tests-platform`
4. Configure:
   - **Project Name**: `psy-tests-user-ui`
   - **Framework Preset**: Vite
   - **Root Directory**: `frontend/user-ui`
   - **Build Command**: `npm run build`
   - **Output Directory**: `dist`

5. **Environment Variables** (click "Add" for each):
   ```bash
   VITE_API_BASE_URL=https://psy-api-backend.onrender.com/api
   VITE_APP_ENV=prod
   VITE_DEMO_MODE=false
   VITE_STRICT_CONTRACTS=true
   ```

6. Click **"Deploy"**
7. Note your URL: `https://psy-tests-user-ui-xxx.vercel.app`

#### **Deploy Admin UI:**

1. Repeat steps above with:
   - **Project Name**: `psy-tests-admin-ui`
   - **Root Directory**: `frontend/admin-ui`
   - **Environment Variables**: (same as user-ui)

2. Note your URL: `https://psy-tests-admin-ui-xxx.vercel.app`

---

### **STEP 5: Update CORS in Render**

Now that you have Vercel URLs, go back to Render:

1. Go to your backend service → **Environment** tab
2. Update `CORS_ALLOWED_ORIGINS`:
   ```
   https://psy-tests-user-ui-xxx.vercel.app,https://psy-tests-admin-ui-xxx.vercel.app
   ```
3. Click **"Save Changes"**
4. Service will automatically redeploy (~1-2 minutes)

---

## ✅ **VERIFICATION COMMANDS**

### **1. Health Check**
Replace `<RENDER_HOST>` with your Render URL (e.g., `psy-api-backend.onrender.com`)

```bash
curl -s https://<RENDER_HOST>/health
```

**Expected Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-10-26T12:34:56Z",
  "environment": "Production",
  "useSdj": true
}
```

### **2. Start Session (SDJ Mode)**
```bash
curl -s -X POST https://<RENDER_HOST>/api/sessions/start \
  -H "Content-Type: application/json" \
  -d "{\"nationalId\":\"1000000001\"}"
```

**Expected Response:**
```json
{
  "sessionId": 123,
  "nationalId": "1000000001",
  "useSdj": true,
  "message": "Session started successfully"
}
```

### **3. Get Next Question**
Replace `<SESSION_ID>` with ID from previous response:

```bash
curl -s https://<RENDER_HOST>/api/sessions/<SESSION_ID>/next
```

**Expected Response:**
```json
{
  "itemCode": "SDJ_001",
  "prompt": "أفضل العمل في بيئة...",
  "options": ["A", "B", "C"],
  "timeoutSeconds": 30,
  "questionNumber": 1
}
```

### **4. Test CORS from Browser**
1. Open User UI in browser: `https://psy-tests-user-ui-xxx.vercel.app`
2. Open DevTools → Network tab
3. Click "ابدأ الاختبار" (Start Test)
4. Verify API calls show **200 OK** status
5. Verify Arabic text renders correctly (no boxes/squares)

### **5. Test Admin UI**
1. Open Admin UI: `https://psy-tests-admin-ui-xxx.vercel.app/login`
2. Login with default credentials:
   - Username: `admin`
   - Password: (as set in `ADMIN_SEED_PASSWORD` env var, or default from code)
3. Verify dashboard loads with analytics
4. Test PDF generation: Go to Results → View any result → Download PDF
5. Verify Arabic text renders in PDF

---

## 🔍 **TROUBLESHOOTING**

### **Backend not responding:**
- Check Render logs for errors
- Verify `$PORT` is being used (check logs: "Now listening on: http://0.0.0.0:10000")
- Free tier spins down after 15min inactivity → first request takes ~30-60s

### **Database connection errors:**
- Verify Neon connection string format is correct
- Check Neon database is not paused (free tier pauses after 7 days inactivity)
- Verify SSL Mode=Require is set

### **CORS errors in browser:**
- Check `CORS_ALLOWED_ORIGINS` includes exact Vercel URLs (no trailing slashes)
- Verify Render service redeployed after CORS update
- Check browser console for specific origin mismatch

### **Arabic text shows as boxes:**
- Verify Docker build included font installation (check Render build logs)
- Verify `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` is set
- Verify `PDF_FONT_FALLBACK=Noto Naskh Arabic` is set

### **Frontend shows "API Error":**
- Verify `VITE_API_BASE_URL` is set correctly in Vercel
- Trigger Vercel redeploy after env var changes
- Check browser DevTools → Network tab for exact error

---

## 📊 **EXPECTED COSTS**

### **Free Tier (Testing):**
- **Render**: Free (750 hours/month, spins down after 15min)
- **Vercel**: Free (100GB bandwidth, unlimited deployments)
- **Neon**: Free (3GB storage, 1 project, sleeps after 7 days)
- **Total**: $0/month

### **Production (Always-On):**
- **Render Starter**: $7/month (always-on, 512MB RAM)
- **Vercel Pro**: $20/month (unlimited bandwidth, priority builds)
- **Neon Scale**: $19/month (10GB storage, always active)
- **Total**: ~$46/month

---

## 🎉 **DEPLOYMENT COMPLETE!**

### **Your Live URLs:**
```
Backend API:  https://psy-api-backend.onrender.com
User UI:      https://psy-tests-user-ui-xxx.vercel.app
Admin UI:     https://psy-tests-admin-ui-xxx.vercel.app
```

### **Next Steps:**
1. ✅ Test all verification commands above
2. ✅ Verify Arabic PDFs render correctly
3. ✅ Test complete user flow (start test → answer questions → view results)
4. ✅ Test admin login and analytics dashboard
5. ✅ Monitor Render logs for first 24 hours
6. ✅ Set up custom domains (optional)
7. ✅ Configure monitoring/alerts (optional)

---

## 📚 **Additional Resources**

- **Render Docs**: https://render.com/docs
- **Vercel Docs**: https://vercel.com/docs
- **Neon Docs**: https://neon.tech/docs
- **Project Repo**: https://github.com/MF860/psy-tests-platform

---

## 🆘 **NEED HELP?**

If you encounter issues:
1. Check Render logs: Dashboard → Service → Logs
2. Check Vercel logs: Dashboard → Project → Deployments → View Function Logs
3. Verify all env vars are set correctly
4. Test backend directly with curl commands above
5. Check browser DevTools console and network tab

---

**Generated**: October 26, 2025
**Commit**: `2fe0288` - Docker deployment with Arabic fonts for Render
