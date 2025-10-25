import { create } from "zustand";

interface Answer {
  questionId: string;
  value: string | boolean;
}

interface UserProfile {
  nationalId: string;
  fullName?: string;
  birthDate?: string;
  age?: number;
  gender?: string;
  city?: string;
  mobile?: string;
  email?: string;
  education?: string;
  occupation?: string;
}

interface UserState {
  nationalId: string;
  sessionId: string | null;
  totalQuestions: number;
  answers: Answer[];
  profile?: UserProfile;
  setProfile: (p: UserProfile | undefined) => void;
  setNationalId: (id: string) => void;
  setSessionId: (sessionId: string | null) => void;
  setTotalQuestions: (total: number) => void;
  addAnswer: (answer: Answer) => void;
  updateAnswer: (questionId: string, value: string | boolean) => void;
  resetAnswers: () => void;
  clearSession: () => void;
  reset: () => void;
}

export const useUserStore = create<UserState>((set) => ({
  nationalId: "",
  sessionId: null,
  totalQuestions: 0,
  answers: [],
  profile: undefined,

  setProfile: (p) => set({ profile: p }),

  setNationalId: (id) => set({ nationalId: id }),

  setSessionId: (sessionId) => set({ sessionId }),

  setTotalQuestions: (total) => set({ totalQuestions: total }),

  addAnswer: (answer) => set((state) => ({
    answers: [...state.answers, answer]
  })),

  updateAnswer: (questionId, value) => set((state) => ({
    answers: state.answers.map((answer) =>
      answer.questionId === questionId ? { ...answer, value } : answer
    )
  })),

  resetAnswers: () => set({ answers: [] }),

  clearSession: () => set({ sessionId: null, totalQuestions: 0, answers: [], profile: undefined }),

  reset: () => set({
    nationalId: "",
    sessionId: null,
    totalQuestions: 0,
    answers: [],
    profile: undefined
  })
}));
