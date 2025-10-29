/**
 * Admin API data contract with safe mapping and validation
 * Handles PascalCase -> camelCase conversion and provides fallbacks
 */

export interface AnalyticsSummary {
  totalUsers: number
  totalSessions: number
  totalResults: number
  resultsToday: number
  avgTotalScore: number
  byDimension: Array<{ dimension: string; tScore: number }>
  byTypeAccuracy: Array<{ type: string; accuracy: number; avgTimeMs: number }>
  scoreDistribution: Array<{ score: string; count: number }>
  recentSessions: Array<{ 
    sessionId: number | string
    nationalId: string
    score: number
    createdAt: string 
  }>
}

/**
 * Safe number conversion - handles NaN, null, undefined
 */
function safeNumber(value: any, defaultValue: number = 0): number {
  if (typeof value === 'number' && !isNaN(value)) return value
  if (typeof value === 'string') {
    const parsed = parseFloat(value)
    return isNaN(parsed) ? defaultValue : parsed
  }
  return defaultValue
}

/**
 * Safe string conversion
 */
function safeString(value: any, defaultValue: string = ''): string {
  return value != null ? String(value) : defaultValue
}

/**
 * Safe array conversion
 */
function safeArray<T>(value: any, defaultValue: T[] = []): T[] {
  return Array.isArray(value) ? value : defaultValue
}

/**
 * Known question types in order
 */
const KNOWN_TYPES = [
  'MCQ',
  'TIMED_NUMERIC', 
  'ORDERING',
  'LikertAgreement',
  'Frequency',
  'Text'
]

/**
 * Maps raw API analytics data to our contract
 * Handles both PascalCase and camelCase input
 */
export function mapAnalytics(raw: any): AnalyticsSummary {
  if (!raw || typeof raw !== 'object') {
    console.warn('[ADMIN CONTRACT] Invalid analytics data received, using defaults')
    return getDefaultAnalytics()
  }

  // Check if we have PascalCase and warn once
  const hasPascalCase = raw.TotalUsers !== undefined || 
                        raw.ByDimension !== undefined || 
                        raw.TScore !== undefined

  if (hasPascalCase) {
    console.warn('[ADMIN CONTRACT] mapped PascalCase → camelCase for analytics')
  }

  // Extract values with PascalCase fallbacks
  const totalUsers = safeNumber(raw.totalUsers ?? raw.TotalUsers)
  const totalSessions = safeNumber(raw.totalSessions ?? raw.TotalSessions)
  const totalResults = safeNumber(raw.totalResults ?? raw.TotalResults)
  const resultsToday = safeNumber(raw.resultsToday ?? raw.ResultsToday)
  const avgTotalScore = safeNumber(raw.avgTotalScore ?? raw.AvgTotalScore)

  // Process dimensions array
  const rawDimensions = safeArray(raw.byDimension ?? raw.ByDimension)
  const byDimension = rawDimensions
    .map((item: any) => ({
      dimension: safeString(item.dimension ?? item.Dimension),
      tScore: safeNumber(item.tScore ?? item.TScore ?? item.t)
    }))
    .filter(item => item.dimension) // Remove empty dimensions
    .sort((a, b) => a.dimension.localeCompare(b.dimension)) // Sort alphabetically

  // Process type accuracy array
  const rawTypeAccuracy = safeArray(raw.byTypeAccuracy ?? raw.ByTypeAccuracy)
  const byTypeAccuracy = rawTypeAccuracy
    .map((item: any) => ({
      type: safeString(item.type ?? item.Type),
      accuracy: safeNumber(item.accuracy ?? item.Accuracy, 0),
      avgTimeMs: safeNumber(item.avgTimeMs ?? item.AvgTimeMs ?? item.avgTime, 0)
    }))
    .filter(item => item.type) // Remove empty types
    .sort((a, b) => {
      // Sort by known types order first, then alphabetically
      const aIndex = KNOWN_TYPES.indexOf(a.type)
      const bIndex = KNOWN_TYPES.indexOf(b.type)
      
      if (aIndex !== -1 && bIndex !== -1) return aIndex - bIndex
      if (aIndex !== -1) return -1
      if (bIndex !== -1) return 1
      return a.type.localeCompare(b.type)
    })

  // Process recent sessions
  const rawSessions = safeArray(raw.recentSessions ?? raw.RecentSessions)
  const recentSessions = rawSessions
    .map((item: any) => ({
      sessionId: item.sessionId ?? item.SessionId ?? 0,
      nationalId: safeString(item.nationalId ?? item.NationalId),
      score: safeNumber(item.score ?? item.Score),
      createdAt: safeString(item.createdAt ?? item.CreatedAt)
    }))
    .filter(item => item.nationalId) // Must have nationalId
    .slice(0, 10) // Limit to 10 most recent

  // Process score distribution
  const rawDistribution = safeArray(raw.scoreDistribution ?? raw.ScoreDistribution)
  const scoreDistribution = rawDistribution
    .map((item: any) => ({
      score: safeString(item.score ?? item.Score),
      count: safeNumber(item.count ?? item.Count)
    }))
    .filter(item => item.score) // Must have score label

  return {
    totalUsers,
    totalSessions,
    totalResults,
    resultsToday,
    avgTotalScore,
    byDimension,
    byTypeAccuracy,
    scoreDistribution,
    recentSessions
  }
}

/**
 * Default analytics when no data available
 */
function getDefaultAnalytics(): AnalyticsSummary {
  return {
    totalUsers: 0,
    totalSessions: 0,
    totalResults: 0,
    resultsToday: 0,
    avgTotalScore: 0,
    byDimension: [],
    byTypeAccuracy: [],
    scoreDistribution: [],
    recentSessions: []
  }
}

/**
 * Result List Item contract
 */
export interface ResultListItemUI {
  resultId: number | string
  sessionId?: number | string
  nationalId: string
  fullName?: string
  totalScore: number
  createdAt: string
  sdjProfile?: string // Top strength from SDJ if available
  hasSdjData?: boolean
}

/**
 * Maps raw result list item to UI contract
 */
export function mapResultListItem(raw: any): ResultListItemUI {
  if (!raw || typeof raw !== 'object') {
    console.warn('[ADMIN CONTRACT] Invalid result item received, using defaults')
    return {
      resultId: '',
      nationalId: '',
      totalScore: 0,
      createdAt: ''
    }
  }

  // Helper to get value with PascalCase fallback
  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  // Check for SDJ data
  const sdjData = get('sdjData', 'SdjData');
  const hasSdjData = sdjData && sdjData.SubDimensions && sdjData.SubDimensions.length > 0;
  const sdjProfile = hasSdjData && sdjData.SubDimensions.length > 0
    ? sdjData.SubDimensions.sort((a: any, b: any) => b.T - a.T)[0]?.SubDimension
    : undefined;

  return {
    resultId: get('resultId', 'ResultId') ?? '',
    sessionId: get('sessionId', 'SessionId'),
    nationalId: safeString(get('nationalId', 'NationalId')),
    fullName: get('fullName', 'FullName') || undefined,
    totalScore: safeNumber(get('totalScore', 'TotalScore')),
    createdAt: safeString(get('createdAt', 'CreatedAt')),
    sdjProfile,
    hasSdjData: !!hasSdjData
  }
}

/**
 * Maps raw result list response to UI contract
 */
export function mapResultListResponse(raw: any) {
  if (!raw || typeof raw !== 'object') {
    console.warn('[ADMIN CONTRACT] Invalid results response received')
    return { data: [], total: 0 }
  }

  // Check if we have PascalCase and warn once
  const hasPascalCase = raw.Data !== undefined || raw.Total !== undefined

  if (hasPascalCase) {
    console.warn('[ADMIN CONTRACT] mapped PascalCase → camelCase for results list')
  }

  const rawData = raw.data ?? raw.Data ?? []
  const data = Array.isArray(rawData) ? rawData.map(mapResultListItem) : []
  const total = safeNumber(raw.total ?? raw.Total ?? data.length)

  return { data, total }
}

/**
 * Result Detail contract
 */
export interface ResultDetailUI {
  resultId: number | string
  sessionId?: number | string
  sessionGuid?: string
  nationalId: string
  fullName?: string
  totalScore: number
  createdAt: string
  scoringModelVersion?: string
  dimensions: Array<{
    dimension: string
    raw: number
    z: number
    t: number
    percentile: number
  }>
  sdjData?: {
    Dimensions: Array<{
      Dimension: string
      Raw: number
      T: number
      Percentile: number
      Band: 'Weak' | 'Average' | 'Excellent'
      SubDimensions?: Array<{
        Dimension: string
        SubDimension: string
        T: number
        Band: 'Weak' | 'Average' | 'Excellent'
      }>
    }>
    SubDimensions: Array<{
      Dimension: string
      SubDimension: string
      T: number
      Band: 'Weak' | 'Average' | 'Excellent'
    }>
    TrackFits: Array<{
      trackNameAr: string
      trackNameEn: string
      fitLevel: 'high' | 'medium' | 'low'
      fitScore: number
      reasoningAr: string
      keyCompetencies: string[]
    }>
    SevenPatternScores?: Array<{
      PatternNameAr: string
      PatternNameEn: string
      TScore: number
      Band: string
      SubDimensions: string[]
    }>
    Version?: string
  }
}

/**
 * Maps raw result detail to UI contract
 */
export function mapResultDetail(raw: any): ResultDetailUI {
  if (!raw || typeof raw !== 'object') {
    console.warn('[ADMIN CONTRACT] Invalid result detail received, using defaults')
    return {
      resultId: '',
      nationalId: '',
      totalScore: 0,
      createdAt: '',
      dimensions: []
    }
  }

  // Helper to get value with PascalCase fallback
  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  // Process dimensions array with safe mapping
  const rawDimensions = safeArray(get('dimensions', 'Dimensions'))
  const dimensions = rawDimensions
    .map((dim: any) => ({
      dimension: safeString(dim.dimension ?? dim.Dimension),
      raw: safeNumber(dim.raw ?? dim.Raw),
      z: safeNumber(dim.z ?? dim.Z),
      t: safeNumber(dim.t ?? dim.T),
      percentile: safeNumber(dim.percentile ?? dim.Percentile)
    }))
    .filter(dim => dim.dimension) // Remove empty dimensions
    .sort((a, b) => b.t - a.t) // Sort by T-score descending

  return {
    resultId: get('resultId', 'ResultId') ?? '',
    sessionId: get('sessionId', 'SessionId'),
    sessionGuid: get('sessionGuid', 'SessionGuid'),
    nationalId: safeString(get('nationalId', 'NationalId')),
    fullName: get('fullName', 'FullName') || undefined,
    totalScore: safeNumber(get('totalScore', 'TotalScore')),
    createdAt: safeString(get('createdAt', 'CreatedAt')),
    scoringModelVersion: get('scoringModelVersion', 'ScoringModelVersion'),
    dimensions,
    sdjData: get('sdjData', 'SdjData') || undefined
  }
}

/**
 * OpenAI Recommendations contracts
 */
export interface RecommendationsRequestUI {
  context?: string
  language?: string
  forceRegenerate?: boolean
}

export interface RecommendationsResponseUI {
  resultId: number
  participantId: string
  generatedAt: string
  modelVersion: string
  fromCache: boolean
  summary: OverallSummaryUI
  strengths: StrengthInsightUI[]
  growthAreas: GrowthAreaUI[]
  recommendations: DevelopmentRecommendationUI[]
  courses: CourseRecommendationUI[]
}

export interface OverallSummaryUI {
  profileType: string
  description: string
  overallScore: number
  performanceLevel: string
  keyCharacteristics: string[]
}

export interface StrengthInsightUI {
  dimension: string
  tScore: number
  title: string
  description: string
  applications: string[]
  reinforcementTip: string
}

export interface GrowthAreaUI {
  dimension: string
  tScore: number
  title: string
  challenge: string
  improvementStrategies: string[]
  priority: string
}

export interface DevelopmentRecommendationUI {
  category: string
  title: string
  description: string
  actionSteps: string[]
  timeline: string
  priority: string
  expectedOutcomes: string[]
}

export interface CourseRecommendationUI {
  name: string
  description: string
  provider: string
  duration: string
  level: string
  topics: string[]
  relevancyReason: string
  externalUrl?: string
}

export interface OpenAIUsageMetricsUI {
  requestsToday: number
  tokensUsedToday: number
  costToday: number
  requestsThisMonth: number
  tokensUsedThisMonth: number
  costThisMonth: number
  lastRequest: string
  averageResponseTime: number
  cacheHitRate: number
}

/**
 * Maps raw recommendations response to UI contract
 */
export function mapRecommendationsResponse(raw: any): RecommendationsResponseUI {
  if (!raw || typeof raw !== 'object') {
    console.warn('[ADMIN CONTRACT] Invalid recommendations response received')
    throw new Error('Invalid recommendations response')
  }

  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  return {
    resultId: safeNumber(get('resultId', 'ResultId')),
    participantId: safeString(get('participantId', 'ParticipantId')),
    generatedAt: safeString(get('generatedAt', 'GeneratedAt')),
    modelVersion: safeString(get('modelVersion', 'ModelVersion')),
    fromCache: Boolean(get('fromCache', 'FromCache')),
    summary: mapOverallSummary(get('summary', 'Summary')),
    strengths: safeArray(get('strengths', 'Strengths')).map(mapStrengthInsight),
    growthAreas: safeArray(get('growthAreas', 'GrowthAreas')).map(mapGrowthArea),
    recommendations: safeArray(get('recommendations', 'Recommendations')).map(mapDevelopmentRecommendation),
    courses: safeArray(get('courses', 'Courses')).map(mapCourseRecommendation)
  }
}

function mapOverallSummary(raw: any): OverallSummaryUI {
  if (!raw) return {
    profileType: '',
    description: '',
    overallScore: 0,
    performanceLevel: '',
    keyCharacteristics: []
  }

  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  return {
    profileType: safeString(get('profileType', 'ProfileType')),
    description: safeString(get('description', 'Description')),
    overallScore: safeNumber(get('overallScore', 'OverallScore')),
    performanceLevel: safeString(get('performanceLevel', 'PerformanceLevel')),
    keyCharacteristics: safeArray(get('keyCharacteristics', 'KeyCharacteristics'))
  }
}

function mapStrengthInsight(raw: any): StrengthInsightUI {
  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  return {
    dimension: safeString(get('dimension', 'Dimension')),
    tScore: safeNumber(get('tScore', 'TScore')),
    title: safeString(get('title', 'Title')),
    description: safeString(get('description', 'Description')),
    applications: safeArray(get('applications', 'Applications')),
    reinforcementTip: safeString(get('reinforcementTip', 'ReinforcementTip'))
  }
}

function mapGrowthArea(raw: any): GrowthAreaUI {
  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  return {
    dimension: safeString(get('dimension', 'Dimension')),
    tScore: safeNumber(get('tScore', 'TScore')),
    title: safeString(get('title', 'Title')),
    challenge: safeString(get('challenge', 'Challenge')),
    improvementStrategies: safeArray(get('improvementStrategies', 'ImprovementStrategies')),
    priority: safeString(get('priority', 'Priority'))
  }
}

function mapDevelopmentRecommendation(raw: any): DevelopmentRecommendationUI {
  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  return {
    category: safeString(get('category', 'Category')),
    title: safeString(get('title', 'Title')),
    description: safeString(get('description', 'Description')),
    actionSteps: safeArray(get('actionSteps', 'ActionSteps')),
    timeline: safeString(get('timeline', 'Timeline')),
    priority: safeString(get('priority', 'Priority')),
    expectedOutcomes: safeArray(get('expectedOutcomes', 'ExpectedOutcomes'))
  }
}

function mapCourseRecommendation(raw: any): CourseRecommendationUI {
  const get = (key: string, altKey?: string) => 
    raw[key] ?? (altKey ? raw[altKey] : undefined)

  return {
    name: safeString(get('name', 'Name')),
    description: safeString(get('description', 'Description')),
    provider: safeString(get('provider', 'Provider')),
    duration: safeString(get('duration', 'Duration')),
    level: safeString(get('level', 'Level')),
    topics: safeArray(get('topics', 'Topics')),
    relevancyReason: safeString(get('relevancyReason', 'RelevancyReason')),
    externalUrl: get('externalUrl', 'ExternalUrl') || undefined
  }
}

/**
 * AI Analysis contracts (matching backend with SDJ enhancements)
 */
export interface CategoryAnalysis {
  name: string
  t: number
  note: string
}

export interface AiAnalysis {
  strengths: string[]
  weaknesses: string[]
  recommendations: string[]
  rationale?: string
  summary?: string
  methodology?: string
  categories?: CategoryAnalysis[]
}

export interface AiAnalysisUsage {
  promptTokens: number
  completionTokens: number
  totalTokens: number
  latencyMs: number
}

export interface AiAnalysisResponse {
  analysis: AiAnalysis
  model: string
  usage?: AiAnalysisUsage
  generatedAt?: string
}

/**
 * Formatting helpers for Arabic locale
 */
export const formatters = {
  /**
   * Format number with Arabic digits
   */
  number: (value: number): string => {
    return new Intl.NumberFormat('ar-EG').format(value)
  },

  /**
   * Format percentage with Arabic digits
   */
  percent: (value: number): string => {
    return new Intl.NumberFormat('ar-EG', { 
      style: 'percent',
      minimumFractionDigits: 0,
      maximumFractionDigits: 1
    }).format(value)
  },

  /**
   * Format seconds with Arabic digits
   */
  seconds: (milliseconds: number): string => {
    const seconds = milliseconds / 1000
    return new Intl.NumberFormat('ar-EG', {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1
    }).format(seconds) + ' ث'
  },

  /**
   * Format T-Score with Arabic digits
   */
  tScore: (value: number): string => {
    return new Intl.NumberFormat('ar-EG', {
      minimumFractionDigits: 1,
      maximumFractionDigits: 1
    }).format(value)
  },

  /**
   * Format date/time for Arabic locale
   */
  dateTime: (dateString: string): string => {
    try {
      const date = new Date(dateString)
      return new Intl.DateTimeFormat('ar-SA', {
        dateStyle: 'medium',
        timeStyle: 'short'
      }).format(date)
    } catch {
      return dateString
    }
  }
}