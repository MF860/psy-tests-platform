# 🚀 Quick Deployment Reference Card

## URLs (Update with YOUR values)
```
Backend:  https://psy-api-________.onrender.com
User UI:  https://psy-user-ui-________.vercel.app
Admin UI: https://psy-admin-ui-________.vercel.app
```

## 📦 Render Backend Configuration

### Build & Start Commands
```bash
# Build Command
dotnet restore && dotnet build -c Release

# Start Command  
dotnet run --configuration Release --urls http://0.0.0.0:$PORT
```

### Environment Variables (Render Dashboard)
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:10000
USE_SDJ=1
USE_SQLITE=0

# Connection String - REPLACE with your Neon details
ConnectionStrings__DefaultConnection=Host=ep-hidden-hat-a432u8az-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=YOUR_PASSWORD;Ssl Mode=Require;Trust Server Certificate=true

# CORS - UPDATE after deploying frontends
CORS_ALLOWED_ORIGINS=https://user-ui-xxx.vercel.app,https://admin-ui-xxx.vercel.app

PDF_FONT_FALLBACK=Noto Naskh Arabic
```

## 🌐 Vercel User UI Configuration

### Project Settings
- **Root Directory**: `frontend/user-ui`
- **Framework**: Vite
- **Build Command**: `npm run build` (auto-detected)
- **Output Directory**: `dist` (auto-detected)

### Environment Variables (Vercel Dashboard)
```bash
# REPLACE with your Render URL
VITE_API_BASE_URL=https://psy-api-xxx.onrender.com/api

VITE_DEMO_MODE=false
VITE_APP_ENV=prod
VITE_STRICT_CONTRACTS=true
```

## 👨‍💼 Vercel Admin UI Configuration

### Project Settings
- **Root Directory**: `frontend/admin-ui`
- **Framework**: Vite
- **Build Command**: `npm run build`
- **Output Directory**: `dist`

### Environment Variables (Vercel Dashboard)
```bash
# REPLACE with your Render URL
# NOTE: Different variable name (no _URL suffix)
VITE_API_BASE=https://psy-api-xxx.onrender.com/api

VITE_DEMO_MODE=false
VITE_APP_ENV=prod
VITE_STRICT_CONTRACTS=true
```

## ✅ Post-Deployment Verification

### Quick Health Check
```bash
curl https://psy-api-xxx.onrender.com/health
```
Expected: `{"status":"healthy","useSdj":true}`

### Test Session Creation
```bash
curl -X POST https://psy-api-xxx.onrender.com/api/sessions/start \
  -H "Content-Type: application/json" \
  -d '{"nationalId":"1000000001"}'
```
Expected: `{"sessionId":"...","totalQuestions":80}`

### Run Full Verification Script
```powershell
.\verify-deployment.ps1 `
  -RenderUrl "https://psy-api-xxx.onrender.com" `
  -UserUrl "https://user-ui-xxx.vercel.app" `
  -AdminUrl "https://admin-ui-xxx.vercel.app"
```

## 🔧 Common Issues & Fixes

### Issue: CORS Error
**Fix**: Update Render env var
```bash
CORS_ALLOWED_ORIGINS=https://exact-user-url.vercel.app,https://exact-admin-url.vercel.app
```
Then: Manual Deploy in Render

### Issue: 502 Bad Gateway
**Fix**: Check Render logs, verify:
- Connection string format (SSL Mode required for Neon)
- `$PORT` variable used in start command
- No syntax errors in Program.cs

### Issue: Arabic shows as boxes
**Fix**:
- User/Admin UI: Check `<html lang="ar" dir="rtl">`
- PDF: Ensure `PDF_FONT_FALLBACK=Noto Naskh Arabic`

### Issue: "Cannot connect to database"
**Fix**: Test Neon connection directly:
```bash
psql "postgresql://user:pass@ep-xxx.neon.tech/db?sslmode=require"
```

## 📋 Deployment Checklist

- [ ] Neon database created and accessible
- [ ] Backend deployed to Render
- [ ] Health endpoint returns 200
- [ ] User UI deployed to Vercel
- [ ] Admin UI deployed to Vercel  
- [ ] CORS updated with Vercel URLs
- [ ] Backend redeployed after CORS update
- [ ] Verification script passes
- [ ] E2E exam flow works
- [ ] PDF generation works

## 🔗 Important Links

- [Full Deployment Guide](./DEPLOYMENT_GUIDE_RENDER_VERCEL.md)
- [Render Dashboard](https://dashboard.render.com/)
- [Vercel Dashboard](https://vercel.com/dashboard)
- [Neon Console](https://console.neon.tech/)

## 📞 Support Commands

### View Render Logs
```bash
# In Render Dashboard: Your Service → Logs
```

### Force Redeploy
```bash
# Render: Manual Deploy → Deploy latest commit
# Vercel: Deployments → Redeploy
```

### Rollback
```bash
# Render: Events → Previous deploy → Rollback
# Vercel: Deployments → Previous → Promote to Production
```

---

**Last Updated**: 2025-10-26
**Deployment Status**: Ready for production ✅
