import { useCallback, useEffect, useRef, useState } from "react";
import { motion, AnimatePresence } from "framer-motion";
import { cn } from "../lib/utils";
import { ApiError } from "../lib/api";
import { strings, formatTime } from "../lib/strings";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "../components/ui/dialog";
import { Timer, CheckCircle, AlertCircle, ArrowLeft, ArrowRight } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { getNextQuestion, submitAnswer, finishSession, submitSession } from "../lib/api-client";
import AutosaveIndicator from "../components/Exam/AutosaveIndicator";
import { ProgressiveLoader } from "../components/ui/progressive-loader";

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
    <div className="relative min-h-screen w-screen overflow-hidden bg-gradient-to-br from-gray-900 via-indigo-900 to-gray-900" dir="rtl">
      {/* Animated background particles */}
      <div className="absolute inset-0 opacity-10">
        <div className="absolute top-20 left-20 w-96 h-96 bg-blue-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse"></div>
        <div className="absolute top-40 right-20 w-96 h-96 bg-purple-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-2000"></div>
        <div className="absolute bottom-20 left-1/3 w-96 h-96 bg-indigo-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-4000"></div>
      </div>

      {/* Header with logos and step indicator */}
      <header className="absolute top-0 w-full flex justify-between items-center p-4 sm:p-6 z-20 bg-gray-900/80 backdrop-blur-md border-b border-white/10">
        <div className="flex-1 hidden sm:block"></div>
        <div className="flex-1 flex justify-center">
          <img 
            src="/STEST.png" 
            alt="شعار المنصة" 
            className="h-10 sm:h-14 object-contain drop-shadow-lg"
          />
        </div>
        <div className="flex-1 flex justify-end items-center gap-2 sm:gap-4">
          <div className="px-3 sm:px-4 py-1.5 sm:py-2 bg-indigo-500/30 backdrop-blur-sm border border-indigo-400/40 rounded-full text-indigo-200 text-xs sm:text-sm font-medium">
            {headerTitle}
          </div>
          <img 
            src="/FB-ICON.png" 
            alt="أيقونة" 
            className="h-8 sm:h-10 object-contain drop-shadow-lg"
          />
        </div>
      </header>

      {/* Loading State */}
      {isLoadingQuestion && !current && (
        <main className="relative z-10 flex items-center justify-center min-h-screen pt-20 px-4">
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            className="w-full max-w-2xl bg-white/10 backdrop-blur-lg rounded-2xl shadow-xl border border-white/20 p-12"
          >
            <ProgressiveLoader 
              stage={isLoadingQuestion ? 'fetching' : 'complete'}
              className="min-h-[200px]"
            >
              <div className="text-center">
                <p className="text-lg font-medium text-white mb-2">جاري تحضير السؤال التالي</p>
                <p className="text-gray-300">الرجاء الانتظار...</p>
              </div>
            </ProgressiveLoader>
          </motion.div>
        </main>
      )}

      {/* Debug State */}
      {!isLoadingQuestion && !current && questions.length === 0 && (
        <main className="relative z-10 flex items-center justify-center min-h-screen pt-20 px-4">
          <div className="w-full max-w-4xl bg-white/10 backdrop-blur-lg rounded-2xl shadow-xl border border-white/20 p-12">
            <h2 className="text-xl font-bold text-red-400 mb-4">Debug: No Questions Loaded</h2>
            <div className="space-y-4 text-left text-gray-300" dir="ltr">
              <div><strong>Session ID:</strong> {sessionId || 'null'}</div>
              <div><strong>National ID:</strong> {nationalId || 'null'}</div>
              <div><strong>Questions Array Length:</strong> {questions.length}</div>
              <div><strong>Current Question:</strong> {current ? 'exists' : 'null'}</div>
              <div><strong>Is Loading:</strong> {isLoadingQuestion ? 'true' : 'false'}</div>
              <div><strong>Fetching Ref:</strong> {fetchingRef.current ? 'true' : 'false'}</div>
              <div><strong>Current Index:</strong> {currentIndex}</div>
              <div><strong>Flow State - Consented:</strong> {consented ? 'true' : 'false'}</div>
              <div><strong>Flow State - Instructions:</strong> {instructionsCompleted ? 'true' : 'false'}</div>
              <div><strong>Error Message:</strong> {errorMessage || 'none'}</div>
              {errorMessage && (
                <div className="bg-red-500/20 border border-red-400/50 text-red-300 px-4 py-3 rounded-lg">
                  {errorMessage}
                </div>
              )}
              <button
                onClick={() => sessionId && fetchNextFromApi()}
                className="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg mt-4 transition-all"
                disabled={!sessionId || isLoadingQuestion}
              >
                Retry Loading Question
              </button>
            </div>
          </div>
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
                className="fixed top-20 left-1/2 transform -translate-x-1/2 z-50 bg-blue-500/20 backdrop-blur-sm border border-blue-400/30 text-blue-200 px-6 py-3 rounded-lg shadow-lg max-w-md mx-auto text-center text-sm"
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
                className="fixed top-20 left-1/2 transform -translate-x-1/2 z-50 bg-orange-500/20 backdrop-blur-sm border-2 border-orange-400/50 text-orange-200 px-6 py-3 rounded-lg shadow-lg max-w-md mx-auto text-center text-sm font-semibold"
              >
                {sessionWarningMessage}
              </motion.div>
            )}
          </AnimatePresence>

          {/* Top Status Bar - Enhanced visibility */}
          <div className="fixed top-16 sm:top-20 left-0 right-0 z-10 bg-gray-800/90 backdrop-blur-md border-b border-indigo-400/30 shadow-lg">
            <div className="max-w-7xl mx-auto px-3 sm:px-4 py-3 sm:py-4">
              <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2 sm:gap-3">
                {/* Progress & Counter */}
                <div className="flex items-center gap-2 sm:gap-4 flex-1 min-w-0">
                  <div className="flex-1 min-w-0 bg-gray-700/70 rounded-full h-2.5 sm:h-3 overflow-hidden border border-indigo-400/40 shadow-inner">
                    <motion.div 
                      className="h-full bg-gradient-to-r from-blue-500 via-indigo-500 to-purple-500 rounded-full shadow-md"
                      initial={{ width: 0 }}
                      animate={{ width: `${progressPercent}%` }}
                      transition={{ duration: 0.5 }}
                    />
                  </div>
                  <motion.span 
                    key={currentIndex}
                    initial={{ opacity: 0, scale: 0.9 }}
                    animate={{ opacity: 1, scale: 1 }}
                    transition={{ duration: 0.2 }}
                    className="text-xs sm:text-sm font-bold text-white whitespace-nowrap flex-shrink-0 bg-indigo-600/40 px-2 sm:px-3 py-1 rounded-lg border border-indigo-400/40"
                  >
                    {currentIndex + 1}/{totalQuestions || "-"}
                  </motion.span>
                </div>

                {/* Status Indicators */}
                <div className="flex items-center justify-between sm:justify-end gap-2 sm:gap-3">
                  {/* Session Timer */}
                  <div className={`
                    flex items-center gap-1.5 sm:gap-2 px-2.5 sm:px-4 py-1.5 sm:py-2 rounded-lg text-xs sm:text-sm font-bold transition-all duration-200 shadow-md
                    ${sessionTimeLeft <= 60 
                      ? 'bg-red-600/50 text-white border-2 border-red-400 animate-pulse' 
                      : sessionTimeLeft <= 300
                      ? 'bg-orange-600/40 text-orange-100 border border-orange-400' 
                      : 'bg-green-600/40 text-green-100 border border-green-400'
                    }
                  `}>
                    <Timer className="h-3 w-3 sm:h-4 sm:w-4" />
                    <span className="font-mono">
                      {Math.floor(sessionTimeLeft / 60)}:{String(sessionTimeLeft % 60).padStart(2, '0')}
                    </span>
                  </div>
                  
                  <AutosaveIndicator state={autosaveState} />
                  
                  {/* Question Timer */}
                  {questionTimeLeft !== null && (
                    <div className={`
                      flex items-center gap-1.5 sm:gap-2 px-2.5 sm:px-4 py-1.5 sm:py-2 rounded-lg text-xs sm:text-sm font-semibold transition-all duration-200 shadow-md
                      ${questionTimeLeft <= 10 
                        ? 'bg-red-600/50 text-white border-2 border-red-400 animate-pulse' 
                        : questionTimeLeft <= 30
                        ? 'bg-red-600/40 text-red-100 border border-red-400' 
                        : 'bg-blue-600/40 text-blue-100 border border-blue-400'
                      }
                    `}>
                      <Timer className="h-3 w-3 sm:h-4 sm:w-4" />
                      <span className="font-mono">{formatTime(questionTimeLeft)}</span>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>

          {/* Main Content - Improved spacing and responsiveness */}
          <main className="relative z-10 pt-32 sm:pt-44 md:pt-48 pb-24 sm:pb-32 px-3 sm:px-4">
            <div className="max-w-7xl mx-auto">
              <AnimatePresence mode="wait">
                <motion.div
                  key={current?.id || 'loading'}
                  initial={{ opacity: 0, x: 50 }}
                  animate={{ opacity: 1, x: 0 }}
                  exit={{ opacity: 0, x: -50 }}
                  transition={{ duration: 0.3 }}
                  className="grid grid-cols-1 lg:grid-cols-12 gap-4 sm:gap-6"
                >
                  {/* Question Content - Left Column */}
                  <div className="lg:col-span-5">
                    <div className="bg-white/10 backdrop-blur-lg rounded-xl sm:rounded-2xl shadow-xl border border-white/20 p-4 sm:p-6">
                      <div className="space-y-3 sm:space-y-4">
                        <h2 className="text-xl sm:text-2xl font-bold leading-relaxed text-white">
                          {current?.text ?? "لا يوجد نص للسؤال"}
                        </h2>
                        
                        {/* Question metadata */}
                        <div className="flex items-center justify-between flex-wrap gap-2">
                          {current?.dimensionTags && (
                            <span className="px-2.5 sm:px-3 py-1 bg-indigo-500/30 border border-indigo-400/40 rounded-full text-xs text-indigo-100">
                              {current.dimensionTags}
                            </span>
                          )}
                          
                          {/* Question type indicator */}
                          <div className="text-xs text-gray-300 bg-gray-800/50 px-2.5 py-1 rounded-full">
                            {current?.type === 'MCQ' && 'اختيار متعدد'}
                            {current?.type === 'LikertAgreement' && 'ليكرت'}
                            {current?.type === 'Frequency' && 'تكرار'}
                            {current?.type === 'ORDERING' && 'ترتيب'}
                            {current?.type === 'TIMED_NUMERIC' && 'رقمي مؤقت'}
                            {current?.type === 'TEXT' && 'نصي'}
                          </div>
                        </div>

                        {/* Question instructions */}
                        <div className="pt-3 sm:pt-4 border-t border-white/10">
                          <div className="text-xs sm:text-sm text-gray-300">
                            {current?.type === 'MCQ' && (
                              <p className="flex items-center gap-2">
                                <span className="w-2 h-2 bg-blue-400 rounded-full flex-shrink-0"></span>
                                <span>اختر إجابة واحدة من الخيارات التالية</span>
                              </p>
                            )}
                            {current?.type === 'ORDERING' && (
                              <p className="flex items-center gap-2">
                                <span className="w-2 h-2 bg-amber-400 rounded-full flex-shrink-0"></span>
                                <span>رتّب الخيارات من الأولى إلى الأخيرة</span>
                              </p>
                            )}
                            {current?.type === 'TIMED_NUMERIC' && (
                              <p className="flex items-center gap-2">
                                <span className="w-2 h-2 bg-red-400 rounded-full flex-shrink-0"></span>
                                <span>أدخل رقماً صحيحاً أو عشرياً</span>
                              </p>
                            )}
                            {(current?.type === 'LikertAgreement' || current?.type === 'Frequency') && (
                              <p className="flex items-center gap-2">
                                <span className="w-2 h-2 bg-green-400 rounded-full flex-shrink-0"></span>
                                <span>اختر درجة الموافقة أو التكرار المناسبة</span>
                              </p>
                            )}
                            {current?.type === 'TEXT' && (
                              <p className="flex items-center gap-2">
                                <span className="w-2 h-2 bg-purple-400 rounded-full flex-shrink-0"></span>
                                <span>أجب بإيجاز ووضوح</span>
                              </p>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                  
                  {/* Answer Section - Right Column */}
                  <div className="lg:col-span-7">
                    <div className="bg-white/10 backdrop-blur-lg rounded-xl sm:rounded-2xl shadow-xl border border-white/20 p-4 sm:p-6 md:p-8 min-h-[300px] sm:min-h-[400px]">
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
                          className="flex items-start gap-2 sm:gap-3 p-3 sm:p-4 mt-4 sm:mt-6 bg-red-500/20 border border-red-400/50 rounded-lg text-red-200"
                        >
                          <AlertCircle className="h-4 w-4 sm:h-5 sm:w-5 flex-shrink-0 mt-0.5" />
                          <p className="text-xs sm:text-sm leading-relaxed">{errorMessage}</p>
                        </motion.div>
                      )}
                    </div>
                  </div>
                </motion.div>
              </AnimatePresence>

              {/* Keyboard shortcuts hint */}
              <div className="mt-6 sm:mt-8 text-center hidden lg:block">
                <p className="text-xs text-gray-400">
                  <kbd className="px-2 py-1 bg-white/10 rounded text-xs border border-white/20">Enter</kbd> التالي • 
                  <kbd className="px-2 py-1 bg-white/10 rounded text-xs border border-white/20 ml-2">Shift+Enter</kbd> السابق • 
                  <kbd className="px-2 py-1 bg-white/10 rounded text-xs border border-white/20 ml-2">1-9</kbd> اختيار سريع
                </p>
              </div>
            </div>
          </main>

          {/* Bottom Action Bar - Enhanced responsiveness */}
          <div className="fixed bottom-0 left-0 right-0 z-20 bg-gray-800/90 backdrop-blur-md border-t border-indigo-400/30 p-3 sm:p-4 shadow-lg">
            <div className="max-w-7xl mx-auto">
              <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2 sm:gap-3">
                {/* Previous Button */}
                <button
                  onClick={handlePrevious}
                  disabled={currentIndex === 0}
                  className="w-full sm:w-auto bg-white/10 backdrop-blur-sm border border-white/30 text-white font-bold py-2.5 sm:py-3 px-4 sm:px-6 rounded-lg hover:bg-white/20 transition-all duration-300 disabled:opacity-30 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                >
                  <ArrowRight className="h-4 w-4 sm:h-5 sm:w-5" />
                  <span className="text-sm sm:text-base">{strings.exam.navigation.previous}</span>
                </button>
                
                {/* Next/Finish Button */}
                {isLastQuestion ? (
                  <button
                    onClick={() => setShowFinishDialog(true)}
                    disabled={!canSubmit || isSubmitting}
                    className={cn(
                      "w-full sm:w-auto font-bold py-2.5 sm:py-3 px-6 sm:px-8 rounded-lg shadow-lg transition-all duration-300 flex items-center justify-center gap-2",
                      !canSubmit 
                        ? 'bg-gray-700 text-gray-400 cursor-not-allowed' 
                        : 'bg-gradient-to-r from-green-500 to-emerald-400 text-white hover:scale-105 hover:shadow-2xl'
                    )}
                  >
                    {isSubmitting ? (
                      <>
                        <Timer className="h-4 w-4 sm:h-5 sm:w-5 animate-spin" />
                        <span className="text-sm sm:text-base">جاري الإنهاء...</span>
                      </>
                    ) : (
                      <>
                        <CheckCircle className="h-4 w-4 sm:h-5 sm:w-5" />
                        <span className="text-sm sm:text-base">إنهاء الاختبار</span>
                      </>
                    )}
                  </button>
                ) : (
                  <button
                    onClick={canSubmit ? handleSubmitAnswer : handleNext}
                    disabled={isSubmitting}
                    className="w-full sm:w-auto bg-gradient-to-r from-blue-500 to-indigo-400 text-white font-bold py-2.5 sm:py-3 px-6 sm:px-8 rounded-lg shadow-lg hover:scale-105 hover:shadow-2xl transition-all duration-300 disabled:opacity-50 flex items-center justify-center gap-2"
                  >
                    {isSubmitting ? (
                      <>
                        <Timer className="h-3.5 w-3.5 sm:h-4 sm:w-4 animate-spin" />
                        <span className="text-sm sm:text-base">{strings.exam.submitting}</span>
                      </>
                    ) : (
                      <>
                        <span className="text-sm sm:text-base">{strings.exam.navigation.next}</span>
                        <ArrowLeft className="h-4 w-4 sm:h-5 sm:w-5" />
                      </>
                    )}
                  </button>
                )}
              </div>
            </div>
          </div>

          {/* Finish confirmation dialog */}
          <Dialog open={showFinishDialog} onOpenChange={setShowFinishDialog}>
            <DialogContent className="sm:max-w-md mx-3 sm:mx-auto rounded-2xl bg-gray-900/95 backdrop-blur-lg border border-white/20 text-white">
              <DialogHeader className="text-center">
                <DialogTitle className="text-xl font-bold text-white">
                  {strings.exam.confirmFinish.title}
                </DialogTitle>
                <DialogDescription className="text-sm sm:text-base mt-2 leading-relaxed text-gray-300">
                  {strings.exam.confirmFinish.message}
                </DialogDescription>
              </DialogHeader>
              <DialogFooter className="flex flex-col sm:flex-row gap-3 pt-4">
                <button
                  onClick={() => setShowFinishDialog(false)}
                  className="w-full sm:w-auto bg-white/10 border border-white/20 text-white font-bold py-3 px-6 rounded-lg hover:bg-white/20 transition-all duration-300"
                >
                  {strings.exam.confirmFinish.cancel}
                </button>
                <button
                  onClick={confirmFinish}
                  disabled={isSubmitting}
                  className="w-full sm:w-auto bg-gradient-to-r from-green-500 to-emerald-400 text-white font-bold py-3 px-6 rounded-lg hover:scale-105 transition-all duration-300 flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  <CheckCircle className="h-5 w-5" />
                  {strings.exam.confirmFinish.confirm}
                </button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
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