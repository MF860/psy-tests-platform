# Vercel Deployment Guide

## Overview
This guide walks you through deploying both frontend applications (User UI and Admin UI) to Vercel.

## Prerequisites

- [Vercel Account](https://vercel.com/signup) (free tier available)
- Backend deployed to Render (see render.yaml)
- GitHub/GitLab repository connected to Vercel

## Part 1: Deploy User UI

### Step 1: Import Project

1. Go to [Vercel Dashboard](https://vercel.com/dashboard)
2. Click **Add New** → **Project**
3. Select your repository: `psy-tests-platform`
4. Click **Import**

### Step 2: Configure User UI Build

In the project configuration screen:

**Framework Preset:** Vite  
**Root Directory:** `frontend/user-ui`  
**Build Command:** `npm run build`  
**Output Directory:** `dist`  
**Install Command:** `npm install`

### Step 3: Environment Variables

Click **Environment Variables** and add:

| Name | Value | Environment |
|------|-------|-------------|
| `VITE_API_BASE_URL` | `https://your-backend.onrender.com/api` | Production, Preview |
| `VITE_DEMO_MODE` | `false` | Production, Preview |

> ⚠️ **Important**: Replace `your-backend.onrender.com` with your actual Render backend URL

### Step 4: Deploy

1. Click **Deploy**
2. Wait 2-3 minutes for build to complete
3. Your User UI will be available at: `https://your-project.vercel.app`

### Step 5: Custom Domain (Optional)

1. Go to Project Settings → **Domains**
2. Add your custom domain (e.g., `psy-user.yourdomain.com`)
3. Follow DNS configuration instructions
4. Update CORS in backend `Program.cs` with your domain

---

## Part 2: Deploy Admin UI

### Step 1: Create Second Project

1. In Vercel Dashboard, click **Add New** → **Project**
2. Select same repository: `psy-tests-platform`
3. Click **Import**

### Step 2: Configure Admin UI Build

**Framework Preset:** Vite  
**Root Directory:** `frontend/admin-ui`  
**Build Command:** `npm run build`  
**Output Directory:** `dist`  
**Install Command:** `npm install`

### Step 3: Environment Variables

| Name | Value | Environment |
|------|-------|-------------|
| `VITE_API_BASE` | `https://your-backend.onrender.com/api` | Production, Preview |
| `VITE_DEMO_MODE` | `false` | Production, Preview |

### Step 4: Deploy

1. Click **Deploy**
2. Wait 2-3 minutes for build
3. Admin UI available at: `https://your-admin-project.vercel.app`

---

## Part 3: Update Backend CORS

After both UIs are deployed, update backend CORS configuration:

### Option A: Edit Program.cs (Recommended)

```csharp
// In Program.cs, replace TODO sections with actual domains
if (!builder.Environment.IsDevelopment())
{
    origins.Add("https://your-user-ui.vercel.app");
    origins.Add("https://your-admin-ui.vercel.app");
    
    // For custom domains:
    // origins.Add("https://psy-user.yourdomain.com");
    // origins.Add("https://psy-admin.yourdomain.com");
}
```

### Option B: Environment Variable Approach

Add to render.yaml:
```yaml
- key: ALLOWED_ORIGINS
  value: "https://your-user-ui.vercel.app,https://your-admin-ui.vercel.app"
```

Then update Program.cs to read from environment.

### Redeploy Backend

```bash
git add backend/PsyApi/Program.cs
git commit -m "chore: update CORS for Vercel deployments"
git push
```

Render will auto-deploy the changes.

---

## Part 4: Testing Deployment

### Test User UI

1. Visit your User UI URL
2. Open browser DevTools (F12) → Console
3. Start a test session
4. Check Network tab for API calls to Render backend
5. Verify no CORS errors

### Test Admin UI

1. Visit your Admin UI URL
2. Login with admin credentials
3. Check Analytics dashboard loads
4. Verify results list displays
5. Test PDF download

### Common Issues

#### CORS Errors

**Symptom:** `Access-Control-Allow-Origin` errors in console  
**Solution:** 
- Verify backend CORS includes your Vercel domains
- Check `AllowCredentials()` is set
- Ensure no trailing slashes in URLs

#### API Connection Failed

**Symptom:** Network errors, timeout errors  
**Solution:**
- Check `/health` endpoint: `https://your-backend.onrender.com/health`
- Verify Render service is running (not sleeping)
- Check environment variables in Vercel match backend URL

#### Build Failures

**Symptom:** Vercel build fails with TypeScript errors  
**Solution:**
- Run `npm run type-check` locally first
- Fix TypeScript errors before deploying
- Check `tsconfig.json` is correct

#### 404 on Refresh

**Symptom:** Refreshing page returns 404  
**Solution:**
- Vercel auto-handles this for Vite apps
- If using custom routing, add `vercel.json`:

```json
{
  "rewrites": [
    { "source": "/(.*)", "destination": "/index.html" }
  ]
}
```

---

## Part 5: Performance Optimization

### Enable Edge Caching

In Vercel project settings:

1. Go to **Settings** → **General**
2. Enable **Edge Caching** for static assets
3. Set cache duration: 1 year for fonts/images

### Image Optimization

Vercel automatically optimizes images. Use:

```typescript
<img src="/logo.png" /> // Auto-optimized
```

### Bundle Analysis

```bash
cd frontend/user-ui
npm run build:analyze
```

Review bundle size and optimize imports.

---

## Part 6: Environment Management

### Development

Use `.env` for local development:
```bash
VITE_API_BASE_URL=http://localhost:5019/api
VITE_DEMO_MODE=false
```

### Preview (Staging)

Vercel creates preview deployments for every PR:
- Each PR gets unique URL
- Uses Preview environment variables
- Ideal for testing before production

### Production

Production branch (main/master) deploys to production:
- Uses Production environment variables
- Custom domain (if configured)
- Stable, versioned deployments

---

## Part 7: Monitoring & Analytics

### Vercel Analytics

1. Enable Vercel Analytics in project settings
2. View real-time traffic, performance
3. Track Core Web Vitals (LCP, FID, CLS)

### Error Tracking

Integrate Sentry (optional):

```bash
npm install @sentry/react
```

Configure in `main.tsx`:

```typescript
import * as Sentry from '@sentry/react';

Sentry.init({
  dsn: "your-sentry-dsn",
  environment: import.meta.env.MODE,
});
```

---

## Part 8: Continuous Deployment

### Auto-Deploy on Push

Vercel automatically deploys on git push to:
- **main/master** → Production
- **feature branches** → Preview deployments
- **PRs** → Preview with unique URL

### Deploy Hooks

Create deploy hooks for manual triggers:

1. Go to **Settings** → **Git**
2. Create **Deploy Hook**
3. Trigger via API:

```bash
curl -X POST https://api.vercel.com/v1/integrations/deploy/xxx/yyy
```

---

## Part 9: Production Checklist

Before going live:

- [ ] Backend health check returns 200: `/health`
- [ ] Database migrations completed successfully
- [ ] SDJ mode enabled: `USE_SDJ=1`
- [ ] Both UIs deployed and accessible
- [ ] Admin login works
- [ ] User session flow completes end-to-end
- [ ] PDF reports generate correctly
- [ ] AI analysis works (if DeepSeek key configured)
- [ ] CORS configured for production domains
- [ ] Environment variables set correctly
- [ ] Custom domains configured (if applicable)
- [ ] SSL certificates active (auto via Vercel)
- [ ] Analytics tracking enabled
- [ ] Error monitoring configured
- [ ] Backup strategy for Neon database

---

## Part 10: Cost Optimization

### Vercel Free Tier

- 100 GB bandwidth/month
- Unlimited personal projects
- Automatic HTTPS
- Global CDN

### Upgrade Triggers

Consider upgrading to Pro ($20/mo) when:
- Traffic > 100 GB/month
- Need team collaboration
- Require password-protected previews
- Want advanced analytics

### Render Free Tier

- Service sleeps after 15 min inactivity
- 750 hours/month free
- Cold start: 30-60 seconds

Upgrade to Starter ($7/mo) for:
- Always-on service
- No cold starts
- Faster builds

---

## Support & Resources

- [Vercel Documentation](https://vercel.com/docs)
- [Vite Deployment Guide](https://vitejs.dev/guide/static-deploy.html)
- [Render Node.js Deploy](https://render.com/docs/deploy-node-express-app)
- Community: [Vercel Discord](https://vercel.com/discord)

---

## Troubleshooting Contact

If you encounter issues:

1. Check `/health` endpoint on backend
2. Review Vercel build logs
3. Check Render logs for backend errors
4. Verify environment variables match
5. Test API directly with curl/Postman

**Quick Health Check:**
```bash
curl https://your-backend.onrender.com/health
# Should return: {"status":"healthy","useSdj":true}
```

---

## Next Steps

After successful deployment:

1. Run smoke tests (see `docs/GO_LIVE_SMOKE.md`)
2. Monitor error rates for 24 hours
3. Set up alerts for downtime
4. Create database backup schedule
5. Document any custom configuration
6. Share URLs with stakeholders
7. Update project README with live URLs
