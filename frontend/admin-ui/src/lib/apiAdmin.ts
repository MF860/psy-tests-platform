/**
 * Centralized Admin API with contract mapping
 */
import axios from 'axios'
import { 
  mapAnalytics, 
  mapResultDetail, 
  mapRecommendationsResponse,
  type AnalyticsSummary, 
  type ResultDetailUI,
  type RecommendationsRequestUI,
  type RecommendationsResponseUI,
  type OpenAIUsageMetricsUI
} from './adminContract'
import resultsData from '../mocks/results.json'
import { generateMockRecommendations } from '../mocks/recommendations'

// Demo mode configuration
const isDemoMode = () => {
  const env = (import.meta as any).env;
  const demoMode = env?.VITE_DEMO_MODE === 'true';
  console.log('[AdminApi] Demo mode check:', {
    VITE_DEMO_MODE: env?.VITE_DEMO_MODE,
    isDemoMode: demoMode,
    allEnv: env
  });
  return demoMode;
};

// Mock router for demo mode
async function mockAdminApi(method: string, url: string, data?: any) {
  // Simulate network delay
  await new Promise(resolve => setTimeout(resolve, 200 + Math.random() * 300));
  
  if (method === 'POST' && url === '/admin/login') {
    return {
      data: {
        token: 'demo_admin_token_' + Date.now(),
        expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
      }
    };
  }
  
  if (method === 'GET' && url === '/admin/analytics/overview') {
    return { data: getMockAnalytics() };
  }
  
  if (method === 'GET' && url.startsWith('/admin/results/') && !url.includes('pdf')) {
    const id = url.split('/')[3];
    const result = resultsData.find(r => r.resultId.toString() === id);
    return { data: result || resultsData[0] };
  }
  
  if (method === 'GET' && url === '/admin/questions/validate') {
    return { data: { valid: true, count: 80 } };
  }
  
  if (method === 'GET' && url === '/admin/recommendations/usage') {
    return { data: { totalRequests: 150, totalTokens: 45000 } };
  }
  
  // Default response for unhandled endpoints
  return { data: {} };
}

const api = {
  get: async (url: string, config?: any) => {
    if (isDemoMode()) {
      return mockAdminApi('GET', url);
    }
    const axiosInstance = axios.create({
      baseURL: import.meta.env.VITE_API_BASE,
      timeout: 15000,
    });
    const token = localStorage.getItem('admin_token');
    if (token) axiosInstance.defaults.headers.Authorization = `Bearer ${token}`;
    
    try {
      return await axiosInstance.get(url, config);
    } catch (error: any) {
      if (error?.response?.status === 401) {
        console.log('[AdminApi] 401 Unauthorized - clearing token and redirecting to login');
        localStorage.removeItem('admin_token');
        window.location.href = '/login';
      }
      throw error;
    }
  },
  
  post: async (url: string, data?: any, config?: any) => {
    if (isDemoMode()) {
      return mockAdminApi('POST', url, data);
    }
    const axiosInstance = axios.create({
      baseURL: import.meta.env.VITE_API_BASE,
      timeout: 15000,
    });
    const token = localStorage.getItem('admin_token');
    if (token) axiosInstance.defaults.headers.Authorization = `Bearer ${token}`;
    
    try {
      return await axiosInstance.post(url, data, config);
    } catch (error: any) {
      if (error?.response?.status === 401) {
        console.log('[AdminApi] 401 Unauthorized - clearing token and redirecting to login');
        localStorage.removeItem('admin_token');
        window.location.href = '/login';
      }
      throw error;
    }
  }
};

export type AdminLoginResponse = { token: string; expiresAt?: string }
export type PagedResponse<T> = { page: number; pageSize: number; total: number; data: T[] }

export type ResultListItem = {
  resultId: number
  sessionId: number
  totalScore: number
  createdAt: string
  nationalId: string
  fullName: string
}

export type DimensionScore = {
  dimension: string
  raw: number
  z: number
  t: number
  percentile: number
}

export type ResultDetail = {
  resultId: number
  sessionId: number
  totalScore: number
  createdAt: string
  nationalId: string
  fullName: string
  dimensions: DimensionScore[]
  scoringModelVersion: string
}

export type AuditItem = {
  id: number
  action: string
  adminUsername: string
  ipAddress?: string
  details?: string
  createdAt: string
}

export const AdminApi = {
  /**
   * Admin login
   */
  login: async (username: string, password: string): Promise<AdminLoginResponse> => {
    if (isDemoMode()) {
      // In demo mode, accept any credentials and return mock token
      await new Promise(resolve => setTimeout(resolve, 500)); // Simulate network delay
      return {
        token: 'demo_admin_token_' + Date.now(),
        expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString()
      };
    }
    
    try {
      const response = await api.post('/admin/login', { username, password });
      return response.data as AdminLoginResponse;
    } catch (error) {
      console.error('[AdminApi] Login failed:', error);
      throw error;
    }
  },

  /**
   * Get analytics with contract mapping
   */
  getAnalytics: async (): Promise<AnalyticsSummary> => {
    if (isDemoMode()) {
      return getMockAnalytics();
    }
    
    try {
      const response = await api.get('/admin/analytics/overview')
      return mapAnalytics(response.data)
    } catch (error) {
      console.error('[AdminApi] Failed to fetch analytics:', error)
      throw error
    }
  },

  /**
   * Get paginated results with filters and sorting
   */
  results: async (
    page = 1, 
    pageSize = 20, 
    search = '', 
    filters: {
      startDate?: string
      endDate?: string
      minScore?: string
      maxScore?: string
      sortBy?: string
      sortDir?: 'asc' | 'desc'
    } = {}
  ) => {
    if (isDemoMode()) {
      return getMockResults(page, pageSize, search, filters);
    }
    
    const params = {
      page,
      pageSize,
      search,
      ...filters
    }
    
    return api
      .get('/admin/results', { params })
      .then((r) => r.data as PagedResponse<ResultListItem>)
  },

  /**
   * Get result detail by ID with contract mapping
   */
  resultDetail: async (id: number): Promise<ResultDetailUI> => {
    if (isDemoMode()) {
      return getMockResultDetail(id);
    }
    
    try {
      const response = await api.get(`/admin/results/${id}`)
      return mapResultDetail(response.data)
    } catch (error) {
      console.error('[AdminApi] Failed to fetch result detail:', error)
      throw error
    }
  },

  /**
   * Download result PDF
   */
  resultPdf: (id: number) =>
    api.get(`/admin/results/${id}/pdf`, { responseType: 'blob' }).then((r) => r.data),

  /**
   * Get audit log
   */
  audit: (page = 1, pageSize = 25, search = '', from?: string, to?: string) =>
    api
      .get('/admin/audit', { params: { page, pageSize, search, from, to } })
      .then((r) => r.data as PagedResponse<AuditItem>),

  /**
   * Change admin password
   */
  changePassword: (oldPassword: string, newPassword: string) =>
    api.post('/admin/change-password', { oldPassword, newPassword }).then((r) => r.data),

  /**
   * AI analysis for result using OpenRouter/DeepSeek
   */
  aiAnalyze: async (id: number): Promise<import('./adminContract').AiAnalysisResponse> => {
    if (isDemoMode()) {
      // Return mock data in demo mode
      await new Promise(resolve => setTimeout(resolve, 1000 + Math.random() * 2000)); // Simulate network delay
      
      const result = getMockResultDetail(id);
      const topStrengths = result.dimensions.filter(d => d.t >= 60).slice(0, 3);
      const weaknesses = result.dimensions.filter(d => d.t < 40).slice(0, 3);
      
      return {
        analysis: {
          strengths: topStrengths.length > 0 
            ? topStrengths.map(d => `قوة متميزة في ${d.dimension}`)
            : ['نتائج متوازنة بشكل عام'],
          weaknesses: weaknesses.length > 0
            ? weaknesses.map(d => `مجال للتطوير في ${d.dimension}`)
            : ['لا توجد مجالات ضعف واضحة'],
          recommendations: [
            'ركز على تطوير نقاط القوة في مجالات العمل',
            'استمر في التدريب والتطوير المستمر',
            'استشر متخصص لوضع خطة تطوير شاملة'
          ],
          rationale: 'تحليل تجريبي مبني على النتائج الإحصائية'
        },
        model: 'demo-mock',
        usage: {
          promptTokens: 120,
          completionTokens: 80,
          totalTokens: 200,
          latencyMs: 1500
        },
        generatedAt: new Date().toISOString()
      };
    }
    
    try {
      const response = await api.post('/admin/ai/analyze', { resultId: id });
      return response.data;
    } catch (error: any) {
      console.error('[AdminApi] AI analyze failed:', error);
      
      // Map backend errors to frontend-friendly messages
      if (error?.response?.status === 502) {
        throw new Error('خدمة الذكاء الاصطناعي غير متوفرة حالياً');
      } else if (error?.response?.status === 404) {
        throw new Error('لم يتم العثور على النتيجة المطلوبة');
      } else if (error?.response?.status === 400) {
        throw new Error('طلب غير صالح');
      } else {
        throw new Error('فشل في توليد التحليل - يرجى المحاولة لاحقاً');
      }
    }
  },

  /**
   * Validate questions
   */
  validateQuestions: () => 
    api.get('/admin/questions/validate').then((r) => r.data),

  /**
   * Fix questions
   */
  fixQuestions: () => 
    api.post('/admin/questions/fix').then((r) => r.data),

  /**
   * Generate AI-powered recommendations for a result
   */
  getRecommendations: async (
    id: number, 
    request?: RecommendationsRequestUI
  ): Promise<RecommendationsResponseUI> => {
    if (isDemoMode()) {
      return getMockRecommendations(id);
    }
    
    try {
      const response = await api.post(`/admin/results/${id}/recommendations`, request || {})
      return mapRecommendationsResponse(response.data)
    } catch (error) {
      console.error('[AdminApi] Failed to get recommendations:', error)
      throw error
    }
  },

  /**
   * Get OpenAI usage metrics
   */
  getRecommendationsUsage: async (): Promise<OpenAIUsageMetricsUI> => {
    try {
      const response = await api.get('/admin/recommendations/usage')
      return response.data
    } catch (error) {
      console.error('[AdminApi] Failed to get usage metrics:', error)
      throw error
    }
  },
}

/**
 * Mock functions for demo mode
 */

function getMockAnalytics(): AnalyticsSummary {
  const storedResults = JSON.parse(localStorage.getItem('demo_results') || '[]');
  const allResults = [...resultsData, ...storedResults];
  
  const totalResults = allResults.length;
  const avgScore = totalResults > 0 
    ? allResults.reduce((sum, r) => sum + r.totalScore, 0) / totalResults 
    : 0;
  
  return {
    totalUsers: totalResults,
    totalSessions: totalResults,
    totalResults,
    resultsToday: Math.floor(totalResults * 0.1), // Simulate 10% today
    avgTotalScore: Math.round(avgScore * 10) / 10,
    byDimension: [
      { dimension: 'الذكاء العام', tScore: 58.5 },
      { dimension: 'الاستقرار العاطفي', tScore: 56.2 },
      { dimension: 'الانفتاح على التجارب', tScore: 60.1 },
      { dimension: 'الضمير الحي', tScore: 59.8 },
      { dimension: 'الانبساطية', tScore: 55.4 }
    ],
    byTypeAccuracy: [
      { type: 'MCQ', accuracy: 0.78, avgTimeMs: 12500 },
      { type: 'LikertAgreement', accuracy: 0.85, avgTimeMs: 8200 },
      { type: 'Frequency', accuracy: 0.82, avgTimeMs: 9100 },
      { type: 'ORDERING', accuracy: 0.71, avgTimeMs: 25800 },
      { type: 'TIMED_NUMERIC', accuracy: 0.65, avgTimeMs: 15300 },
      { type: 'Text', accuracy: 0.88, avgTimeMs: 45600 }
    ],
    recentSessions: allResults.slice(-5).map(r => ({
      sessionId: r.sessionId,
      nationalId: r.nationalId,
      score: r.totalScore,
      createdAt: r.createdAt
    }))
  };
}

function getMockResults(
  page: number, 
  pageSize: number, 
  search: string, 
  filters: any
): PagedResponse<ResultListItem> {
  const storedResults = JSON.parse(localStorage.getItem('demo_results') || '[]');
  let allResults = [...resultsData, ...storedResults];
  
  // Apply search filter
  if (search) {
    allResults = allResults.filter(r => 
      r.nationalId.includes(search) || 
      r.fullName.toLowerCase().includes(search.toLowerCase())
    );
  }
  
  // Apply score filters
  if (filters.minScore) {
    allResults = allResults.filter(r => r.totalScore >= parseFloat(filters.minScore));
  }
  if (filters.maxScore) {
    allResults = allResults.filter(r => r.totalScore <= parseFloat(filters.maxScore));
  }
  
  // Apply sorting
  if (filters.sortBy) {
    allResults.sort((a, b) => {
      const aVal = (a as any)[filters.sortBy];
      const bVal = (b as any)[filters.sortBy];
      
      if (filters.sortDir === 'desc') {
        return bVal > aVal ? 1 : -1;
      }
      return aVal > bVal ? 1 : -1;
    });
  }
  
  // Apply pagination
  const start = (page - 1) * pageSize;
  const paginatedResults = allResults.slice(start, start + pageSize);
  
  return {
    data: paginatedResults.map(r => ({
      resultId: r.resultId,
      sessionId: r.sessionId || r.resultId,
      totalScore: r.totalScore,
      createdAt: r.createdAt,
      nationalId: r.nationalId,
      fullName: r.fullName
    })),
    total: allResults.length,
    page,
    pageSize
  };
}

function getMockResultDetail(id: number): ResultDetailUI {
  const storedResults = JSON.parse(localStorage.getItem('demo_results') || '[]');
  const allResults = [...resultsData, ...storedResults];
  
  const result = allResults.find(r => r.resultId === id);
  if (!result) {
    throw new Error('Result not found');
  }
  
  return mapResultDetail({
    resultId: result.resultId,
    sessionId: result.sessionId || result.resultId,
    sessionGuid: `guid-${result.resultId}`,
    nationalId: result.nationalId,
    fullName: result.fullName,
    totalScore: result.totalScore,
    createdAt: result.createdAt,
    scoringModelVersion: result.scoringModelVersion || 'Demo-v1.0',
    dimensions: result.dimensions
  });
}

function getMockRecommendations(id: number): RecommendationsResponseUI {
  const result = getMockResultDetail(id);
  
  const mockData = generateMockRecommendations(
    id,
    result.nationalId,
    result.dimensions
  );
  
  return mapRecommendationsResponse(mockData);
}