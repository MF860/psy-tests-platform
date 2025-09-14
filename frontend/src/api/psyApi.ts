import axios from 'axios';

const api = axios.create({
  baseURL: 'http://localhost:5019/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

export interface SessionResponse {
  sessionId: number;
  totalQuestions: number;
}

export interface QuestionResponse {
  id: number;
  text_ar: string;
  type: string;
  dimension_tags: string;
  difficulty: number;
  time_limit_seconds: number;
  max_score: number;
  options?: string[];
}

export interface CompletedResponse {
  message: string;
}

export interface AnswerResponse {
  message: string;
}

export interface SubmitResponse {
  message: string;
  sessionId: number;
}

export const startSession = async (nationalId: string): Promise<SessionResponse> => {
  const response = await api.post<SessionResponse>('/sessions/start', { national_id: nationalId });
  return response.data;
};

export const getNextQuestion = async (sessionId: number): Promise<QuestionResponse | CompletedResponse> => {
  const response = await api.get<QuestionResponse | CompletedResponse>(`/sessions/${sessionId}/next`);
  return response.data;
};

export const submitAnswer = async (sessionId: number, itemId: string, answer: string): Promise<AnswerResponse> => {
  const response = await api.post<AnswerResponse>(`/sessions/${sessionId}/answer`, { item_id: itemId, answer });
  return response.data;
};

export const submitTest = async (sessionId: number): Promise<SubmitResponse> => {
  const response = await api.post<SubmitResponse>(`/sessions/${sessionId}/submit`);
  return response.data;
};
