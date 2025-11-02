import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import Chart from 'react-apexcharts'
import { Download, ArrowLeft, User, Calendar, Award, Target, TrendingUp, Brain } from 'lucide-react'

import { AdminApi } from '@/lib/apiAdmin'
import { 
  type ResultDetailUI, 
  type RecommendationsResponseUI,
  type AiAnalysisResponse,
  formatters 
} from '@/lib/adminContract'
import { Button } from '@/components/ui/button'
import { Section, GridSection } from '@/components/admin/Section'
import { DimensionsGrid } from '@/components/admin/DimensionCard'
import { BandBadge } from '@/components/admin/BandBadge'
import { CopyableNationalId, SessionStatusBadge } from '@/components/admin/ResultBadges'
import { KpiCard } from '@/components/admin/KpiCard'
import { Skeleton } from '@/components/ui/skeleton'

export default function ResultDetailPage() {
  const { id } = useParams()
  const navigate = useNavigate()
  const [data, setData] = useState<ResultDetailUI | undefined>()
  const [loading, setLoading] = useState(false)
  const [pdfLoading, setPdfLoading] = useState(false)
  const rid = Number(id)

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      try {
        const result = await AdminApi.resultDetail(rid)
        setData(result)
      } catch (error) {
        console.error('Failed to load result detail:', error)
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [rid])

  const downloadPdf = async () => {
    setPdfLoading(true)
    try {
      const blob = await AdminApi.resultPdf(rid)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `result_${rid}_${data?.nationalId || 'unknown'}.pdf`
      a.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      console.error('Failed to download PDF:', error)
    } finally {
      setPdfLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50" dir="rtl">
        {/* Sticky Header Skeleton */}
        <div className="sticky top-0 z-10 bg-white border-b border-gray-200 shadow-sm">
          <div className="container mx-auto px-4 py-4">
            <div className="flex items-center justify-between">
              <Skeleton className="h-8 w-32" />
              <Skeleton className="h-10 w-24" />
            </div>
          </div>
        </div>
        
        {/* Content Skeleton */}
        <div className="container mx-auto px-4 py-6 space-y-6">
          <Skeleton className="h-32 w-full" />
          <div className="grid md:grid-cols-3 gap-4">
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-24 w-full" />
          </div>
          <Skeleton className="h-64 w-full" />
        </div>
      </div>
    )
  }

  if (!data) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center" dir="rtl">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-gray-900 mb-2">لم يتم العثور على النتيجة</h2>
          <p className="text-gray-600 mb-4">لا يمكن العثور على النتيجة المطلوبة</p>
          <Button onClick={() => navigate('/results')} variant="outline">
            <ArrowLeft className="h-4 w-4 ml-2" />
            العودة للقائمة
          </Button>
        </div>
      </div>
    )
  }

  // Calculate insights
  const avgTScore = data.dimensions.length 
    ? data.dimensions.reduce((sum, d) => sum + d.t, 0) / data.dimensions.length 
    : 0
  const topStrengths = data.dimensions.filter(d => d.t >= 60).slice(0, 3)
  const growthAreas = data.dimensions.filter(d => d.t < 40).slice(0, 3)
  
  return (
    <div className="min-h-screen bg-gray-50" dir="rtl">
      {/* Sticky Header */}
      <div className="sticky top-0 z-10 bg-white border-b border-gray-200 shadow-sm">
        <div className="container mx-auto px-4 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <Button 
                onClick={() => navigate('/results')} 
                variant="ghost" 
                size="sm"
                className="text-gray-600 hover:text-gray-900"
              >
                <ArrowLeft className="h-4 w-4 ml-2" />
                العودة للقائمة
              </Button>
              <div className="flex items-center gap-2">
                <h1 className="text-xl font-bold text-gray-900">
                  تفاصيل النتيجة #{data.resultId}
                </h1>
                <BandBadge score={avgTScore} variant="compact" />
              </div>
            </div>
            
            <div className="flex items-center gap-2">
              <SessionStatusBadge sessionId={data.sessionGuid} />
              <Button 
                onClick={downloadPdf} 
                disabled={pdfLoading}
                size="sm"
              >
                <Download className="h-4 w-4 ml-2" />
                {pdfLoading ? 'جاري التحميل...' : 'تحميل PDF'}
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="container mx-auto px-4 py-6 space-y-6">
        {/* Participant Info */}
        <Section 
          title="بيانات المشارك"
          subtitle="المعلومات الأساسية والنتيجة الإجمالية"
          className="bg-gradient-to-r from-blue-50 to-indigo-50 border-l-4 border-blue-500"
        >
          <div className="grid md:grid-cols-4 gap-4">
            <div className="flex items-center gap-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <User className="h-5 w-5 text-blue-600" />
              </div>
              <div>
                <p className="text-sm text-gray-600">الاسم الكامل</p>
                <p className="font-semibold text-gray-900">
                  {data.fullName || 'غير محدد'}
                </p>
              </div>
            </div>
            
            <div className="flex items-center gap-3">
              <div className="p-2 bg-green-100 rounded-lg">
                <Target className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <p className="text-sm text-gray-600">الرقم الوطني</p>
                <CopyableNationalId 
                  nationalId={data.nationalId}
                  className="font-semibold text-gray-900"
                />
              </div>
            </div>
            
            <div className="flex items-center gap-3">
              <div className="p-2 bg-orange-100 rounded-lg">
                <Calendar className="h-5 w-5 text-orange-600" />
              </div>
              <div>
                <p className="text-sm text-gray-600">تاريخ الاختبار</p>
                <p className="font-semibold text-gray-900">
                  {formatters.dateTime(data.createdAt)}
                </p>
              </div>
            </div>
            
            <div className="flex items-center gap-3">
              <div className="p-2 bg-purple-100 rounded-lg">
                <Award className="h-5 w-5 text-purple-600" />
              </div>
              <div>
                <p className="text-sm text-gray-600">النتيجة الإجمالية</p>
                <p className="font-semibold text-gray-900 text-xl">
                  {formatters.number(data.totalScore)}
                </p>
              </div>
            </div>
          </div>
        </Section>

        {/* Key Performance Indicators */}
        <GridSection
          title="مؤشرات الأداء الرئيسية"
          subtitle="تحليل سريع للنتائج والإحصائيات"
          columns={3}
        >
          <KpiCard
            label="متوسط T-Score"
            value={formatters.tScore(avgTScore)}
            sublabel="عبر جميع الأبعاد"
            trend={{
              value: avgTScore >= 60 ? '+عالي' : avgTScore >= 40 ? 'متوسط' : '-منخفض',
              type: avgTScore >= 60 ? 'positive' : avgTScore >= 40 ? 'neutral' : 'negative'
            }}
          />
          
          <KpiCard
            label="نقاط القوة"
            value={topStrengths.length}
            sublabel="أبعاد فوق المتوسط (≥60)"
            trend={{
              value: `${Math.round((topStrengths.length / data.dimensions.length) * 100)}%`,
              type: topStrengths.length >= data.dimensions.length / 2 ? 'positive' : 'neutral'
            }}
          />
          
          <KpiCard
            label="مجالات النمو"
            value={growthAreas.length}
            sublabel="أبعاد تحتاج تطوير (<40)"
            trend={{
              value: `${Math.round((growthAreas.length / data.dimensions.length) * 100)}%`,
              type: growthAreas.length === 0 ? 'positive' : growthAreas.length <= 2 ? 'neutral' : 'negative'
            }}
          />
        </GridSection>

        {/* Dimensions Analysis */}
        <Section
          title="تحليل الأبعاد التفصيلي"
          subtitle={`${data.dimensions.length} بُعد • مرتبة حسب النتيجة`}
          actions={
            <div className="text-xs text-gray-500">
              إصدار النموذج: {data.scoringModelVersion || 'غير محدد'}
            </div>
          }
        >
          <DimensionsGrid
            dimensions={data.dimensions}
            variant="detailed"
            columns={2}
            showRaw
            showPercentile
          />
        </Section>

        {/* Visual Chart */}
        <Section
          title="الرسم البياني للأبعاد"
          subtitle="مقارنة بصرية لنتائج T-Score"
        >
          <div className="h-96 w-full">
            <Chart
              type="bar"
              height="100%"
              series={[{
                name: 'T-Score',
                data: data.dimensions.map(d => ({
                  x: d.dimension,
                  y: d.t
                }))
              }]}
              options={{
                chart: {
                  type: 'bar',
                  toolbar: { show: false }
                },
                plotOptions: {
                  bar: {
                    borderRadius: 4,
                    columnWidth: '60%'
                  }
                },
                colors: ['#3b82f6'],
                xaxis: {
                  labels: {
                    rotate: -45,
                    rotateAlways: true,
                    style: { fontSize: '12px' }
                  }
                },
                yaxis: {
                  min: 0,
                  max: 100,
                  labels: { style: { fontSize: '12px' } }
                },
                tooltip: {
                  y: {
                    formatter: (value: number) => formatters.tScore(value)
                  }
                },
                grid: { show: true, strokeDashArray: 3 },
                dataLabels: { enabled: false }
              }}
            />
          </div>
        </Section>

        {/* Seven Patterns Chart - Only show for V2 results */}
        {data.sdjData?.SevenPatternScores && data.sdjData.SevenPatternScores.length > 0 && (
          <Section
            title="الأنماط السباعية - SDJ v2"
            subtitle={`تحليل الأنماط السباعية (الإصدار ${data.sdjData.Version || 'v2.1'})`}
            actions={
              <div className="flex items-center gap-2 px-3 py-1 bg-blue-50 text-blue-700 rounded-full text-xs font-medium">
                <Award className="h-3.5 w-3.5" />
                <span>7 أنماط رئيسية</span>
              </div>
            }
          >
            <div className="space-y-6">
              {/* Horizontal Bar Chart */}
              <div className="h-96 w-full">
                <Chart
                  type="bar"
                  height="100%"
                  series={[{
                    name: 'T-Score',
                    data: data.sdjData.SevenPatternScores
                      .sort((a, b) => b.TScore - a.TScore)
                      .map(p => ({
                        x: p.PatternNameAr,
                        y: p.TScore
                      }))
                  }]}
                  options={{
                    chart: {
                      type: 'bar',
                      toolbar: { show: false }
                    },
                    plotOptions: {
                      bar: {
                        horizontal: true,
                        borderRadius: 6,
                        columnWidth: '70%',
                        distributed: true
                      }
                    },
                    colors: ['#10b981', '#3b82f6', '#8b5cf6', '#ec4899', '#f59e0b', '#ef4444', '#6366f1'],
                    xaxis: {
                      min: 0,
                      max: 80,
                      labels: { style: { fontSize: '12px' } }
                    },
                    yaxis: {
                      labels: { 
                        style: { fontSize: '13px', fontWeight: 600 },
                        maxWidth: 200
                      }
                    },
                    tooltip: {
                      y: {
                        formatter: (value: number) => `${value.toFixed(1)} (${
                          value < 40 ? 'ضعيف' : value < 55 ? 'متوسط' : 'ممتاز'
                        })`
                      }
                    },
                    grid: { show: true, strokeDashArray: 3 },
                    dataLabels: { 
                      enabled: true,
                      formatter: (value: number) => value.toFixed(1),
                      style: { fontSize: '11px', fontWeight: 'bold', colors: ['#fff'] }
                    },
                    legend: { show: false }
                  }}
                />
              </div>

              {/* Pattern Cards Grid */}
              <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
                {data.sdjData.SevenPatternScores.map((pattern, idx) => (
                  <div 
                    key={idx}
                    className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                  >
                    <div className="flex items-start justify-between mb-2">
                      <h4 className="font-semibold text-gray-900 text-sm">{pattern.PatternNameAr}</h4>
                      <BandBadge score={pattern.TScore} variant="compact" />
                    </div>
                    <div className="text-2xl font-bold text-blue-600 mb-2">
                      {pattern.TScore.toFixed(1)}
                    </div>
                    <div className="text-xs text-gray-500 mb-2">
                      {pattern.SubDimensions.length} بُعد فرعي
                    </div>
                    {pattern.SubDimensions.length > 0 && (
                      <div className="text-xs text-gray-600 space-y-1 mt-2 pt-2 border-t">
                        {pattern.SubDimensions.slice(0, 3).map((sub, subIdx) => (
                          <div key={subIdx} className="flex items-center gap-1">
                            <div className="w-1.5 h-1.5 rounded-full bg-gray-400"></div>
                            <span>{sub}</span>
                          </div>
                        ))}
                        {pattern.SubDimensions.length > 3 && (
                          <div className="text-xs text-gray-400">
                            +{pattern.SubDimensions.length - 3} آخرين
                          </div>
                        )}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          </Section>
        )}

        {/* AI Analysis - Only show for SDJ results */}
        {data.sdjData && (
          <Section
            title="التحليل الذكي بالذكاء الاصطناعي"
            subtitle="تحليل مخصص مدعوم بـ DeepSeek للأبعاد السبعة"
          >
            <AIAnalyzer resultId={rid} data={data} />
          </Section>
        )}
      </div>
    </div>
  )
}

interface AIAnalyzerProps {
  resultId: number
  data: ResultDetailUI
}

function AIAnalyzer({ resultId, data }: AIAnalyzerProps) {
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [analysis, setAnalysis] = useState<import('../lib/adminContract').SdjAiAnalysisResponse | null>(null)
  const [hasTriedGeneration, setHasTriedGeneration] = useState(false)
  const [aiHealthStatus, setAiHealthStatus] = useState<{enabled: boolean, message: string} | null>(null)

  // Check AI health on mount
  useEffect(() => {
    const checkHealth = async () => {
      try {
        const health = await AdminApi.aiHealth()
        setAiHealthStatus({ enabled: health.enabled, message: health.message })
      } catch (err) {
        setAiHealthStatus({ enabled: false, message: 'خطأ في فحص حالة خدمة الذكاء الاصطناعي' })
      }
    }
    checkHealth()
  }, [])

  const generateAnalysis = async (forceRegenerate = false) => {
    setLoading(true)
    setError('')
    
    try {
      const result = await AdminApi.aiAnalyze(resultId)
      setAnalysis(result)
      setHasTriedGeneration(true)
      
    } catch (err: any) {
      console.error('Failed to generate AI analysis:', err)
      setError(err.message || 'فشل في توليد التحليل - يرجى المحاولة لاحقاً')
      setHasTriedGeneration(true)
    } finally {
      setLoading(false)
    }
  }

  // Auto-generate on mount if not cached and AI is enabled
  useEffect(() => {
    if (!hasTriedGeneration && !analysis && aiHealthStatus?.enabled) {
      generateAnalysis()
    }
  }, [resultId, aiHealthStatus])

  // Show disabled state if AI is not enabled
  if (aiHealthStatus && !aiHealthStatus.enabled) {
    return (
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6 text-center">
        <Brain className="h-12 w-12 text-yellow-600 mx-auto mb-4" />
        <h3 className="font-medium text-yellow-900 mb-2">خدمة الذكاء الاصطناعي غير مفعّلة</h3>
        <p className="text-yellow-800 text-sm" dir="rtl">
          {aiHealthStatus.message}
        </p>
      </div>
    )
  }

  if (loading) {
    return (
      <div className="space-y-4">
        <div className="animate-pulse">
          <div className="flex items-center gap-2 mb-4">
            <div className="w-5 h-5 bg-gray-300 rounded"></div>
            <div className="h-4 bg-gray-300 rounded w-40"></div>
          </div>
          <div className="space-y-4">
            <div className="h-32 bg-gray-300 rounded"></div>
            <div className="grid md:grid-cols-3 gap-4">
              {[1,2,3,4,5,6,7].map(i => (
                <div key={i} className="h-40 bg-gray-300 rounded"></div>
              ))}
            </div>
          </div>
        </div>
        <div className="flex items-center justify-center p-4">
          <div className="flex items-center gap-2 text-blue-600">
            <Brain className="h-5 w-5 animate-pulse" />
            <span className="text-sm">جاري توليد التحليل المتقدم (SDJ-7)...</span>
          </div>
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="space-y-4">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-2">
            <Target className="h-5 w-5 text-red-600" />
            <h4 className="font-medium text-red-900">خطأ في توليد التحليل</h4>
          </div>
          <p className="text-red-700 text-sm mb-3" dir="rtl">{error}</p>
          
          {/* Show specific guidance for non-SDJ results */}
          {error.includes('ليست من نوع SDJ') && (
            <div className="bg-yellow-50 border border-yellow-200 rounded p-3 mb-3">
              <p className="text-xs text-yellow-800" dir="rtl">
                💡 <strong>ملاحظة:</strong> التحليل الذكي متاح حالياً فقط لنتائج اختبار SDJ (120 سؤال).
                هذه النتيجة من نوع الاختبار القديم (200 سؤال).
              </p>
            </div>
          )}
          
          {!error.includes('ليست من نوع SDJ') && (
            <Button 
              onClick={() => generateAnalysis(true)} 
              size="sm" 
              variant="outline"
              className="border-red-300 text-red-700 hover:bg-red-100"
            >
              <Brain className="h-4 w-4 ml-2" />
              إعادة المحاولة
            </Button>
          )}
        </div>
        
        {/* Fallback client-side insights */}
        {!error.includes('ليست من نوع SDJ') && <FallbackInsights data={data} />}
      </div>
    )
  }

  if (!analysis) {
    return (
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-6 text-center">
        <Brain className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h3 className="font-medium text-gray-900 mb-2">التحليل الذكي المتقدم (SDJ-7)</h3>
        <p className="text-gray-600 text-sm mb-4" dir="rtl">
          احصل على تحليل شامل للأنماط السبعة مدعوم بالذكاء الاصطناعي
        </p>
        <Button onClick={() => generateAnalysis()}>
          <Brain className="h-4 w-4 ml-2" />
          تشغيل التحليل
        </Button>
      </div>
    )
  }

  // Render SDJ-7 analysis
  return (
    <div className="space-y-6">
      {/* Header with metadata */}
      <div className="bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-center justify-between mb-3">
          <div className="flex items-center gap-2">
            <Brain className="h-5 w-5 text-blue-600" />
            <span className="font-medium text-blue-900">تحليل ذكي متقدم (SDJ-7)</span>
          </div>
          <div className="text-xs text-blue-700">
            {analysis.model} • {analysis.usage?.latencyMs.toFixed(0)}ms
          </div>
        </div>
        
        {/* Summary */}
        <div className="bg-white rounded-lg p-4">
          <p className="text-sm text-gray-800 leading-relaxed" dir="rtl">
            {analysis.summary}
          </p>
        </div>
      </div>

      {/* 7-Pattern Cards */}
      <div>
        <h4 className="font-medium text-gray-900 mb-4" dir="rtl">الأنماط السبعة (SDJ-7)</h4>
        <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
          {analysis.patterns.map((pattern, idx) => (
            <div
              key={pattern.key}
              className={`border rounded-lg p-4 ${
                pattern.band === 'قوي'
                  ? 'border-green-300 bg-green-50'
                  : pattern.band === 'متوسط'
                  ? 'border-yellow-300 bg-yellow-50'
                  : 'border-red-300 bg-red-50'
              }`}
            >
              {/* Pattern header */}
              <div className="flex items-start justify-between mb-3">
                <h5 className="font-medium text-gray-900 text-sm" dir="rtl">
                  {idx + 1}. {pattern.label}
                </h5>
                <span
                  className={`px-2 py-1 rounded text-xs font-medium ${
                    pattern.band === 'قوي'
                      ? 'bg-green-100 text-green-800'
                      : pattern.band === 'متوسط'
                      ? 'bg-yellow-100 text-yellow-800'
                      : 'bg-red-100 text-red-800'
                  }`}
                >
                  {pattern.band} ({pattern.tScore.toFixed(1)})
                </span>
              </div>

              {/* Insights */}
              {pattern.insights.length > 0 && (
                <div className="mb-3">
                  <p className="text-xs font-medium text-gray-700 mb-1" dir="rtl">✓ رؤى:</p>
                  <ul className="space-y-1">
                    {pattern.insights.slice(0, 2).map((insight, i) => (
                      <li key={i} className="text-xs text-gray-700" dir="rtl">
                        • {insight}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Risks (if any) */}
              {pattern.risks.length > 0 && (
                <div className="mb-3">
                  <p className="text-xs font-medium text-orange-700 mb-1" dir="rtl">⚠ تحديات:</p>
                  <ul className="space-y-1">
                    {pattern.risks.slice(0, 1).map((risk, i) => (
                      <li key={i} className="text-xs text-orange-700" dir="rtl">
                        • {risk}
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              {/* Recommendations */}
              {pattern.recommendations.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-blue-700 mb-1" dir="rtl">💡 توصيات:</p>
                  <ul className="space-y-1">
                    {pattern.recommendations.slice(0, 2).map((rec, i) => (
                      <li key={i} className="text-xs text-blue-700" dir="rtl">
                        • {rec}
                      </li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Chart Visualization (Text fallback) */}
      <div className="border rounded-lg p-4 bg-gray-50">
        <h4 className="font-medium text-gray-900 mb-4" dir="rtl">التمثيل البصري للأنماط</h4>
        
        {/* Simple bar representation */}
        <div className="space-y-2">
          {analysis.patterns.map((pattern) => (
            <div key={pattern.key} className="flex items-center gap-2">
              <span className="text-xs text-gray-700 w-32 text-right" dir="rtl">
                {pattern.label.split(' ').slice(-2).join(' ')}:
              </span>
              <div className="flex-1 bg-gray-200 rounded-full h-4 overflow-hidden">
                <div
                  className={`h-full rounded-full ${
                    pattern.tScore >= 55
                      ? 'bg-green-500'
                      : pattern.tScore >= 40
                      ? 'bg-yellow-500'
                      : 'bg-red-500'
                  }`}
                  style={{ width: `${Math.min(pattern.tScore, 100)}%` }}
                ></div>
              </div>
              <span className="text-xs font-medium text-gray-900 w-12">
                {pattern.tScore.toFixed(1)}
              </span>
            </div>
          ))}
        </div>
        
        <p className="text-xs text-gray-500 mt-3" dir="rtl">
          * T-Score: ضعيف (&lt;40)، متوسط (40-54)، قوي (≥55)
        </p>
      </div>

      {/* Regenerate button */}
      <div className="flex justify-end">
        <Button 
          onClick={() => generateAnalysis(true)} 
          size="sm" 
          variant="outline"
        >
          <Brain className="h-4 w-4 ml-2" />
          إعادة توليد التحليل
        </Button>
      </div>
    </div>
  )
}
// Fallback component for basic insights when AI fails
function FallbackInsights({ data }: { data: ResultDetailUI }) {
  const topStrengths = data.dimensions.filter(d => d.t >= 60).slice(0, 3)
  const growthAreas = data.dimensions.filter(d => d.t < 40).slice(0, 3)

  return (
    <div className="space-y-4">
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
        <div className="flex items-center gap-2 mb-2">
          <TrendingUp className="h-5 w-5 text-yellow-600" />
          <h4 className="font-medium text-yellow-900">تحليل أساسي (بديل)</h4>
        </div>
        <p className="text-yellow-700 text-sm mb-3">
          التحليل التلقائي بناءً على النتائج المحلية
        </p>
        
        {topStrengths.length > 0 && (
          <div className="mb-4">
            <h5 className="font-medium text-yellow-800 mb-2">نقاط القوة:</h5>
            <div className="space-y-1">
              {topStrengths.map((strength, i) => (
                <div key={i} className="flex items-center gap-2 text-sm text-yellow-700">
                  <BandBadge score={strength.t} variant="compact" />
                  <span>{strength.dimension}</span>
                </div>
              ))}
            </div>
          </div>
        )}
        
        {growthAreas.length > 0 && (
          <div>
            <h5 className="font-medium text-yellow-800 mb-2">مجالات التطوير:</h5>
            <div className="space-y-1">
              {growthAreas.map((area, i) => (
                <div key={i} className="flex items-center gap-2 text-sm text-yellow-700">
                  <BandBadge score={area.t} variant="compact" />
                  <span>{area.dimension}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
