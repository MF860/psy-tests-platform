import { BrowserRouter as Router, Routes, Route, Navigate } from "react-router-dom";
import { useEffect } from "react";
import AppShell from "./components/AppShell";
import DemoBadge from "./components/DemoBadge";
import { sessionStorage } from "./lib/session";
import { useUserStore } from "./store/user";
import { createLazyComponent, performanceMetrics } from "./lib/performance";

// Performance monitoring - mark app start
performanceMetrics.mark('app-start')

// Always-loaded components (critical path)
import Login from "./pages/Login";
import ApiTest from "./debug/ApiTest";

// Lazy-loaded page components with custom loading states
const Privacy = createLazyComponent(
  () => import('./pages/Privacy'),
  {
    fallback: <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        <p className="text-muted-foreground">جاري التحميل...</p>
      </div>
    </div>
  }
)

const Instructions = createLazyComponent(
  () => import('./pages/Instructions'),
  {
    fallback: <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        <p className="text-muted-foreground">جاري تحميل التعليمات...</p>
      </div>
    </div>
  }
)

const ExamNew = createLazyComponent(
  () => import('./pages/ExamNew'),
  {
    fallback: <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        <p className="text-muted-foreground">جاري تحميل الاختبار...</p>
      </div>
    </div>
  }
)

const Finish = createLazyComponent(
  () => import('./pages/Finish'),
  {
    fallback: <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto mb-4"></div>
        <p className="text-muted-foreground">جاري التحميل...</p>
      </div>
    </div>
  }
)

function App() {
  // Global session clearing effect
  useEffect(() => {
    if (typeof window !== 'undefined' && window.location.search.includes('clear=1')) {
      console.log('Clearing all session data...');
      sessionStorage.clear();
      localStorage.clear();
      const { clearSession } = useUserStore.getState();
      clearSession();
      // Remove the query parameter
      window.history.replaceState({}, '', window.location.pathname);
    }
  }, []);
  return (
    <Router>
      <div className="min-h-screen bg-background text-foreground font-noto-arabic" dir="rtl">
        <DemoBadge />
        <AppShell>
          <Routes>
            {/* Redirect root to login */}
            <Route path="/" element={<Navigate to="/login" replace />} />
            
            {/* Debug route for API testing */}
            <Route path="/debug" element={<ApiTest />} />
            
            {/* All pages now use GuardedRoute internally */}
            <Route path="/login" element={<Login />} />
            <Route path="/privacy" element={<Privacy />} />
            <Route path="/instructions" element={<Instructions />} />
            <Route path="/exam" element={<ExamNew />} />
            <Route path="/thank-you" element={<Finish />} />
            
            {/* Legacy route redirects */}
            <Route path="/finish" element={<Navigate to="/thank-you" replace />} />
            <Route path="/thankyou" element={<Navigate to="/thank-you" replace />} />
          </Routes>
        </AppShell>
      </div>
    </Router>
  );
}

export default App;