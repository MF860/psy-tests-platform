# Production Environment Variables Setup Script
# This script documents the environment variables needed for production deployment

Write-Host "=== PSY TESTS PLATFORM - PRODUCTION ENVIRONMENT SETUP ===" -ForegroundColor Cyan
Write-Host ""

# ===================================
# STEP 1: NEON DATABASE
# ===================================
Write-Host "STEP 1: Neon PostgreSQL Database" -ForegroundColor Yellow
Write-Host "-------------------------------" -ForegroundColor Yellow
Write-Host "Your Neon Connection Details:"
Write-Host "  PGHOST:     ep-hidden-hat-a432u8az-pooler.us-east-1.aws.neon.tech"
Write-Host "  PGDATABASE: neondb"
Write-Host "  PGUSER:     neondb_owner"
Write-Host "  PGPASSWORD: <your-password>" -ForegroundColor Red
Write-Host ""

# ===================================
# STEP 2: RENDER BACKEND
# ===================================
Write-Host "STEP 2: Render Backend Environment Variables" -ForegroundColor Yellow
Write-Host "-------------------------------" -ForegroundColor Yellow
Write-Host "Set these in Render Dashboard -> Environment:"
Write-Host ""
Write-Host "ASPNETCORE_ENVIRONMENT=Production"
Write-Host "ASPNETCORE_URLS=http://0.0.0.0:10000"
Write-Host "USE_SDJ=1"
Write-Host "USE_SQLITE=0"
Write-Host ""
Write-Host "ConnectionStrings__DefaultConnection=" -NoNewline
Write-Host "Host=ep-hidden-hat-a432u8az-pooler.us-east-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=<YOUR_PASSWORD>;Ssl Mode=Require;Trust Server Certificate=true" -ForegroundColor Cyan
Write-Host ""
Write-Host "CORS_ALLOWED_ORIGINS=" -NoNewline
Write-Host "<UPDATE_AFTER_DEPLOYING_FRONTENDS>" -ForegroundColor Red
Write-Host "  Example: https://user-ui-xxx.vercel.app,https://admin-ui-xxx.vercel.app"
Write-Host ""
Write-Host "PDF_FONT_FALLBACK=Noto Naskh Arabic"
Write-Host ""

# ===================================
# STEP 3: VERCEL USER UI
# ===================================
Write-Host "STEP 3: Vercel User UI Environment Variables" -ForegroundColor Yellow
Write-Host "-------------------------------" -ForegroundColor Yellow
Write-Host "Set these in Vercel Dashboard -> Settings -> Environment Variables:"
Write-Host ""
Write-Host "VITE_API_BASE_URL=" -NoNewline
Write-Host "<YOUR_RENDER_URL>/api" -ForegroundColor Red
Write-Host "  Example: https://psy-api-xxx.onrender.com/api"
Write-Host ""
Write-Host "VITE_DEMO_MODE=false"
Write-Host "VITE_APP_ENV=prod"
Write-Host "VITE_STRICT_CONTRACTS=true"
Write-Host ""

# ===================================
# STEP 4: VERCEL ADMIN UI
# ===================================
Write-Host "STEP 4: Vercel Admin UI Environment Variables" -ForegroundColor Yellow
Write-Host "-------------------------------" -ForegroundColor Yellow
Write-Host "Set these in Vercel Dashboard -> Settings -> Environment Variables:"
Write-Host ""
Write-Host "VITE_API_BASE=" -NoNewline
Write-Host "<YOUR_RENDER_URL>/api" -ForegroundColor Red
Write-Host "  Example: https://psy-api-xxx.onrender.com/api"
Write-Host "  NOTE: Admin UI uses VITE_API_BASE (no _URL suffix)" -ForegroundColor Magenta
Write-Host ""
Write-Host "VITE_DEMO_MODE=false"
Write-Host "VITE_APP_ENV=prod"
Write-Host "VITE_STRICT_CONTRACTS=true"
Write-Host ""

# ===================================
# STEP 5: UPDATE CORS
# ===================================
Write-Host "STEP 5: Update CORS After Frontend Deployment" -ForegroundColor Yellow
Write-Host "-------------------------------" -ForegroundColor Yellow
Write-Host "After deploying both frontends to Vercel:"
Write-Host "1. Note your Vercel URLs:"
Write-Host "   User UI:  https://psy-user-ui-xxx.vercel.app"
Write-Host "   Admin UI: https://psy-admin-ui-xxx.vercel.app"
Write-Host ""
Write-Host "2. Go to Render Dashboard -> psy-api-backend -> Environment"
Write-Host "3. Update CORS_ALLOWED_ORIGINS:"
Write-Host "   CORS_ALLOWED_ORIGINS=https://user-ui-xxx.vercel.app,https://admin-ui-xxx.vercel.app"
Write-Host ""
Write-Host "4. Trigger manual redeploy of backend"
Write-Host ""

# ===================================
# VERIFICATION
# ===================================
Write-Host "VERIFICATION CHECKLIST" -ForegroundColor Yellow
Write-Host "-------------------------------" -ForegroundColor Yellow
Write-Host "[ ] Backend deployed to Render"
Write-Host "[ ] Health check returns 200: curl https://<your-render>.onrender.com/health"
Write-Host "[ ] User UI deployed to Vercel"
Write-Host "[ ] Admin UI deployed to Vercel"
Write-Host "[ ] CORS updated with both Vercel URLs"
Write-Host "[ ] Backend redeployed after CORS update"
Write-Host "[ ] User UI loads without errors"
Write-Host "[ ] Admin UI loads without errors"
Write-Host "[ ] Can login and complete exam"
Write-Host "[ ] Results display in admin dashboard"
Write-Host "[ ] PDF generation works"
Write-Host ""

Write-Host "=== DEPLOYMENT READY ===" -ForegroundColor Green
Write-Host ""
Write-Host "Follow the complete guide in DEPLOYMENT_GUIDE_RENDER_VERCEL.md" -ForegroundColor Cyan
Write-Host "Good luck! 🚀" -ForegroundColor Cyan
