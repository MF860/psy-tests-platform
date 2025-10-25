export interface SessionData {
  nationalId: string;
  sessionId: string;
  startedAt: string;
  consented: boolean;
  instructionsCompleted: boolean;
  currentQuestionIndex: number;
  answers: Record<string, string>;
  totalQuestions?: number;
}

export interface ResumeData {
  sessionId: string;
  nationalId: string;
  currentQuestionIndex: number;
  answers: Record<string, string>;
  totalQuestions?: number;
}

const SESSION_KEY = 'psy-test-session';
const RESUME_KEY = 'psy-test-resume';

export const sessionStorage = {
  save: (data: Partial<SessionData>): void => {
    try {
      const existing = sessionStorage.get();
      const updated = { ...existing, ...data };
      localStorage.setItem(SESSION_KEY, JSON.stringify(updated));
    } catch (error) {
      console.warn('Failed to save session data:', error);
    }
  },

  get: (): SessionData | null => {
    try {
      const data = localStorage.getItem(SESSION_KEY);
      return data ? JSON.parse(data) : null;
    } catch (error) {
      console.warn('Failed to get session data:', error);
      return null;
    }
  },

  clear: (): void => {
    try {
      localStorage.removeItem(SESSION_KEY);
      localStorage.removeItem(RESUME_KEY);
    } catch (error) {
      console.warn('Failed to clear session data:', error);
    }
  },

  saveAnswer: (questionId: string, answer: string): void => {
    try {
      const session = sessionStorage.get();
      if (session) {
        session.answers = { ...session.answers, [questionId]: answer };
        sessionStorage.save(session);
      }
    } catch (error) {
      console.warn('Failed to save answer:', error);
    }
  },

  saveProgress: (currentIndex: number): void => {
    try {
      const session = sessionStorage.get();
      if (session) {
        session.currentQuestionIndex = currentIndex;
        sessionStorage.save(session);
      }
    } catch (error) {
      console.warn('Failed to save progress:', error);
    }
  },

  setConsent: (consented: boolean): void => {
    sessionStorage.save({ consented });
  },

  setInstructionsCompleted: (completed: boolean): void => {
    sessionStorage.save({ instructionsCompleted: completed });
  },

  canResumeExam: (): boolean => {
    const session = sessionStorage.get();
    return !!(session?.sessionId && session?.consented && session?.instructionsCompleted);
  },

  shouldRedirectToPrivacy: (): boolean => {
    const session = sessionStorage.get();
    return !!(session?.sessionId && !session?.consented);
  },

  shouldRedirectToInstructions: (): boolean => {
    const session = sessionStorage.get();
    return !!(session?.sessionId && session?.consented && !session?.instructionsCompleted);
  },
};

export const createInitialSession = (nationalId: string, sessionId: string): SessionData => {
  return {
    nationalId,
    sessionId,
    startedAt: new Date().toISOString(),
    consented: false,
    instructionsCompleted: false,
    currentQuestionIndex: 0,
    answers: {},
  };
};

export const getResumeRedirectPath = (): string => {
  const session = sessionStorage.get();
  if (!session?.sessionId) return '/login';
  if (!session.consented) return '/privacy';
  if (!session.instructionsCompleted) return '/instructions';
  return '/exam';
};