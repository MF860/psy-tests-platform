# 🚀 QUICK START GUIDE - Platform Modernization

## Prerequisites
- Node.js 18+ installed
- npm or yarn package manager
- .NET 8.0 SDK (for backend)
- Git

---

## Step 1: Install Frontend Dependencies

### Admin UI
```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\frontend\admin-ui
npm install
```

### User UI
```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\frontend\user-ui
npm install
```

**Expected time:** 2-3 minutes per UI

---

## Step 2: Integrate Theme Provider

### Admin UI - Update `main.tsx`

Open: `frontend/admin-ui/src/main.tsx`

```typescript
import React from 'react'
import ReactDOM from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from './components/theme-provider'
import { Toaster } from 'sonner'
import App from './App.tsx'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
      cacheTime: 1000 * 60 * 30,
      refetchOnWindowFocus: false,
      retry: 1
    }
  }
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider defaultTheme="light" storageKey="admin-ui-theme">
        <App />
        <Toaster position="top-center" richColors dir="rtl" />
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>,
)
```

### User UI - Update `main.tsx`

Open: `frontend/user-ui/src/main.tsx`

```typescript
import React from 'react'
import ReactDOM from 'react-dom/client'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ThemeProvider } from './components/theme-provider'
import { Toaster } from 'sonner'
import App from './App'
import './index.css'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5,
      cacheTime: 1000 * 60 * 30,
      refetchOnWindowFocus: false,
      retry: 1
    }
  }
})

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <QueryClientProvider client={queryClient}>
      <ThemeProvider defaultTheme="light" storageKey="user-ui-theme">
        <App />
        <Toaster position="top-center" richColors dir="rtl" />
      </ThemeProvider>
    </QueryClientProvider>
  </React.StrictMode>,
)
```

---

## Step 3: Add Theme Toggle to Layouts

### Admin UI - Update `AdminLayout.tsx`

Open: `frontend/admin-ui/src/components/admin/AdminLayout.tsx`

Add to the header/navbar:

```typescript
import { ThemeToggle } from '../ui/theme-toggle'

// In your header component
<div className="flex items-center gap-4">
  {/* Other header items */}
  <ThemeToggle />
</div>
```

### User UI - Update Navigation

Open: `frontend/user-ui/src/components/layout/` (your layout component)

Add theme toggle to appropriate location.

---

## Step 4: Backend - Install QuestPDF (PDF Generation)

```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi
dotnet add package QuestPDF --version 2024.3.0
dotnet restore
```

---

## Step 5: Test the Setup

### Start Backend
```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\backend\PsyApi
dotnet run
```

Backend should start on: `https://localhost:7071` (or configured port)

### Start Admin UI
```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\frontend\admin-ui
npm run dev
```

Admin UI should start on: `http://localhost:5173`

### Start User UI
```powershell
cd c:\Users\ASUS\Desktop\saitest\psy-tests-platform\frontend\user-ui
npm run dev
```

User UI should start on: `http://localhost:5174`

---

## Step 6: Verify Theme System

1. Open Admin UI in browser
2. Look for theme toggle button (sun/moon icon)
3. Click to switch between light/dark modes
4. Refresh page - theme should persist

---

## Step 7: Test AI Chat Assistant

### Create AI Chat Endpoint (Backend)

Create: `backend/PsyApi/Controllers/AiChatController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PsyApi.Services.AI;

namespace PsyApi.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/admin/ai")]
    public class AiChatController : ControllerBase
    {
        private readonly IAiAnalyzerService _ai;

        public AiChatController(IAiAnalyzerService ai)
        {
            _ai = ai;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                // Simple implementation - extend based on your AI service
                var response = await _ai.GenerateResponse(request.Message, request.ResultId);
                return Ok(new { response });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "فشل في معالجة الطلب" });
            }
        }
    }

    public class ChatRequest
    {
        public int ResultId { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<ChatMessage>? History { get; set; }
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
```

### Use AI Chat in Result Detail Page

Open: `frontend/admin-ui/src/pages/ResultDetail.tsx`

Add:

```typescript
import { useState } from 'react'
import { MessageCircle } from 'lucide-react'
import { Button } from '../components/ui/button'
import { Dialog, DialogContent, DialogTrigger } from '../components/ui/dialog'
import AIChatAssistant from '../components/ai/AIChatAssistant'

// In your component
const [showChat, setShowChat] = useState(false)

// Add button
<Button 
  onClick={() => setShowChat(true)}
  className="gap-2"
>
  <MessageCircle className="h-4 w-4" />
  مساعد AI
</Button>

// Add dialog
<Dialog open={showChat} onOpenChange={setShowChat}>
  <DialogContent className="max-w-3xl max-h-[90vh] p-0">
    <AIChatAssistant 
      resultId={resultId} 
      onClose={() => setShowChat(false)} 
    />
  </DialogContent>
</Dialog>
```

---

## Step 8: Download Lottie Animations

Visit: https://lottiefiles.com/

Download these animations (free):
1. **Brain/AI Processing** - Search: "brain ai"
2. **Success/Celebration** - Search: "success confetti"
3. **Document/PDF** - Search: "document loading"
4. **Loading Spinner** - Search: "loading modern"

Save to:
- `frontend/admin-ui/src/assets/lottie/`
- `frontend/user-ui/src/assets/lottie/`

Example usage:
```typescript
import brainAnimation from '../assets/lottie/brain.json'
import { LottieLoader } from '../components/ui/lottie-loader'

<LottieLoader animationData={brainAnimation} size="lg" />
```

---

## Step 9: Add Dialog Component (if missing)

Create: `frontend/admin-ui/src/components/ui/dialog.tsx`

```typescript
import * as React from "react"
import * as DialogPrimitive from "@radix-ui/react-dialog"
import { X } from "lucide-react"
import { cn } from "../../lib/utils"

const Dialog = DialogPrimitive.Root
const DialogTrigger = DialogPrimitive.Trigger
const DialogPortal = DialogPrimitive.Portal
const DialogClose = DialogPrimitive.Close

const DialogOverlay = React.forwardRef<
  React.ElementRef<typeof DialogPrimitive.Overlay>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Overlay>
>(({ className, ...props }, ref) => (
  <DialogPrimitive.Overlay
    ref={ref}
    className={cn(
      "fixed inset-0 z-50 bg-background/80 backdrop-blur-sm data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0",
      className
    )}
    {...props}
  />
))
DialogOverlay.displayName = DialogPrimitive.Overlay.displayName

const DialogContent = React.forwardRef<
  React.ElementRef<typeof DialogPrimitive.Content>,
  React.ComponentPropsWithoutRef<typeof DialogPrimitive.Content>
>(({ className, children, ...props }, ref) => (
  <DialogPortal>
    <DialogOverlay />
    <DialogPrimitive.Content
      ref={ref}
      className={cn(
        "fixed left-[50%] top-[50%] z-50 grid w-full max-w-lg translate-x-[-50%] translate-y-[-50%] gap-4 border bg-background p-6 shadow-lg duration-200 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[state=closed]:slide-out-to-left-1/2 data-[state=closed]:slide-out-to-top-[48%] data-[state=open]:slide-in-from-left-1/2 data-[state=open]:slide-in-from-top-[48%] sm:rounded-lg",
        className
      )}
      {...props}
    >
      {children}
      <DialogPrimitive.Close className="absolute right-4 top-4 rounded-sm opacity-70 ring-offset-background transition-opacity hover:opacity-100 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:pointer-events-none data-[state=open]:bg-accent data-[state=open]:text-muted-foreground">
        <X className="h-4 w-4" />
        <span className="sr-only">Close</span>
      </DialogPrimitive.Close>
    </DialogPrimitive.Content>
  </DialogPortal>
))
DialogContent.displayName = DialogPrimitive.Content.displayName

export { Dialog, DialogPortal, DialogOverlay, DialogClose, DialogTrigger, DialogContent }
```

---

## Step 10: Verify Everything Works

### Checklist:
- [ ] Admin UI loads without errors
- [ ] User UI loads without errors
- [ ] Theme toggle works
- [ ] Dark mode persists after refresh
- [ ] Sonner toasts appear correctly
- [ ] AI Chat component renders
- [ ] No console errors

---

## Common Issues & Solutions

### Issue: "Cannot find module '@radix-ui/...'"
**Solution:** Run `npm install` again in the respective UI folder

### Issue: Theme toggle doesn't show
**Solution:** Make sure you integrated ThemeProvider in main.tsx

### Issue: Sonner toasts don't appear
**Solution:** Ensure `<Toaster />` is added in main.tsx

### Issue: TypeScript errors
**Solution:** Run `npm run build` to check for actual errors

---

## Next Steps

1. ✅ Complete Steps 1-10
2. 🔄 Implement PDF generation backend
3. 🔄 Migrate Dashboard charts to ApexCharts
4. 🔄 Add Lottie animations throughout
5. 🔄 Enhance User Portal UI
6. 🔄 Performance optimizations

---

## Need Help?

Check the detailed plan:
- `MODERNIZATION_PLAN.md` - Full implementation guide

---

**Last Updated:** October 11, 2025
