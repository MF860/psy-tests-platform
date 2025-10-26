# 🏗️ Production Architecture - Render + Vercel + Neon

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        PRODUCTION ARCHITECTURE                          │
│                     PSY Tests Platform (SDJ Enabled)                    │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────┐          ┌──────────────────┐          ┌──────────────────┐
│                  │          │                  │          │                  │
│   USER BROWSER   │          │   USER BROWSER   │          │   ADMIN BROWSER  │
│                  │          │                  │          │                  │
│  (Arabic RTL)    │          │  (Testing Flow)  │          │  (Admin Portal)  │
└────────┬─────────┘          └────────┬─────────┘          └────────┬─────────┘
         │                              │                              │
         │ HTTPS                        │ HTTPS                        │ HTTPS
         │                              │                              │
         ▼                              ▼                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           VERCEL EDGE NETWORK                           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌────────────────────────────┐      ┌────────────────────────────┐   │
│  │  USER UI (Vite/React)      │      │  ADMIN UI (Vite/React)     │   │
│  │  -------------------------  │      │  -------------------------  │   │
│  │  • Arabic RTL Interface    │      │  • Dashboard & Analytics   │   │
│  │  • Login (National ID)     │      │  • Results Management      │   │
│  │  • Exam Timer (60 min)     │      │  • SDJ Charts & Reports    │   │
│  │  • 80 SDJ Questions        │      │  • PDF Generation          │   │
│  │  • Progress Tracking       │      │  • User Management         │   │
│  │                            │      │                            │   │
│  │  URL: user-ui-xxx.vercel   │      │  URL: admin-ui-xxx.vercel  │   │
│  │  Env: VITE_API_BASE_URL    │      │  Env: VITE_API_BASE        │   │
│  │       VITE_DEMO_MODE=false │      │       VITE_DEMO_MODE=false │   │
│  └──────────────┬─────────────┘      └──────────────┬─────────────┘   │
│                 │                                    │                 │
└─────────────────┼────────────────────────────────────┼─────────────────┘
                  │                                    │
                  │ API Calls                          │ API Calls
                  │ (CORS Protected)                   │ (CORS Protected)
                  │                                    │
                  └────────────────┬───────────────────┘
                                   │
                                   ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         RENDER WEB SERVICE                              │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐ │
│  │  BACKEND API (.NET 8 / ASP.NET Core)                              │ │
│  │  ---------------------------------------------------------------  │ │
│  │                                                                   │ │
│  │  📍 Endpoints:                                                    │ │
│  │     GET  /health              → System health check              │ │
│  │     POST /api/sessions/start  → Create test session              │ │
│  │     GET  /api/sessions/{id}/next → Get next SDJ question         │ │
│  │     POST /api/sessions/{id}/answer → Submit answer               │ │
│  │     POST /api/sessions/{id}/submit → Finish & score (IRT/PCM)    │ │
│  │     GET  /api/admin/results   → Results list                     │ │
│  │     GET  /api/admin/results/{id} → Result detail (with sdjData)  │ │
│  │     POST /api/admin/results/{id}/pdf → Generate PDF report       │ │
│  │                                                                   │ │
│  │  🔧 Features:                                                     │ │
│  │     • SDJ Mode (USE_SDJ=1)                                        │ │
│  │     • IRT/PCM Scoring Engine                                      │ │
│  │     • T-Score Normalization                                       │ │
│  │     • Rate Limiting (security)                                    │ │
│  │     • JWT Authentication                                          │ │
│  │     • Arabic PDF Generation                                       │ │
│  │     • CORS Protection                                             │ │
│  │                                                                   │ │
│  │  📊 Data:                                                         │ │
│  │     • 125 SDJ Items (Likert scale)                                │ │
│  │     • 125 IRT/PCM Parameters                                      │ │
│  │     • 5 Dimensions × 5 Subdimensions                              │ │
│  │     • 10 Mock Users (testing)                                     │ │
│  │                                                                   │ │
│  │  🌐 URL: psy-api-xxx.onrender.com                                │ │
│  │  🔒 Port: 10000 (internal)                                        │ │
│  │                                                                   │ │
│  │  Environment Variables:                                           │ │
│  │    ASPNETCORE_ENVIRONMENT=Production                              │ │
│  │    USE_SDJ=1                                                      │ │
│  │    CORS_ALLOWED_ORIGINS=https://user-ui...,https://admin-ui...   │ │
│  │    ConnectionStrings__DefaultConnection=(see below)               │ │
│  └───────────────────────────┬───────────────────────────────────────┘ │
│                              │                                         │
└──────────────────────────────┼─────────────────────────────────────────┘
                               │
                               │ PostgreSQL Connection
                               │ (SSL Required)
                               ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        NEON POSTGRESQL DATABASE                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  🗄️  Database: neondb                                                  │
│  🌐 Host: ep-hidden-hat-a432u8az-pooler.us-east-1.aws.neon.tech        │
│  🔐 SSL Mode: Require                                                   │
│  📦 Storage: 3GB (Free Tier)                                            │
│  ⚡ Region: US East 1                                                   │
│  🔄 Auto-Backups: Enabled                                               │
│                                                                         │
│  Tables:                                                                │
│  ┌─────────────────────────────────────────────────────────────────┐  │
│  │  • Users            (10 rows - mock testing data)               │  │
│  │  • Admins           (1 row - 'root' admin)                      │  │
│  │  • Items            (125 rows - SDJ test items)                 │  │
│  │  • ItemParameters   (125 rows - IRT/PCM calibration)            │  │
│  │  • Sessions         (dynamic - user test sessions)              │  │
│  │  • SessionResponses (dynamic - individual answers)              │  │
│  │  • Results          (dynamic - completed test results)          │  │
│  │  • ResultSdjData    (dynamic - SDJ scoring output)              │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  Connection String:                                                     │
│  Host=ep-xxx...;Database=neondb;Username=neondb_owner;                 │
│  Password=<secret>;Ssl Mode=Require;Trust Server Certificate=true      │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘


═══════════════════════════════════════════════════════════════════════════
                              DATA FLOW EXAMPLE
═══════════════════════════════════════════════════════════════════════════

📱 USER FLOW (Exam Completion):

1. User opens https://user-ui-xxx.vercel.app
2. Enters National ID: 1000000001
3. Frontend calls POST https://psy-api-xxx.onrender.com/api/sessions/start
4. Backend creates session in Neon DB, returns sessionId
5. Frontend calls GET /api/sessions/{id}/next 80 times
6. Backend uses CAT algorithm to select optimal SDJ questions from Neon
7. User answers with Arabic options (لا أوافق, محايد, أوافق, etc.)
8. Frontend calls POST /api/sessions/{id}/answer for each response
9. Backend saves to Neon SessionResponses table
10. User clicks "Finish" → POST /api/sessions/{id}/submit
11. Backend runs IRT/PCM scoring engine:
    - Estimates theta per dimension
    - Calculates T-scores (mean=50, SD=10)
    - Assigns bands (Low/Below/Average/Above/High)
12. Backend saves Result + ResultSdjData to Neon
13. Frontend shows Thank You page

👨‍💼 ADMIN FLOW (View Results):

1. Admin opens https://admin-ui-xxx.vercel.app
2. Logs in with admin credentials
3. Frontend calls GET /api/admin/results
4. Backend queries Neon Results table, returns list
5. Admin clicks result → GET /api/admin/results/{id}
6. Backend joins Result + ResultSdjData from Neon
7. Frontend renders:
   - Horizontal bar chart (5 dimensions by T-score)
   - Radar chart (5 dimensions)
   - 5 donut charts (subdimensions per dimension)
8. Admin clicks "Generate PDF" → POST /api/admin/results/{id}/pdf
9. Backend generates PDF with charts & Arabic text
10. Browser downloads PDF report


═══════════════════════════════════════════════════════════════════════════
                            SECURITY & PERFORMANCE
═══════════════════════════════════════════════════════════════════════════

🔒 SECURITY MEASURES:

✅ SSL/TLS Encryption:
   • Vercel → Render: HTTPS only
   • Render → Neon: SSL Mode=Require
   • All traffic encrypted in transit

✅ CORS Protection:
   • Only specified Vercel domains allowed
   • No wildcard (*) in production
   • Credentials allowed for authenticated requests

✅ Rate Limiting:
   • Session creation: Max 5 per minute per IP
   • Prevents abuse and DoS attacks
   • Configurable in Program.cs

✅ Authentication:
   • Admin: JWT Bearer tokens
   • User: National ID validation
   • Passwords hashed (BCrypt)

✅ Input Validation:
   • All DTOs validated
   • SQL injection prevention (EF Core parameterization)
   • XSS prevention (React escaping)

⚡ PERFORMANCE OPTIMIZATIONS:

✅ Caching:
   • Vercel: Global CDN for static assets
   • Browser: Asset caching (1 year TTL)
   • Backend: Response compression (Brotli/Gzip)

✅ Database:
   • Neon: Connection pooling enabled
   • Indexed columns (UserId, SessionId, etc.)
   • Efficient queries with EF Core

✅ Frontend:
   • Code splitting (Vite automatic)
   • Lazy loading of charts
   • Optimized bundle size

📊 MONITORING:

🔍 Render:
   • Built-in logs (7-day retention)
   • Health check monitoring
   • Auto-restart on failure

🔍 Vercel:
   • Real-time function logs
   • Build & deploy notifications
   • Error tracking

🔍 Neon:
   • Query performance metrics
   • Storage usage monitoring
   • Backup history


═══════════════════════════════════════════════════════════════════════════
                         COST BREAKDOWN (Free Tier)
═══════════════════════════════════════════════════════════════════════════

💰 MONTHLY COSTS (Starting):

Render (Free):           $0/month
  • 512 MB RAM
  • Spins down after 15 min inactivity
  • 750 hours/month free
  • Upgrade to Starter: $7/month (always-on)

Vercel (Free):           $0/month
  • 100 GB bandwidth
  • Unlimited deployments
  • Automatic SSL
  • Global CDN

Neon (Free):             $0/month
  • 3 GB storage
  • Automatic backups
  • SSL included
  • Upgrade to Pro: $19/month (10 GB)

TOTAL (Free Tier):       $0/month ✅
TOTAL (Recommended):     $7/month (Render Starter only)


═══════════════════════════════════════════════════════════════════════════
                              DEPLOYMENT STATUS
═══════════════════════════════════════════════════════════════════════════

✅ Backend:    READY FOR DEPLOYMENT
✅ User UI:    READY FOR DEPLOYMENT  
✅ Admin UI:   READY FOR DEPLOYMENT
✅ Database:   CONFIGURED & TESTED
✅ CORS:       FLEXIBLE CONFIGURATION
✅ Docs:       COMPREHENSIVE GUIDES CREATED
✅ Scripts:    AUTOMATED VERIFICATION READY

🎯 Next Action: Follow DEPLOYMENT_GUIDE_RENDER_VERCEL.md

═══════════════════════════════════════════════════════════════════════════
