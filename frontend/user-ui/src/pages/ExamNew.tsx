import { useCallback, useEffect, useRef, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { cn } from "../lib/utils";
import { ApiError } from "../lib/api";
import { strings, formatTime } from "../lib/strings";
import { Button } from "../components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Badge } from "../components/ui/badge";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "../components/ui/dialog";
import { Timer, CheckCircle, AlertCircle, ArrowLeft, ArrowRight } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { getNextQuestion, submitAnswer, finishSession, submitSession } from "../lib/api-client";
import UserHeader from "../components/layout/UserHeader";
import ScreenContainer from "../components/layout/ScreenContainer";
import SectionCard from "../components/layout/SectionCard";
import AutosaveIndicator from "../components/Exam/AutosaveIndicator";
import { ProgressiveLoader } from "../components/ui/progressive-loader";
import { AnimatedEntrance, QuestionTransition, AnimatedProgress } from "../components/ui/micro-interactions";
import { TouchButton, SwipeableCard } from "../components/ui/mobile-responsive";

// Question components
import MCQ from "../components/Question/MCQ";
import Likert from "../components/Question/Likert";
import Frequency from "../components/Question/Frequency";
import Ordering from "../components/Question/Ordering";
import TimedNumeric from "../components/Question/TimedNumeric";
import TextAnswer from "../components/Question/TextAnswer";
import { normalizeApiQuestion } from "../lib/contract-bridge";
import { type UiQuestion } from "../lib/contracts";

type ApiQuestion = {
  id: number;
  item_id?: string;
  text_ar: string;
  type: string;
  dimension_tags?: string;
  difficulty?: number;
  time_limit_seconds: number;
  max_score: number;
  options?: string | null;
  orderingChoices?: string[];
  orderingLabels?: string[];
};

type CompletedResponse = { message: string };

// Use UiQuestion from contracts for type safety
type Question = UiQuestion;

function ExamContent() {
  const { nationalId, sessionId, setExamFinished, consented, instructionsCompleted } = useFlowState();

  const [questions, setQuestions] = useState<Question[]>([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [current, setCurrent] = useState<Question | null>(null);
  const [answersMap, setAnswersMap] = useState<Record<string, string>>({});
  const [isLoadingQuestion, setIsLoadingQuestion] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [questionTimeLeft, setQuestionTimeLeft] = useState<number | null>(null);
  const [sessionTimeLeft, setSessionTimeLeft] = useState<number>(3600); // 60 minutes = 3600 seconds
  const [showFinishDialog, setShowFinishDialog] = useState(false);
  const [showSessionWarning, setShowSessionWarning] = useState(false);
  const [sessionWarningMessage, setSessionWarningMessage] = useState<string>("");
  const [resumeBanner] = useState(false);
  const [autosaveState, setAutosaveState] = useState<"idle" | "saving" | "saved" | "error">("idle");
  
  const questionStartRef = useRef<number | null>(null);
  const autoSubmittingRef = useRef(false);
  const fetchingRef = useRef(false);
  const sessionAutoSubmittingRef = useRef(false);
  const { totalQuestions } = useFlowState();

  // Emit progress updates for AppShell
  const emitProgressUpdate = useCallback((currentIndex: number, totalQuestions: number) => {
    const event = new CustomEvent('exam-progress-update', {
      detail: { currentIndex, totalQuestions }
    });
    window.dispatchEvent(event);
  }, []);

  const setActiveQuestion = useCallback((q: Question | null) => {
    setCurrent(q);
    if (!q) {
      setQuestionTimeLeft(null);
      questionStartRef.current = null;
      return;
    }
    
    questionStartRef.current = Date.now();
    setQuestionTimeLeft(q.timeLimitSeconds > 0 ? q.timeLimitSeconds : null);
  }, []);

  const finalizeSession = useCallback(async () => {
    if (!sessionId) return;
    
    try {
      setIsSubmitting(true);
      setErrorMessage(null);

      await finishSession(sessionId);
      await new Promise(resolve => setTimeout(resolve, 500));

      await submitSession(sessionId);
      
      setExamFinished(true);
      // GuardedRoute will handle navigation to thank-you
    } catch (error) {
      console.error("Failed to submit session.", error);
      if (error instanceof ApiError) {
        setErrorMessage(`${error.message} (خطأ: ${error.status})`);
      } else {
        setErrorMessage(strings.exam.errors.finishingExam);
      }
    } finally {
      setIsSubmitting(false);
    }
  }, [sessionId, setExamFinished]);

  const fetchNextFromApi = useCallback(async (): Promise<boolean> => {
    if (!sessionId) {
      console.error('No sessionId for fetchNextFromApi');
      return false;
    }
    
    if (fetchingRef.current) {
      console.log('Already fetching, skipping duplicate request');
      return false;
    }
    
    fetchingRef.current = true;
    console.log('Fetching next question for session:', sessionId);
    setIsLoadingQuestion(true);
    setErrorMessage(null);
    
    try {
      const data = await getNextQuestion(sessionId);
      console.log('API Response raw:', data);
      console.log('API Response type:', typeof data);
      console.log('API Response keys:', data ? Object.keys(data) : 'no keys');
      
      if ((data as CompletedResponse).message === "completed" || !data) {
        console.log('Session completed');
        await finalizeSession();
        return false;
      }
      
      console.log('Before normalization - data:', JSON.stringify(data, null, 2));
      
      const normalized = normalizeApiQuestion(data as unknown as ApiQuestion);
      console.log('After normalization - normalized question:', JSON.stringify(normalized, null, 2));
      
      let newIndex = 0;
      setQuestions((prev) => {
        const exists = prev.some((p) => p.id === normalized.id);
        const list = exists ? prev : [...prev, normalized];
        const idx = list.findIndex((p) => p.id === normalized.id);
        newIndex = idx >= 0 ? idx : list.length - 1;
        setCurrentIndex(newIndex);
        console.log('Updated questions list, current index:', newIndex, 'total questions:', list.length);
        return list;
      });
      
      setActiveQuestion(normalized);
      console.log('Set active question:', normalized);
      
      // Emit progress update after state updates
      setTimeout(() => {
        emitProgressUpdate(newIndex, totalQuestions || 0);
      }, 0);
      
      return true;
    } catch (error) {
      console.error('fetchNextFromApi error (full):', error);
      console.error('fetchNextFromApi error type:', typeof error);
      console.error('fetchNextFromApi error name:', error?.constructor?.name);
      
      if (error instanceof Error) {
        console.error('Error message:', error.message);
        console.error('Error stack:', error.stack);
      }
      
      if (error instanceof ApiError) {
        console.error('ApiError status:', error.status);
        console.error('ApiError body:', error.body);
        if (error.status === 404 || error.status === 401) {
          console.log('Invalid session, clearing and redirecting to login');
          // Let GuardedRoute handle the redirect
          return false;
        }
        setErrorMessage(`API Error ${error.status}: ${error.message}`);
      } else {
        const errorMsg = error instanceof Error ? error.message : String(error);
        setErrorMessage(`Fetch Error: ${errorMsg}`);
        console.error('Setting error message:', `Fetch Error: ${errorMsg}`);
      }
      return false;
    } finally {
      setIsLoadingQuestion(false);
      fetchingRef.current = false;
    }
  }, [finalizeSession, sessionId, setActiveQuestion, emitProgressUpdate, totalQuestions]);

  const saveAnswerToStorage = useCallback((questionId: string, answer: string) => {
    // Save to sessionStorage for persistence
    const key = `answer_${sessionId}_${questionId}`;
    sessionStorage.setItem(key, answer);
    
    // Save progress
    const progressKey = `progress_${sessionId}`;
    sessionStorage.setItem(progressKey, currentIndex.toString());
  }, [currentIndex, sessionId]);

  const handleSubmitAnswer = useCallback(async () => {
    if (!sessionId || !current) return;
    
    const trimmed = (answersMap[current.id.toString()] ?? "").trim();
    if (!trimmed) return;
    
    setIsSubmitting(true);
    setErrorMessage(null);
    setAutosaveState("saving");
    
    try {
      await submitAnswer(sessionId, current, trimmed, questionStartRef.current ?? undefined);

      // Save answer to localStorage
      saveAnswerToStorage(current.id.toString(), trimmed);
      setAutosaveState("saved");

      await handleNext();
    } catch (error) {
      setAutosaveState("error");
      if (error instanceof ApiError) {
        if (error.message === "هذا السؤال تم الإجابة عليه مسبقاً") {
          await handleNext();
          return;
        }
        setErrorMessage(error.message);
      } else {
        setErrorMessage(strings.exam.errors.submittingAnswer);
      }
    } finally {
      setIsSubmitting(false);
    }
  }, [answersMap, current, sessionId, saveAnswerToStorage]);

  const handleNext = useCallback(async () => {
    if (!sessionId) return;
    
    const currentNum = currentIndex + 1;
    if (totalQuestions && currentNum >= totalQuestions && current) {
      const currentAnswer = answersMap[current.id.toString()]?.trim() ?? "";
      if (currentAnswer) {
        setShowFinishDialog(true);
        return;
      }
    }

    const nextIndex = currentIndex + 1;
    if (nextIndex < questions.length) {
      setCurrentIndex(nextIndex);
      setActiveQuestion(questions[nextIndex]);
      emitProgressUpdate(nextIndex, totalQuestions || 0);
      return;
    }
    
    await fetchNextFromApi();
  }, [currentIndex, fetchNextFromApi, questions, sessionId, setActiveQuestion, answersMap, current, totalQuestions, emitProgressUpdate]);

  const handlePrevious = useCallback(() => {
    if (currentIndex <= 0) return;
    
    const prevIndex = currentIndex - 1;
    setCurrentIndex(prevIndex);
    setActiveQuestion(questions[prevIndex] ?? null);
    emitProgressUpdate(prevIndex, totalQuestions || 0);
  }, [currentIndex, questions, setActiveQuestion, emitProgressUpdate, totalQuestions]);

  const handleAnswerChange = useCallback((value: string) => {
    if (!current) return;
    
    setAnswersMap((prev) => ({ 
      ...prev, 
      [current.id.toString()]: value 
    }));
    
    // Auto-save to localStorage with feedback
    setAutosaveState("saving");
    try {
      saveAnswerToStorage(current.id.toString(), value);
      setAutosaveState("saved");
      setTimeout(() => setAutosaveState("idle"), 2000);
    } catch (error) {
      setAutosaveState("error");
      setTimeout(() => setAutosaveState("idle"), 3000);
    }
  }, [current, saveAnswerToStorage]);

  const confirmFinish = async () => {
    setShowFinishDialog(false);
    
    if (current) {
      const currentAnswer = answersMap[current.id.toString()]?.trim() ?? "";
      if (currentAnswer) {
        try {
          await handleSubmitAnswer();
          await new Promise(resolve => setTimeout(resolve, 500));
          await finalizeSession();
        } catch (error) {
          console.error("Error finishing exam:", error);
          setErrorMessage(strings.exam.errors.finishingExam);
        }
      }
    }
  };

  // Initialize exam
  useEffect(() => {
    if (questions.length === 0 && sessionId && !isLoadingQuestion) {
      console.log('No questions loaded, fetching from API...', { sessionId, nationalId });
      
      fetchNextFromApi().then((success) => {
        if (!success) {
          console.error('Failed to fetch questions');
        }
      });
    }
  }, [sessionId, nationalId, fetchNextFromApi]);

  // Initialize session timer from storage or start at 60 minutes
  useEffect(() => {
    if (!sessionId) return;
    
    const startKey = `session_start_${sessionId}`;
    const storedStart = sessionStorage.getItem(startKey);
    
    if (storedStart) {
      // Resume from stored time
      const startTime = parseInt(storedStart, 10);
      const elapsed = Math.floor((Date.now() - startTime) / 1000);
      const remaining = Math.max(0, 3600 - elapsed);
      setSessionTimeLeft(remaining);
      console.log(`[SessionTimer] Resumed - ${Math.floor(remaining / 60)}:${String(remaining % 60).padStart(2, '0')} remaining`);
    } else {
      // First time - start timer (60 minutes)
      const startTime = Date.now();
      sessionStorage.setItem(startKey, startTime.toString());
      setSessionTimeLeft(3600); // 60 minutes
      console.log('[SessionTimer] Started - 60:00');
    }
  }, [sessionId]);

  // Session timer countdown
  useEffect(() => {
    if (sessionTimeLeft <= 0) return;
    
    const interval = setInterval(() => {
      setSessionTimeLeft((prev) => {
        const newTime = Math.max(0, prev - 1);
        
        // Show warnings at 5:00 and 1:00
        if (newTime === 300 && !showSessionWarning) {
          setSessionWarningMessage("⏰ تبقى 5 دقائق فقط على انتهاء الاختبار");
          setShowSessionWarning(true);
          setTimeout(() => setShowSessionWarning(false), 5000);
        } else if (newTime === 60 && !showSessionWarning) {
          setSessionWarningMessage("⏰ تبقت دقيقة واحدة فقط! سيتم إرسال الإجابات تلقائياً");
          setShowSessionWarning(true);
          setTimeout(() => setShowSessionWarning(false), 5000);
        }
        
        return newTime;
      });
    }, 1000);
    
    return () => clearInterval(interval);
  }, [sessionTimeLeft, showSessionWarning]);

  // Auto-submit when session time expires
  useEffect(() => {
    if (sessionTimeLeft !== 0 || sessionAutoSubmittingRef.current) return;
    
    sessionAutoSubmittingRef.current = true;
    
    (async () => {
      console.log('[SessionTimer] Time expired - auto-submitting session');
      
      // Try to submit current answer if exists
      if (current && answersMap[current.id.toString()]?.trim()) {
        try {
          await submitAnswer(sessionId!, current, answersMap[current.id.toString()].trim());
        } catch (error) {
          console.error('[SessionTimer] Failed to submit final answer', error);
        }
      }
      
      // Finalize session
      await finalizeSession();
    })();
  }, [sessionTimeLeft, current, answersMap, sessionId, finalizeSession]);

  // Question timer
  useEffect(() => {
    if (questionTimeLeft === null || questionTimeLeft <= 0) return;
    
    const interval = setInterval(() => {
      setQuestionTimeLeft((prev) => (prev !== null && prev > 0 ? prev - 1 : 0));
    }, 1000);
    
    return () => clearInterval(interval);
  }, [questionTimeLeft]);

  // Auto-submit when question time expires
  useEffect(() => {
    if (questionTimeLeft !== 0 || !current || autoSubmittingRef.current) return;
    
    autoSubmittingRef.current = true;
    
    (async () => {
      const value = (answersMap[current.id.toString()] ?? "").trim();
      if (value) {
        await handleSubmitAnswer();
      } else {
        await handleNext();
      }
      autoSubmittingRef.current = false;
    })();
  }, [questionTimeLeft, current, answersMap, handleSubmitAnswer, handleNext]);

  // Calculate states for UI
  const canSubmit = !!(current && (answersMap[current.id.toString()] ?? "").trim()) && !isSubmitting;
  const isLastQuestion = totalQuestions && (currentIndex + 1) === totalQuestions;
  const progressPercent = totalQuestions && totalQuestions > 0 ? Math.min(((currentIndex + 1) / totalQuestions) * 100, 100) : 0;

  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Ignore if user is typing in an input
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
      }

      // Next question (Enter)
      if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        if (canSubmit) {
          handleSubmitAnswer();
        } else if (!isSubmitting) {
          handleNext();
        }
      }
      
      // Previous question (Shift + Enter)
      if (event.key === 'Enter' && event.shiftKey) {
        event.preventDefault();
        handlePrevious();
      }

      // Numeric shortcuts for MCQ (1-9)
      if (current?.type === 'MCQ' && /^[1-9]$/.test(event.key)) {
        const optionIndex = parseInt(event.key) - 1;
        if (current.options && optionIndex < current.options.length) {
          event.preventDefault();
          handleAnswerChange(current.options[optionIndex].value);
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [canSubmit, handleSubmitAnswer, handleNext, handlePrevious, handleAnswerChange, current, isSubmitting]);

  const renderQuestionInput = () => {
    if (!current) {
      console.log('renderQuestionInput: No current question');
      return <div className="p-4 text-muted-foreground">لا يوجد سؤال للعرض</div>;
    }

    const commonProps = {
      value: answersMap[current.id.toString()] ?? "",
      onChange: handleAnswerChange,
      questionId: current.itemId,
    };

    switch (current.type) {
      case "LikertAgreement":
        return <Likert {...commonProps} options={current.options} />;
        
      case "Frequency":
        return <Frequency {...commonProps} options={current.options} />;
        
      case "MCQ":
        return <MCQ {...commonProps} options={current.options} />;
        
      case "ORDERING":
        return (
          <Ordering 
            {...commonProps} 
            choices={current.orderingChoices}
            labels={current.orderingLabels}
          />
        );
        
      case "TIMED_NUMERIC":
        return <TimedNumeric {...commonProps} />;
        
      case "TEXT":
      case "Text":
        return <TextAnswer {...commonProps} />;
        
      default:
        return <TextAnswer {...commonProps} />;
    }
  };

  // Single header component - avoid duplication
  const headerTitle = !isLoadingQuestion && !current && questions.length === 0 
    ? "الاختبار - تصحيح الأخطاء" 
    : "الاختبار";

  return (
    <div className="min-h-screen bg-gradient-to-b from-white to-slate-50" dir="rtl">
      {/* Single Global Header */}
      <UserHeader showStep stepLabel={headerTitle} />

      {/* Loading State */}
      {isLoadingQuestion && !current && (
        <main className="flex items-center justify-center min-h-[calc(100vh-80px)] p-4">
          <AnimatedEntrance animation="scaleIn">
            <Card className="w-full max-w-2xl rounded-2xl shadow-lg">
              <CardContent className="py-12">
                <ProgressiveLoader 
                  stage={isLoadingQuestion ? 'fetching' : 'complete'}
                  className="min-h-[200px]"
                >
                  <div className="text-center">
                    <p className="text-lg font-medium text-gray-900 mb-2">جاري تحضير السؤال التالي</p>
                    <p className="text-muted-foreground">الرجاء الانتظار...</p>
                  </div>
                </ProgressiveLoader>
              </CardContent>
            </Card>
          </AnimatedEntrance>
        </main>
      )}

      {/* Debug State */}
      {!isLoadingQuestion && !current && questions.length === 0 && (
        <main className="flex items-center justify-center min-h-[calc(100vh-80px)] p-4">
          <Card className="w-full max-w-4xl rounded-2xl shadow-lg">
            <CardContent className="py-12">
              <h2 className="text-xl font-bold text-red-600 mb-4">Debug: No Questions Loaded</h2>
              <div className="space-y-4 text-left" dir="ltr">
                <div>
                  <strong>Session ID:</strong> {sessionId || 'null'}
                </div>
                <div>
                  <strong>National ID:</strong> {nationalId || 'null'}
                </div>
                <div>
                  <strong>Questions Array Length:</strong> {questions.length}
                </div>
                <div>
                  <strong>Current Question:</strong> {current ? 'exists' : 'null'}
                </div>
                <div>
                  <strong>Is Loading:</strong> {isLoadingQuestion ? 'true' : 'false'}
                </div>
                <div>
                  <strong>Fetching Ref:</strong> {fetchingRef.current ? 'true' : 'false'}
                </div>
                <div>
                  <strong>Current Index:</strong> {currentIndex}
                </div>
                <div>
                  <strong>Flow State - Consented:</strong> {consented ? 'true' : 'false'}
                </div>
                <div>
                  <strong>Flow State - Instructions:</strong> {instructionsCompleted ? 'true' : 'false'}
                </div>
                <div>
                  <strong>Error Message:</strong> {errorMessage || 'none'}
                </div>
                {errorMessage && (
                  <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded">
                    {errorMessage}
                  </div>
                )}
                <button
                  onClick={() => sessionId && fetchNextFromApi()}
                  className="bg-blue-500 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded mt-4"
                  disabled={!sessionId || isLoadingQuestion}
                >
                  Retry Loading Question
                </button>
              </div>
            </CardContent>
          </Card>
        </main>
      )}

      {/* Main Exam Interface */}
      {current && (
        <>
          {/* Resume banner */}
      <AnimatePresence>
        {resumeBanner && (
          <motion.div
            initial={{ opacity: 0, y: -50 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -50 }}
            className="fixed top-16 sm:top-18 left-1/2 transform -translate-x-1/2 z-50 bg-blue-100 text-blue-800 px-3 sm:px-4 py-2 rounded-lg shadow-lg border border-blue-200 max-w-md mx-auto text-center text-sm"
          >
            {strings.exam.resume.banner}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Session warning banner */}
      <AnimatePresence>
        {showSessionWarning && (
          <motion.div
            initial={{ opacity: 0, y: -50 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0, y: -50 }}
            className="fixed top-16 sm:top-18 left-1/2 transform -translate-x-1/2 z-50 bg-orange-100 text-orange-900 px-4 py-3 rounded-lg shadow-lg border-2 border-orange-300 max-w-md mx-auto text-center text-sm font-semibold"
          >
            {sessionWarningMessage}
          </motion.div>
        )}
      </AnimatePresence>

      {/* Top Status Bar - Responsive */}
      <div className="sticky top-14 sm:top-16 z-10 bg-white/90 backdrop-blur supports-[backdrop-filter]:bg-white/70 border-b">
        <ScreenContainer maxWidth="4xl" className="py-3 sm:py-4">
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3 sm:gap-4">
            {/* Progress & Counter - Full width on mobile */}
            <div className="flex items-center gap-3 sm:gap-4 flex-1 min-w-0">
              <div className="flex-1 min-w-0">
                <AnimatedProgress 
                  value={progressPercent} 
                  max={100}
                  color="primary"
                  className="h-2 sm:h-2.5" 
                />
              </div>
              <motion.span 
                key={currentIndex}
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ duration: 0.2 }}
                className="text-xs sm:text-sm font-medium text-slate-600 whitespace-nowrap flex-shrink-0"
              >
                سؤال {currentIndex + 1} من {totalQuestions || "-"}
              </motion.span>
            </div>

            {/* Status Indicators - Responsive layout */}
            <div className="flex items-center justify-between sm:justify-end gap-2 sm:gap-3">
              {/* Session Timer - Always visible */}
              <div className={`
                flex items-center space-xs px-3 py-2 rounded-xl text-xs sm:text-sm font-bold transition-all duration-200 shadow-sm
                ${sessionTimeLeft <= 60 
                  ? 'bg-red-500 text-white animate-pulse' 
                  : sessionTimeLeft <= 300
                  ? 'bg-orange-100 text-orange-700 border border-orange-300' 
                  : 'bg-green-100 text-green-700 border border-green-200'
                }
              `}>
                <Timer className="h-4 w-4 flex-shrink-0" />
                <span className="font-mono tracking-wide">
                  {Math.floor(sessionTimeLeft / 60)}:{String(sessionTimeLeft % 60).padStart(2, '0')}
                </span>
              </div>
              
              <AutosaveIndicator state={autosaveState} />
              
              {/* Question Timer - Enhanced with clear warning states */}
              {questionTimeLeft !== null && (
                <div className={`
                  flex items-center space-xs px-3 py-2 rounded-xl text-xs sm:text-sm font-semibold transition-all duration-200 shadow-sm
                  ${questionTimeLeft <= 10 
                    ? 'bg-red-500 text-white animate-pulse' 
                    : questionTimeLeft <= 30
                    ? 'bg-red-100 text-red-700 border border-red-200' 
                    : 'bg-blue-100 text-blue-700 border border-blue-200'
                  }
                `}>
                  <Timer className="h-4 w-4 flex-shrink-0" />
                  <span className="font-mono tracking-wide">{formatTime(questionTimeLeft)}</span>
                </div>
              )}
            </div>
          </div>
        </ScreenContainer>
      </div>

      {/* Main Content - Responsive Container with Two-Column Layout */}
      <div style={{ paddingTop: '2rem' }}>
      <ScreenContainer maxWidth="4xl" className="py-4 sm:py-6 pb-24 sm:pb-32">
        <AnimatePresence mode="wait">
          <SwipeableCard
            onSwipeLeft={handleNext}
            onSwipeRight={handlePrevious}
            disabled={isSubmitting || currentIndex === 0}
          >
            <QuestionTransition
              questionId={current?.id || 'loading'}
              className="grid grid-cols-1 lg:grid-cols-12 gap-md"
            >
            {/* Question Content - Left Column (Right in RTL) */}
            <div className="lg:col-span-5 space-lg">
              <SectionCard className="shadow-md">
                <CardHeader className="space-md">
                  <div className="space-sm">
                    <CardTitle className="text-lg sm:text-xl lg:text-2xl leading-relaxed text-slate-900">
                      {current?.text ?? "لا يوجد نص للسؤال"}
                    </CardTitle>
                    {!current && (
                      <p className="text-red-600 text-sm">تحذير: لا يوجد سؤال نشط</p>
                    )}
                    {current && !current.text && (
                      <p className="text-orange-600 text-sm">تحذير: السؤال موجود لكن لا يوجد نص</p>
                    )}
                    
                    {/* Question metadata */}
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-xs">
                        {current?.dimensionTags && (
                          <Badge variant="secondary" className="text-xs">
                            {current.dimensionTags}
                          </Badge>
                        )}
                      </div>
                      
                      {/* Question type indicator */}
                      <div className="text-xs text-muted-foreground">
                        {current?.type === 'MCQ' && 'اختيار متعدد'}
                        {current?.type === 'LikertAgreement' && 'ليكرت'}
                        {current?.type === 'Frequency' && 'تكرار'}
                        {current?.type === 'ORDERING' && 'ترتيب'}
                        {current?.type === 'TIMED_NUMERIC' && 'رقمي مؤقت'}
                        {current?.type === 'TEXT' && 'نصي'}
                      </div>
                    </div>
                  </div>
                </CardHeader>
                
                {/* Question instructions or hints based on type */}
                <CardContent>
                  <div className="text-sm text-muted-foreground space-xs">
                    {current?.type === 'MCQ' && (
                      <p className="flex items-center space-xs">
                        <span className="w-2 h-2 bg-primary rounded-full"></span>
                        <span>اختر إجابة واحدة من الخيارات التالية</span>
                      </p>
                    )}
                    {current?.type === 'ORDERING' && (
                      <p className="flex items-center space-xs">
                        <span className="w-2 h-2 bg-amber-500 rounded-full"></span>
                        <span>رتّب الخيارات من الأولى إلى الأخيرة</span>
                      </p>
                    )}
                    {current?.type === 'TIMED_NUMERIC' && (
                      <p className="flex items-center space-xs">
                        <span className="w-2 h-2 bg-red-500 rounded-full"></span>
                        <span>أدخل رقماً صحيحاً أو عشرياً</span>
                      </p>
                    )}
                    {(current?.type === 'LikertAgreement' || current?.type === 'Frequency') && (
                      <p className="flex items-center space-xs">
                        <span className="w-2 h-2 bg-green-500 rounded-full"></span>
                        <span>اختر درجة الموافقة أو التكرار المناسبة</span>
                      </p>
                    )}
                    {current?.type === 'TEXT' && (
                      <p className="flex items-center space-xs">
                        <span className="w-2 h-2 bg-purple-500 rounded-full"></span>
                        <span>أجب بإيجاز ووضوح</span>
                      </p>
                    )}
                  </div>
                </CardContent>
              </SectionCard>
            </div>
            
            {/* Answer Section - Right Column (Left in RTL) */}
            <div className="lg:col-span-7 space-lg">
              <SectionCard className="shadow-md min-h-[400px]">
                <CardContent className="space-lg">
                  <motion.div
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    transition={{ delay: 0.1 }}
                    className="w-full"
                  >
                    {renderQuestionInput()}
                  </motion.div>
                  
                  {errorMessage && (
                    <motion.div
                      initial={{ opacity: 0, scale: 0.95 }}
                      animate={{ opacity: 1, scale: 1 }}
                      className="flex items-start space-sm p-4 bg-red-50 border border-red-200 rounded-xl text-red-700"
                    >
                      <AlertCircle className="h-5 w-5 flex-shrink-0 mt-0.5" />
                      <p className="text-sm leading-relaxed">{errorMessage}</p>
                    </motion.div>
                  )}
                </CardContent>
              </SectionCard>
            </div>
            </QuestionTransition>
          </SwipeableCard>
        </AnimatePresence>

        {/* Keyboard shortcuts hint */}
        <div className="mt-lg text-center hidden lg:block">
          <p className="text-xs text-muted-foreground">
            <kbd className="px-2 py-1 bg-muted rounded text-xs">Enter</kbd> التالي • 
            <kbd className="px-2 py-1 bg-muted rounded text-xs ml-2">Shift+Enter</kbd> السابق • 
            <kbd className="px-2 py-1 bg-muted rounded text-xs ml-2">1-9</kbd> اختيار سريع
          </p>
        </div>
      </ScreenContainer>

      {/* Bottom Action Bar - Responsive with safe area */}
      <div className="fixed bottom-0 inset-x-0 bg-background/95 backdrop-blur border-t p-3 sm:p-4 pb-[max(1rem,var(--safe-bottom))]">
        <ScreenContainer maxWidth="4xl" className="p-0">
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
            {/* Previous Button - Mobile optimized */}
            <TouchButton
              onClick={handlePrevious}
              disabled={currentIndex === 0}
              variant="outline"
              size="lg"
              fullWidth
              className="sm:w-auto"
            >
              <ArrowRight className="h-4 w-4 sm:h-5 sm:w-5" />
              {strings.exam.navigation.previous}
            </TouchButton>
            
            {/* Center info - Mobile only timer display */}
            <div className="sm:hidden">
              {questionTimeLeft !== null && (
                <div className="text-xs text-center text-muted-foreground py-1">
                  الوقت للسؤال: {formatTime(questionTimeLeft)}
                </div>
              )}
            </div>
            
            {/* Next/Finish Button - Full width on mobile, enhanced finish button */}
            <div className="w-full sm:w-auto flex gap-2 sm:gap-3">
              {isLastQuestion ? (
                <TouchButton
                  onClick={() => setShowFinishDialog(true)}
                  disabled={!canSubmit || isSubmitting}
                  variant="primary"
                  size="lg"
                  fullWidth
                  className={cn(
                    "sm:w-auto font-semibold shadow-lg",
                    !canSubmit ? 'bg-muted hover:bg-muted' : 'bg-green-600 hover:bg-green-700 text-white'
                  )}
                >
                  {isSubmitting ? (
                    <motion.div
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      className="flex items-center gap-2"
                    >
                      <Timer className="h-5 w-5 animate-spin" />
                      جاري الإنهاء...
                    </motion.div>
                  ) : (
                    <motion.div
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      className="flex items-center gap-2"
                    >
                      <CheckCircle className="h-5 w-5" />
                      إنهاء الاختبار
                    </motion.div>
                  )}
                </TouchButton>
              ) : (
                <TouchButton
                  onClick={canSubmit ? handleSubmitAnswer : handleNext}
                  disabled={isSubmitting}
                  variant="primary"
                  size="lg"
                  fullWidth
                  className="sm:w-auto"
                >
                  {isSubmitting ? (
                    <motion.div
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      className="flex items-center gap-2"
                    >
                      <Timer className="h-4 w-4 animate-spin" />
                      {strings.exam.submitting}
                    </motion.div>
                  ) : (
                    <motion.div
                      initial={{ opacity: 0 }}
                      animate={{ opacity: 1 }}
                      className="flex items-center gap-2"
                    >
                      {strings.exam.navigation.next}
                      <ArrowLeft className="h-4 w-4 sm:h-5 sm:w-5" />
                    </motion.div>
                  )}
                </TouchButton>
              )}
            </div>
          </div>
        </ScreenContainer>
      </div>

      {/* Finish confirmation dialog - Responsive */}
      <Dialog open={showFinishDialog} onOpenChange={setShowFinishDialog}>
        <DialogContent className="sm:max-w-md mx-3 sm:mx-auto rounded-2xl">
          <DialogHeader className="text-center">
            <DialogTitle className="text-[clamp(18px,2vw,20px)] font-bold">
              {strings.exam.confirmFinish.title}
            </DialogTitle>
            <DialogDescription className="text-sm sm:text-base mt-2 leading-relaxed">
              {strings.exam.confirmFinish.message}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="flex flex-col sm:flex-row gap-3 sm:gap-2 pt-4">
            <Button
              variant="outline"
              onClick={() => setShowFinishDialog(false)}
              className="w-full sm:w-auto h-11 sm:h-10 min-h-[44px] order-2 sm:order-1"
            >
              {strings.exam.confirmFinish.cancel}
            </Button>
            <Button
              onClick={confirmFinish}
              disabled={isSubmitting}
              className="w-full sm:w-auto h-11 sm:h-10 min-h-[44px] gap-2 order-1 sm:order-2"
            >
              <CheckCircle className="h-4 w-4 sm:h-5 sm:w-5" />
              {strings.exam.confirmFinish.confirm}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      </div>
        </>
      )}
    </div>
  );
}

export default function Exam() {
  return (
    <GuardedRoute page="exam">
      <ExamContent />
    </GuardedRoute>
  );
}