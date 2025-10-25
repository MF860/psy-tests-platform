// lib/useFlowState.ts
import { create } from 'zustand';

export interface FlowState {
  nationalId?: string;
  sessionId?: string;
  resume?: boolean;
  consented: boolean;
  instructionsCompleted: boolean;
  examFinished: boolean;
  totalQuestions?: number;
  
  // Actions
  setSession: (data: { nationalId: string; sessionId: string; resume?: boolean; totalQuestions?: number }) => void;
  setConsented: (consented: boolean) => void;
  setInstructionsCompleted: (completed: boolean) => void;
  setExamFinished: (finished: boolean) => void;
  clearSession: () => void;
}

// Session-keyed storage functions
const getStorageKey = (sessionId: string | undefined, key: string): string => 
  sessionId ? `${key}:${sessionId}` : key;

const getSessionFlag = (sessionId: string | undefined, key: string): boolean => {
  if (!sessionId) return false;
  try {
    const value = sessionStorage.getItem(getStorageKey(sessionId, key));
    return value === 'true';
  } catch {
    return false;
  }
};

const setSessionFlag = (sessionId: string | undefined, key: string, value: boolean): void => {
  if (!sessionId) return;
  try {
    sessionStorage.setItem(getStorageKey(sessionId, key), value.toString());
  } catch {
    // Ignore storage errors
  }
};

const clearSessionFlags = (sessionId: string | undefined): void => {
  if (!sessionId) return;
  try {
    ['consent', 'instr', 'finished', 'resume'].forEach(key => {
      sessionStorage.removeItem(getStorageKey(sessionId, key));
    });
  } catch {
    // Ignore storage errors
  }
};

export const useFlowState = create<FlowState>((set, get) => ({
  nationalId: undefined,
  sessionId: undefined,
  resume: false,
  consented: false,
  instructionsCompleted: false,
  examFinished: false,
  totalQuestions: undefined,

  setSession: (data) => {
    const { nationalId, sessionId, resume = false, totalQuestions } = data;
    
    // Clear old session flags when starting a new session
    const currentSessionId = get().sessionId;
    if (currentSessionId && currentSessionId !== sessionId) {
      clearSessionFlags(currentSessionId);
    }

    // Load existing flags for this session
    const consented = getSessionFlag(sessionId, 'consent');
    const instructionsCompleted = getSessionFlag(sessionId, 'instr');
    const examFinished = getSessionFlag(sessionId, 'finished');
    
    // Store resume flag
    setSessionFlag(sessionId, 'resume', resume);
    
    set({
      nationalId,
      sessionId,
      resume,
      consented,
      instructionsCompleted,
      examFinished,
      totalQuestions
    });
  },

  setConsented: (consented) => {
    const { sessionId } = get();
    setSessionFlag(sessionId, 'consent', consented);
    set({ consented });
  },

  setInstructionsCompleted: (instructionsCompleted) => {
    const { sessionId } = get();
    setSessionFlag(sessionId, 'instr', instructionsCompleted);
    set({ instructionsCompleted });
  },

  setExamFinished: (examFinished) => {
    const { sessionId } = get();
    setSessionFlag(sessionId, 'finished', examFinished);
    set({ examFinished });
  },

  clearSession: () => {
    const { sessionId } = get();
    clearSessionFlags(sessionId);
    set({
      nationalId: undefined,
      sessionId: undefined,
      resume: false,
      consented: false,
      instructionsCompleted: false,
      examFinished: false,
      totalQuestions: undefined
    });
  }
}));