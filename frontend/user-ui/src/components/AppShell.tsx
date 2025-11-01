import { Outlet, useLocation } from "react-router-dom";
import { useEffect, useState } from "react";
import { Clock } from "lucide-react";
import { formatTime, formatProgress, strings } from "../lib/strings";
import { sessionStorage } from "../lib/session";

interface AppShellProps {
  children?: React.ReactNode;
}

const EXAM_DURATION_SECONDS = 60 * 60; // 60 minutes

export default function AppShell({ children }: AppShellProps) {
  const location = useLocation();
  const [globalTimeLeft, setGlobalTimeLeft] = useState<number | null>(null);
  const [currentQuestionIndex, setCurrentQuestionIndex] = useState(0);
  const [totalQuestions, setTotalQuestions] = useState<number | null>(null);

  const isExamPage = location.pathname === '/exam';
  // Disable AppShell header on exam page - ExamNew has its own complete header
  const showHeader = false; // isExamPage;

  useEffect(() => {
    // Initialize global timer when exam starts
    if (!isExamPage) {
      setGlobalTimeLeft(null);
      return;
    }

    const session = sessionStorage.get();
    if (session?.startedAt) {
      const calculateRemaining = () => {
        const startTime = new Date(session.startedAt).getTime();
        const now = Date.now();
        const elapsed = Math.floor((now - startTime) / 1000);
        return Math.max(0, EXAM_DURATION_SECONDS - elapsed);
      };
      
      // Set initial time
      const initialRemaining = calculateRemaining();
      setGlobalTimeLeft(initialRemaining);
      
      // Auto-submit if time already expired
      if (initialRemaining === 0) {
        handleAutoSubmit();
      }
    }
  }, [isExamPage]);

  useEffect(() => {
    if (!isExamPage || globalTimeLeft === null) return;
    
    // Timer countdown logic
    if (globalTimeLeft <= 0) {
      handleAutoSubmit();
      return;
    }
    
    const interval = setInterval(() => {
      setGlobalTimeLeft(prev => {
        if (prev === null || prev <= 0) {
          clearInterval(interval);
          handleAutoSubmit();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, [isExamPage, globalTimeLeft !== null]);

  // Auto-submission handler
  const handleAutoSubmit = async () => {
    const session = sessionStorage.get();
    if (!session) return;
    
    try {
      console.log('[Timer] Auto-submitting exam due to time expiration');
      // Navigate to finish page which will handle submission
      window.location.href = '/finish';
    } catch (error) {
      console.error('[Timer] Auto-submit failed:', error);
    }
  };

  useEffect(() => {
    // Listen for progress updates from the exam component
    const handleProgressUpdate = (event: CustomEvent) => {
      setCurrentQuestionIndex(event.detail.currentIndex);
      setTotalQuestions(event.detail.totalQuestions);
    };

    window.addEventListener('exam-progress-update' as any, handleProgressUpdate);
    return () => {
      window.removeEventListener('exam-progress-update' as any, handleProgressUpdate);
    };
  }, []);

  const progressPercent = totalQuestions && totalQuestions > 0 
    ? Math.min(((currentQuestionIndex + 1) / totalQuestions) * 100, 100) 
    : 0;

  return (
    <div className="min-h-screen bg-background">
      {showHeader && (
        <header className="bg-card border-b border-border sticky top-0 z-10">
          <div className="max-w-4xl mx-auto px-4 py-3">
            {/* Top row: Timer and Progress */}
            <div className="flex justify-between items-center mb-3">
              <div className="flex items-center gap-2 text-sm font-medium">
                <Clock className="h-4 w-4 text-muted-foreground" />
                <span className="text-muted-foreground">{strings.exam.globalTimer}:</span>
                <span 
                  className={`font-mono ${
                    globalTimeLeft !== null && globalTimeLeft < 300 
                      ? 'text-destructive' 
                      : 'text-foreground'
                  }`}
                >
                  {globalTimeLeft !== null ? formatTime(globalTimeLeft) : '--:--'}
                </span>
              </div>
              
              {totalQuestions && (
                <div className="text-sm font-medium">
                  {formatProgress(currentQuestionIndex + 1, totalQuestions)}
                </div>
              )}
            </div>

            {/* Progress bar */}
            {totalQuestions && totalQuestions > 0 && (
              <div className="w-full bg-muted rounded-full h-2">
                <div 
                  className="bg-primary h-2 rounded-full transition-all duration-300 ease-out" 
                  style={{ width: `${progressPercent}%` }} 
                />
              </div>
            )}

            {/* Breadcrumb dots (disabled for now) */}
          </div>
        </header>
      )}

      <main className={showHeader ? "pt-0" : ""}>
        {children || <Outlet />}
      </main>

      {/* Global exam time warning */}
      {isExamPage && globalTimeLeft !== null && globalTimeLeft <= 300 && globalTimeLeft > 0 && (
        <div className="fixed top-20 sm:top-24 left-1/2 transform -translate-x-1/2 z-50 bg-destructive text-destructive-foreground px-4 py-2 rounded-lg shadow-lg animate-pulse">
          <div className="flex items-center gap-2 text-sm font-medium">
            <Clock className="h-4 w-4" />
            <span>تحذير: باقي {Math.floor(globalTimeLeft / 60)} دقائق على انتهاء الاختبار</span>
          </div>
        </div>
      )}
    </div>
  );
}