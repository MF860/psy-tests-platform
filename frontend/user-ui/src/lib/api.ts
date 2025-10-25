import { z } from 'zod';
import questionsData from '../mocks/questions.json';
import resultsData from '../mocks/results.json';
import { generateMockRecommendations } from '../mocks/recommendations';

const rawBaseUrl = ((import.meta as unknown as { env?: Record<string, string | undefined> }).env?.VITE_API_BASE_URL ?? "http://localhost:5019/api").replace(/\/$/, "");

const API_BASE_URL = rawBaseUrl;

// Demo mode configuration
const isDemoMode = () => {
  const env = (import.meta as unknown as { env?: Record<string, string | undefined> }).env;
  return env?.VITE_DEMO_MODE === 'true';
};

export class ApiError extends Error {
  status: number;
  body: unknown;

  constructor(message: string, status: number, body: unknown) {
    super(message);
    this.status = status;
    this.body = body;
  }
}

export class ContractError extends Error {
  endpoint: string;
  expectedFields: string[];
  actualData: unknown;

  constructor(message: string, endpoint: string, expectedFields: string[], actualData: unknown) {
    super(message);
    this.endpoint = endpoint;
    this.expectedFields = expectedFields;
    this.actualData = actualData;
  }
}

function buildUrl(path: string): string {
  if (!path.startsWith("/")) {
    return `${API_BASE_URL}/${path}`;
  }
  return `${API_BASE_URL}${path}`;
}

function buildHeaders(init?: RequestInit): Headers {
  const headers = new Headers(init?.headers ?? undefined);
  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json");
  }

  const hasBody = typeof init?.body !== "undefined" && init.body !== null;
  if (hasBody && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  return headers;
}

// Contract validation options
interface ContractOptions<T> {
  schema?: z.ZodSchema<T>;
  endpoint?: string;
  normalizer?: (data: unknown) => T;
  skipValidation?: boolean;
}

// Request retry options for handling naming variations
interface RetryOptions {
  alternateBody?: Record<string, unknown>;
  maxRetries?: number;
}

/**
 * Mock router for demo mode - handles all API endpoints locally
 */
async function mockRouter<T>(path: string, init: RequestInit = {}): Promise<T> {
  console.log('[MOCK ROUTER] Handling request:', { path, method: init.method || 'GET' });
  
  const method = init.method || 'GET';
  const body = init.body ? JSON.parse(init.body as string) : null;
  
  // Add artificial delay to simulate network
  await new Promise(resolve => setTimeout(resolve, 200 + Math.random() * 300));
  
  try {
    // SESSION ENDPOINTS
    if (path === '/sessions/start' && method === 'POST') {
      return handleSessionStart(body) as T;
    }
    
    if (path.match(/^\/sessions\/(.+)\/next$/) && method === 'GET') {
      const sessionId = path.match(/^\/sessions\/(.+)\/next$/)?.[1];
      const rawQuestion = handleGetNextQuestion(sessionId!);
      // Apply normalization to ensure consistent format
      if (rawQuestion && typeof rawQuestion === 'object' && 'id' in rawQuestion) {
        try {
          const { normalizeApiQuestion } = await import('./contract-bridge');
          return normalizeApiQuestion(rawQuestion) as T;
        } catch (error) {
          console.error('Failed to normalize mock question:', error);
          return rawQuestion as T;
        }
      }
      return rawQuestion as T;
    }
    
    if (path.match(/^\/sessions\/(.+)\/answer$/) && method === 'POST') {
      const sessionId = path.match(/^\/sessions\/(.+)\/answer$/)?.[1];
      return handleSubmitAnswer(sessionId!, body) as T;
    }
    
    if (path.match(/^\/sessions\/(.+)\/finish$/) && method === 'POST') {
      return handleFinishSession() as T;
    }
    
    if (path.match(/^\/sessions\/(.+)\/submit$/) && method === 'POST') {
      const sessionId = path.match(/^\/sessions\/(.+)\/submit$/)?.[1];
      return handleSubmitSession(sessionId!) as T;
    }
    
    // ADMIN ENDPOINTS
    if (path === '/admin/results' && method === 'GET') {
      return handleGetResults() as T;
    }
    
    if (path.match(/^\/admin\/results\/(.+)$/) && method === 'GET') {
      const resultId = path.match(/^\/admin\/results\/(.+)$/)?.[1];
      return handleGetResultDetail(resultId!) as T;
    }
    
    if (path.match(/^\/admin\/results\/(.+)\/pdf$/) && method === 'GET') {
      return handleGetResultPdf() as T;
    }
    
    if (path === '/admin/ai/analyze' && method === 'POST') {
      return handleAiAnalyze(body) as T;
    }
    
    throw new Error(`Mock router: Unhandled endpoint ${method} ${path}`);
  } catch (error) {
    console.error('[MOCK ROUTER] Error:', error);
    throw error;
  }
}

/**
 * Mock handler functions for demo mode
 */

// Session management

function generateSessionId(): string {
  return `demo-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

function handleSessionStart(body: any) {
  const nationalId = body.nationalId || body.NationalId || '1234567890';
  const sessionId = generateSessionId();
  
  // Check for existing session to support resume
  const existingSession = getStoredSession(nationalId);
  if (existingSession && existingSession.answers.length < questionsData.length) {
    return {
      sessionId: existingSession.sessionId,
      totalQuestions: questionsData.length,
      resume: true,
      currentIndex: existingSession.answers.length,
      user: {
        fullName: "مستخدم تجريبي",
        nationalId
      }
    };
  }
  
  // Create new session
  const sessionData = {
    sessionId,
    nationalId,
    startedAt: new Date().toISOString(),
    answers: [],
    currentIndex: 0
  };
  
  localStorage.setItem(`demo_session_${sessionId}`, JSON.stringify(sessionData));
  localStorage.setItem(`demo_session_by_id_${nationalId}`, sessionId);
  
  return {
    sessionId,
    totalQuestions: questionsData.length,
    resume: false,
    user: {
      fullName: "مستخدم تجريبي",
      nationalId
    }
  };
}

function handleGetNextQuestion(sessionId: string) {
  const session = getStoredSessionById(sessionId);
  if (!session) {
    throw new Error('Session not found');
  }
  
  const currentIndex = session.answers.length;
  if (currentIndex >= questionsData.length) {
    return { message: 'completed' };
  }
  
  const question = questionsData[currentIndex];
  return {
    id: question.id,
    item_id: question.item_id,
    text_ar: question.text_ar,
    type: question.type,
    dimension_tags: question.dimension_tags,
    difficulty: question.difficulty,
    time_limit_seconds: question.time_limit_seconds,
    max_score: question.max_score,
    options: question.options || null,
    orderingChoices: question.orderingChoices || null,
    orderingLabels: question.orderingLabels || null,
    correctAnswer: question.correctAnswer || null
  };
}

function handleSubmitAnswer(sessionId: string, body: any) {
  const session = getStoredSessionById(sessionId);
  if (!session) {
    throw new Error('Session not found');
  }
  
  const answer = {
    itemId: body.ItemId || body.itemId,
    answer: body.Answer || body.answer,
    numericAnswer: body.NumericAnswer || body.numericAnswer,
    responseTimeMs: body.ResponseTimeMs || body.responseTimeMs || 1000,
    submittedAt: new Date().toISOString()
  };
  
  session.answers.push(answer);
  localStorage.setItem(`demo_session_${sessionId}`, JSON.stringify(session));
  
  return { 
    message: 'ok', 
    skipToNext: true 
  };
}

function handleFinishSession() {
  return { finished: true };
}

function handleSubmitSession(sessionId: string) {
  const session = getStoredSessionById(sessionId);
  if (!session) {
    throw new Error('Session not found');
  }
  
  // Calculate scores based on answers
  const scores = calculateMockScores(session.answers);
  
  // Store result for admin interface
  const result = {
    resultId: Date.now(),
    sessionId,
    nationalId: session.nationalId,
    fullName: "مستخدم تجريبي",
    totalScore: scores.totalScore,
    createdAt: new Date().toISOString(),
    scoringModelVersion: "Demo-v1.0",
    dimensions: scores.dimensions
  };
  
  // Store in localStorage for admin access
  const existingResults = JSON.parse(localStorage.getItem('demo_results') || '[]');
  existingResults.push(result);
  localStorage.setItem('demo_results', JSON.stringify(existingResults));
  
  // Mark session as completed
  session.completed = true;
  localStorage.setItem(`demo_session_${sessionId}`, JSON.stringify(session));
  
  return { 
    message: 'submitted',
    sessionId,
    success: true
  };
}

function handleGetResults() {
  const storedResults = JSON.parse(localStorage.getItem('demo_results') || '[]');
  const allResults = [...resultsData, ...storedResults];
  
  return {
    data: allResults.slice(0, 15), // Return first 15 for pagination demo
    total: allResults.length,
    page: 1,
    pageSize: 15
  };
}

function handleGetResultDetail(resultId: string) {
  const storedResults = JSON.parse(localStorage.getItem('demo_results') || '[]');
  const allResults = [...resultsData, ...storedResults];
  
  const result = allResults.find(r => r.resultId.toString() === resultId);
  if (!result) {
    throw new Error('Result not found');
  }
  
  return result;
}

function handleGetResultPdf() {
  // Return a simple blob representing a PDF
  const pdfContent = "JVBERi0xLjQKJdPr6eEKMSAwIG9iago8PAovVHlwZSAvQ2F0YWxvZwovUGFnZXMgMiAwIFIKPj4KZW5kb2JqCjIgMCBvYmoKPDwKL1R5cGUgL1BhZ2VzCi9LaWRzIFszIDAgUl0KL0NvdW50IDEKL01lZGlhQm94IFswIDAgNTk1IDg0Ml0KPj4KZW5kb2JqCjMgMCBvYmoKPDwKL1R5cGUgL1BhZ2UKL1BhcmVudCAyIDAgUgovUmVzb3VyY2VzIDw8Ci9Gb250IDw8Ci9GMSANCj4+Cj4+Ci9Db250ZW50cyA0IDAgUgo+PgplbmRvYmoKNCAwIG9iago8PAovTGVuZ3RoIDQ0MAo+PgpzdHJlYW0KQlQKNzAgNzAwIFRkCi9GMSA0OCBUZgooVGVzdCBSZXBvcnQpIFRqCkVUCmVuZHN0cmVhbQplbmRvYmoKeHJlZgowIDUKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwMDA5IDAwMDAwIG4gCjAwMDAwMDAwNzQgMDAwMDAgbiAKMDAwMDAwMDE3MyAwMDAwMCBuIAowMDAwMDAwMzMwIDAwMDAwIG4gCnRyYWlsZXIKPDwKL1NpemUgNQovUm9vdCAxIDAgUgo+PgpzdGFydHhyZWYKNDIxCiUlRU9G";
  
  // Convert base64 to blob
  const byteCharacters = atob(pdfContent);
  const byteNumbers = new Array(byteCharacters.length);
  for (let i = 0; i < byteCharacters.length; i++) {
    byteNumbers[i] = byteCharacters.charCodeAt(i);
  }
  const byteArray = new Uint8Array(byteNumbers);
  
  return new Blob([byteArray], { type: 'application/pdf' });
}

function handleAiAnalyze(body: any) {
  const resultId = body.resultId;
  
  // Check if we have OpenAI key for real analysis
  const env = (import.meta as unknown as { env?: Record<string, string | undefined> }).env;
  const hasOpenAiKey = Boolean(env?.VITE_OPENAI_KEY);
  
  if (hasOpenAiKey) {
    // TODO: Implement actual OpenAI call via serverless function
    console.log('Would call OpenAI API for result:', resultId);
  }
  
  // For demo, use mock recommendations
  const result = handleGetResultDetail(resultId.toString());
  return generateMockRecommendations(resultId, result.nationalId, result.dimensions);
}

// Helper functions
function getStoredSession(nationalId: string) {
  const sessionId = localStorage.getItem(`demo_session_by_id_${nationalId}`);
  if (!sessionId) return null;
  
  const session = localStorage.getItem(`demo_session_${sessionId}`);
  return session ? JSON.parse(session) : null;
}

function getStoredSessionById(sessionId: string) {
  const session = localStorage.getItem(`demo_session_${sessionId}`);
  return session ? JSON.parse(session) : null;
}

function calculateMockScores(answers: any[]) {
  // Simple scoring algorithm for demo - could use answers for more realistic scoring
  const dimensions = [
    { name: "الذكاء العام", weight: 0.25 },
    { name: "الاستقرار العاطفي", weight: 0.20 },
    { name: "الانفتاح على التجارب", weight: 0.20 },
    { name: "الضمير الحي", weight: 0.20 },
    { name: "الانبساطية", weight: 0.15 }
  ];
  
  let totalScore = 0;
  
  // Use answer count and types to influence scores somewhat realistically
  const answerCount = answers.length;
  const scoreModifier = Math.min(1.0, answerCount / 80); // Normalize by expected total questions
  
  const dimensionScores = dimensions.map(dim => {
    // Generate realistic scores influenced by completion rate
    const baseScore = 45 + (Math.random() * 20 * scoreModifier); // 45-65 range, affected by completion
    const variance = (Math.random() - 0.5) * 10; // -5 to +5 variance
    const tScore = Math.max(40, Math.min(70, baseScore + variance));
    
    totalScore += tScore * dim.weight;
    
    return {
      dimension: dim.name,
      raw: Math.round(tScore * 0.8), // Raw score approximation
      z: (tScore - 50) / 10, // Z-score
      t: Math.round(tScore * 10) / 10,
      percentile: Math.round(((tScore - 30) / 40) * 100)
    };
  });
  
  return {
    totalScore: Math.round(totalScore * 10) / 10,
    dimensions: dimensionScores
  };
}

/**
 * Enhanced fetch with contract validation and retry logic
 */
export async function apiFetchJson<T>(
  path: string, 
  init: RequestInit = {},
  contractOptions: ContractOptions<T> = {},
  retryOptions: RetryOptions = {}
): Promise<T> {
  console.log('[apiFetchJson] Starting request:', { path, endpoint: contractOptions.endpoint });
  
  // Use mock router in demo mode
  if (isDemoMode()) {
    console.log('[apiFetchJson] Demo mode active, using mock router');
    return await mockRouter<T>(path, init);
  }
  
  const { schema, endpoint = path, normalizer, skipValidation = false } = contractOptions;
  const { alternateBody, maxRetries = 1 } = retryOptions;
  
  let lastError: Error | null = null;
  let attempts = 0;
  
  while (attempts <= maxRetries) {
    try {
      // Use alternate body on retry if available
      const currentInit = attempts > 0 && alternateBody 
        ? { ...init, body: JSON.stringify(alternateBody) }
        : init;
      
      const url = buildUrl(path);
      const options: RequestInit = {
        ...currentInit,
        headers: buildHeaders(currentInit)
      };

      console.log('[apiFetchJson] Making request:', { url, options });
      const response = await fetch(url, options);
      console.log('[apiFetchJson] Response received:', { status: response.status, statusText: response.statusText });
      const text = await response.text();
      let data: unknown = null;
      
      if (text) {
        try {
          data = JSON.parse(text);
        } catch {
          data = text;
        }
      }

      if (!response.ok) {
        const message = (data && typeof data === "object" && "error" in (data as Record<string, unknown>))
          ? String((data as Record<string, unknown>).error)
          : `Request failed with status ${response.status}`;
        
        // If this is a 400/404 and we have an alternate to try, don't throw yet
        if (attempts === 0 && (response.status === 400 || response.status === 404) && alternateBody) {
          lastError = new ApiError(message, response.status, data);
          logContractMismatch(endpoint, 'Request failed, trying alternate body format', { originalBody: init.body, alternateBody });
          attempts++;
          continue;
        }
        
        throw new ApiError(message, response.status, data);
      }

      // Validate response if schema provided
      if (!skipValidation && (schema || normalizer)) {
        try {
          let validatedData: T;
          
          if (normalizer) {
            validatedData = normalizer(data);
          } else if (schema) {
            validatedData = schema.parse(data);
          } else {
            validatedData = data as T;
          }
          
          return validatedData;
        } catch (validationError) {
          const expectedFields = schema ? getSchemaFields(schema) : [];
          logContractMismatch(endpoint, 'Response validation failed', { 
            error: validationError, 
            expectedFields, 
            actualData: data 
          });
          
          // In strict dev mode, throw contract errors
          const env = (import.meta as any).env || {};
          if (env.DEV && env.VITE_STRICT_CONTRACTS === 'true') {
            throw new ContractError(
              `Contract validation failed for ${endpoint}`, 
              endpoint, 
              expectedFields, 
              data
            );
          }
          
          // In production, log warning and return raw data
          console.warn(`[CONTRACT WARNING] ${endpoint}: validation failed, using raw data`, validationError);
          return data as T;
        }
      }

      return data as T;
      
    } catch (error) {
      lastError = error as Error;
      
      // If this isn't a retryable error or we're out of retries, throw
      if (!(error instanceof ApiError) || attempts >= maxRetries) {
        throw error;
      }
      
      attempts++;
    }
  }

  // If we exhausted retries, throw the last error
  throw lastError || new Error('Unknown fetch error');
}

/**
 * Log contract mismatches for debugging
 */
function logContractMismatch(endpoint: string, message: string, details: Record<string, unknown>): void {
  const env = (import.meta as any).env || {};
  if (env.DEV) {
    console.warn(`[CONTRACT WARN] ${endpoint}: ${message}`, details);
  }
}

/**
 * Extract field names from Zod schema for better error messages
 */
function getSchemaFields(schema: z.ZodSchema): string[] {
  try {
    if ('shape' in schema && schema.shape && typeof schema.shape === 'object') {
      return Object.keys(schema.shape);
    }
    return [];
  } catch {
    return [];
  }
}
