import axios from 'axios'

// Support both VITE_API_BASE_URL and VITE_API_BASE for compatibility
const getApiBaseUrl = () => {
  const env = (import.meta as any).env;
  return env?.VITE_API_BASE_URL || env?.VITE_API_BASE || 'http://localhost:5019/api';
};

const api = axios.create({
  baseURL: getApiBaseUrl(),
  timeout: 15000,
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('admin_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (r) => r,
  (error) => {
    if (error?.response?.status === 401) {
      localStorage.removeItem('admin_token')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

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

export type AnalyticsSummary = {
  totalUsers: number
  totalSessions: number
  totalResults: number
  resultsToday: number
  avgTotalScore: number
  avgResponseTimeMs: number
  activityLast30d: Array<{ date: string; count: number }>
  byDimension: Array<{
    dimension: string
    avg: number
    tScore: number
  }>
  byTypeAccuracy: Array<{
    type: string
    accuracy: number
    avgTimeMs: number
  }>
  recentSessions: Array<{
    sessionId: number
    nationalId: string
    score: number
    createdAt: string
  }>
}

export const AdminApi = {
  login: (username: string, password: string) =>
    api.post<AdminLoginResponse>('/admin/login', { username, password }).then((r) => r.data),

  results: (page = 1, pageSize = 20, search = '') =>
    api
      .get<PagedResponse<ResultListItem>>('/admin/results', { params: { page, pageSize, search } })
      .then((r) => r.data),

  resultDetail: (id: number) =>
    api.get<ResultDetail>(`/admin/results/${id}`).then((r) => r.data),

  resultPdf: (id: number) =>
    api.get(`/admin/results/${id}/pdf`, { responseType: 'blob' }).then((r) => r.data),

  audit: (page = 1, pageSize = 25, search = '', from?: string, to?: string) =>
    api
      .get<PagedResponse<AuditItem>>('/admin/audit', { params: { page, pageSize, search, from, to } })
      .then((r) => r.data),

  changePassword: (oldPassword: string, newPassword: string) =>
    api.post('/admin/change-password', { oldPassword, newPassword }).then((r) => r.data),

  aiAnalyze: (id: number) => api.post(`/admin/results/${id}/ai-analyze`, {}).then((r) => r.data),
  getAnalytics: () => api.get<AnalyticsSummary>('/admin/analytics/overview').then((r) => r.data),
  validateQuestions: () => api.get('/admin/questions/validate').then((r) => r.data),
  fixQuestions: () => api.post('/admin/questions/fix').then((r) => r.data),
}


