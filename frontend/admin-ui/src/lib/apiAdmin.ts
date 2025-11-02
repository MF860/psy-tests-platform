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

// API base URL with fallback support for multiple env var names
const getApiBaseUrl = () => {
  const env = (import.meta as any).env;
  return env?.VITE_API_BASE_URL || env?.VITE_API_BASE || 'http://localhost:5019/api';
};

const api = {
  get: async (url: string, config?: any) => {
    if (isDemoMode()) {
      return mockAdminApi('GET', url);
    }
    // AI analysis can take longer, so increase timeout
    const timeout = url.includes('/ai-analyze') ? 60000 : 15000;
    const axiosInstance = axios.create({
      baseURL: getApiBaseUrl(),
      timeout: timeout,
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
    // AI analysis can take longer, so increase timeout
    const timeout = url.includes('/ai-analyze') ? 60000 : 15000;
    const axiosInstance = axios.create({
      baseURL: getApiBaseUrl(),
      timeout: timeout,
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
   * AI health check
   */
  aiHealth: async (): Promise<{
    enabled: boolean
    provider: string
    model: string
    hasKey: boolean
    status: string
    message: string
  }> => {
    if (isDemoMode()) {
      return {
        enabled: true,
        provider: 'Demo',
        model: 'demo-mock',
        hasKey: true,
        status: 'ready',
        message: 'خدمة الذكاء الاصطناعي التجريبية جاهزة'
      };
    }
    
    try {
      const response = await api.get('/admin/ai/health');
      return response.data;
    } catch (error) {
      console.error('[AdminApi] AI health check failed:', error);
      return {
        enabled: false,
        provider: 'Unknown',
        model: 'Unknown',
        hasKey: false,
        status: 'error',
        message: 'فشل في فحص حالة خدمة الذكاء الاصطناعي'
      };
    }
  },

  /**
   * AI analysis for result using DeepSeek with SDJ-7 patterns
   */
  aiAnalyze: async (id: number): Promise<import('./adminContract').SdjAiAnalysisResponse> => {
    if (isDemoMode()) {
      // Return mock SDJ-7 data in demo mode
      await new Promise(resolve => setTimeout(resolve, 1000 + Math.random() * 2000));
      
      const result = getMockResultDetail(id);
      
      // Generate mock 7-pattern data
      const patterns: import('./adminContract').SdjPatternAnalysis[] = [
        {
          key: "personality_patterns",
          label: "الأنماط الشخصية",
          tScore: 58.3,
          band: "قوي",
          insights: [
            "يظهر توازن جيد بين الانفتاح والضمير الحي",
            "قدرة على التكيف مع المواقف المختلفة"
          ],
          risks: ["قد تحتاج إلى تطوير الثقة بالنفس في مواقف جديدة"],
          recommendations: [
            "شارك في ورش عمل تطوير الذات",
            "اعمل على تحديد أهداف شخصية واضحة"
          ]
        },
        {
          key: "cognitive_mental",
          label: "القدرات المعرفية والعقلية",
          tScore: 62.1,
          band: "قوي",
          insights: [
            "قدرات تحليلية ممتازة",
            "مهارات حل المشكلات فوق المتوسط"
          ],
          risks: [],
          recommendations: [
            "استثمر في التدريب المتخصص",
            "شارك معرفتك مع الآخرين"
          ]
        },
        {
          key: "psychological_patterns",
          label: "الأنماط النفسية",
          tScore: 49.7,
          band: "متوسط",
          insights: ["مستوى متوازن من المرونة النفسية"],
          risks: ["قد تواجه تحديات في إدارة الضغوط العالية"],
          recommendations: [
            "مارس تقنيات الاسترخاء",
            "احصل على دعم نفسي عند الحاجة"
          ]
        },
        {
          key: "behavioral_patterns",
          label: "الأنماط السلوكية",
          tScore: 55.4,
          band: "قوي",
          insights: [
            "مهارات تواصل جيدة",
            "قدرة على بناء علاقات فعالة"
          ],
          risks: [],
          recommendations: ["طور مهارات القيادة الجماعية"]
        },
        {
          key: "numerical_logical",
          label: "الأنماط العددية والمنطقية",
          tScore: 51.0,
          band: "متوسط",
          insights: ["قدرات منطقية أساسية جيدة"],
          risks: ["قد تحتاج لتحسين المهارات الكمية"],
          recommendations: [
            "تدرب على التحليل الإحصائي",
            "حل تمارين منطقية منتظمة"
          ]
        },
        {
          key: "leadership_organizational",
          label: "الأنماط القيادية والتنظيمية",
          tScore: 57.2,
          band: "قوي",
          insights: [
            "إمكانيات قيادية واعدة",
            "مهارات تنظيمية جيدة"
          ],
          risks: [],
          recommendations: [
            "شارك في برامج تطوير القيادة",
            "تدرب على اتخاذ القرارات المعقدة"
          ]
        },
        {
          key: "professional_readiness",
          label: "الاستعدادات المهنية العامة",
          tScore: 46.8,
          band: "متوسط",
          insights: ["استعداد أساسي للبيئة المهنية"],
          risks: ["قد تحتاج لمزيد من الخبرة العملية"],
          recommendations: [
            "ابحث عن فرص تدريبية",
            "طور مهارات التعامل مع ضغوط العمل"
          ]
        }
      ];

      const charts: import('./adminContract').SdjChartData = {
        radar: {
          series: [{
            name: "T-Score",
            data: patterns.map(p => p.tScore)
          }],
          labels: patterns.map(p => p.label)
        },
        bars: {
          data: result.dimensions.slice(0, 10).map(d => ({
            label: d.dimension,
            t: d.t
          }))
        }
      };

      return {
        summary: `التحليل يظهر أداءً إجمالياً جيداً مع نقاط قوة واضحة في القدرات المعرفية والأنماط القيادية. 
                  يُنصح بالتركيز على تطوير الاستعدادات المهنية والمرونة النفسية لتحقيق التوازن الأمثل.`,
        patterns,
        subDimensions: result.dimensions.slice(0, 15).map(d => ({
          key: d.dimension,
          label: d.dimension,
          patternKey: "personality_patterns",
          tScore: d.t,
          band: d.t >= 55 ? "قوي" : d.t >= 40 ? "متوسط" : "ضعيف",
          comment: `الأداء ${d.t >= 55 ? "جيد جداً" : d.t >= 40 ? "متوسط" : "يحتاج تطوير"} في هذا البعد`
        })),
        charts,
        model: 'demo-sdj7-mock',
        usage: {
          promptTokens: 450,
          completionTokens: 320,
          totalTokens: 770,
          latencyMs: 1800
        },
        generatedAt: new Date().toISOString()
      };
    }
    
    try {
      const response = await api.post('/admin/ai/analyze', { resultId: id });
      return response.data as import('./adminContract').SdjAiAnalysisResponse;
    } catch (error: any) {
      console.error('[AdminApi] AI analyze failed:', error);
      
      // Map backend errors to frontend-friendly messages
      if (error?.response?.status === 400) {
        const backendError = error?.response?.data?.error;
        throw new Error(backendError || 'طلب غير صالح - تحقق من نوع النتيجة');
      } else if (error?.response?.status === 502 || error?.response?.status === 503) {
        throw new Error('خدمة الذكاء الاصطناعي غير متوفرة حالياً');
      } else if (error?.response?.status === 404) {
        throw new Error('لم يتم العثور على النتيجة المطلوبة');
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
    scoreDistribution: [
      { score: '0-20', count: Math.floor(totalResults * 0.05) },
      { score: '21-40', count: Math.floor(totalResults * 0.15) },
      { score: '41-60', count: Math.floor(totalResults * 0.30) },
      { score: '61-80', count: Math.floor(totalResults * 0.35) },
      { score: '81-100', count: Math.floor(totalResults * 0.15) }
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