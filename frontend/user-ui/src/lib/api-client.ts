// lib/api-client.ts
import { apiFetchJson } from './api';
import { 
  StartSessionRequestZ, 
  AnswerPayloadZ,
  type UiQuestion,
  type StartSessionResponse
} from './contracts';
import { 
  normalizeApiQuestion, 
  buildAnswerPayload, 
  buildStartSessionBody,
  normalizeStartSessionResponse 
} from './contract-bridge';

/**
 * Start a new session - with retry logic for field name variations
 */
export async function startSession(nationalId: string): Promise<StartSessionResponse> {
  const requestBody = buildStartSessionBody(nationalId);
  
  // Validate request body
  const validatedBody = StartSessionRequestZ.parse(requestBody);
  
  // Try with PascalCase first (documented format), then camelCase if needed
  const alternateBody = { nationalId }; // fallback format
  
  return await apiFetchJson<StartSessionResponse>(
    '/sessions/start',
    {
      method: 'POST',
      body: JSON.stringify(validatedBody)
    },
    {
      endpoint: 'POST /sessions/start',
      normalizer: normalizeStartSessionResponse
    },
    {
      alternateBody,
      maxRetries: 1
    }
  );
}

/**
 * Get the next question in the session
 */
export async function getNextQuestion(sessionId: string): Promise<UiQuestion | { message: 'completed' }> {
  console.log('[getNextQuestion] Starting request for sessionId:', sessionId);
  
  try {
    const response = await apiFetchJson<unknown>(
      `/sessions/${sessionId}/next`,
      { method: 'GET' },
      {
        endpoint: `GET /sessions/${sessionId}/next`,
        skipValidation: true // We'll handle validation manually since response can be question or completion
      }
    );
    
    console.log('[getNextQuestion] Raw API response:', response);
    console.log('[getNextQuestion] Response type:', typeof response);
    
    // Check if this is a completion response
    if (response && typeof response === 'object' && 'message' in response) {
      const msg = (response as { message: string }).message;
      console.log('[getNextQuestion] Found message field:', msg);
      if (msg === 'completed') {
        return { message: 'completed' as const };
      }
    }
    
    // Otherwise, normalize as a question
    console.log('[getNextQuestion] Normalizing as question...');
    const normalized = normalizeApiQuestion(response);
    console.log('[getNextQuestion] Normalized question:', normalized);
    return normalized;
    
  } catch (error) {
    console.error('[getNextQuestion] Error occurred:', error);
    throw error; // Re-throw to be handled by caller
  }
}

/**
 * Submit an answer - with retry logic for field name variations
 */
export async function submitAnswer(
  sessionId: string, 
  question: UiQuestion, 
  rawValue: string, 
  startedAtMs?: number
): Promise<{ message: string; skipToNext?: boolean }> {
  const answerPayload = buildAnswerPayload(question, rawValue, startedAtMs);
  
  // Validate the payload
  const validatedPayload = AnswerPayloadZ.parse(answerPayload);
  
  // Create alternate with camelCase fields as fallback
  const alternateBody = {
    itemId: validatedPayload.ItemId,
    answer: validatedPayload.Answer,
    numericAnswer: validatedPayload.NumericAnswer,
    responseTimeMs: validatedPayload.ResponseTimeMs
  };
  
  return await apiFetchJson<{ message: string; skipToNext?: boolean }>(
    `/sessions/${sessionId}/answer`,
    {
      method: 'POST',
      body: JSON.stringify(validatedPayload)
    },
    {
      endpoint: `POST /sessions/${sessionId}/answer`,
      skipValidation: true // Response is simple success message
    },
    {
      alternateBody,
      maxRetries: 1
    }
  );
}

/**
 * Finish the session
 */
export async function finishSession(sessionId: string): Promise<{ finished: boolean }> {
  return await apiFetchJson<{ finished: boolean }>(
    `/sessions/${sessionId}/finish`,
    { method: 'POST' },
    {
      endpoint: `POST /sessions/${sessionId}/finish`,
      skipValidation: true
    }
  );
}

/**
 * Submit the completed session
 */
export async function submitSession(sessionId: string): Promise<{ message?: string; success?: boolean }> {
  return await apiFetchJson<{ message?: string; success?: boolean }>(
    `/sessions/${sessionId}/submit`,
    { method: 'POST' },
    {
      endpoint: `POST /sessions/${sessionId}/submit`,
      skipValidation: true
    }
  );
}

/**
 * Get session details
 */
export async function getSession(sessionId: string): Promise<{
  sessionId: string;
  userNationalId: string;
  currentIndex: number;
  lastActivityAt: string;
  remainingCount: number;
}> {
  return await apiFetchJson<{
    sessionId: string;
    userNationalId: string; 
    currentIndex: number;
    lastActivityAt: string;
    remainingCount: number;
  }>(
    `/sessions/${sessionId}`,
    { method: 'GET' },
    {
      endpoint: `GET /sessions/${sessionId}`,
      skipValidation: true
    }
  );
}