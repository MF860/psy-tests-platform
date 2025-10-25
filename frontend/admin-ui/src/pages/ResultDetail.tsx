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

        {/* AI Analysis */}
        <Section
          title="التحليل الذكي بالذكاء الاصطناعي"
          subtitle="تحليل مخصص مدعوم بـ DeepSeek عبر OpenRouter"
        >
          <AIAnalyzer resultId={rid} data={data} />
        </Section>
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
  const [analysis, setAnalysis] = useState<AiAnalysisResponse | null>(null)
  const [hasTriedGeneration, setHasTriedGeneration] = useState(false)

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

  // Auto-generate on mount if not cached
  useEffect(() => {
    if (!hasTriedGeneration && !analysis) {
      generateAnalysis()
    }
  }, [resultId])

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
              <div className="h-24 bg-gray-300 rounded"></div>
              <div className="h-24 bg-gray-300 rounded"></div>
              <div className="h-24 bg-gray-300 rounded"></div>
            </div>
          </div>
        </div>
        <div className="flex items-center justify-center p-4">
          <div className="flex items-center gap-2 text-blue-600">
            <Brain className="h-5 w-5 animate-pulse" />
            <span className="text-sm">جاري توليد التحليل بالذكاء الاصطناعي...</span>
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
          <p className="text-red-700 text-sm mb-3">{error}</p>
          <Button 
            onClick={() => generateAnalysis(true)} 
            size="sm" 
            variant="outline"
            className="border-red-300 text-red-700 hover:bg-red-100"
          >
            <Brain className="h-4 w-4 ml-2" />
            إعادة المحاولة
          </Button>
        </div>
        
        {/* Fallback client-side insights */}
        <FallbackInsights data={data} />
      </div>
    )
  }

  if (!analysis) {
    return (
      <div className="bg-gray-50 border border-gray-200 rounded-lg p-6 text-center">
        <Brain className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <h3 className="font-medium text-gray-900 mb-2">التحليل الذكي</h3>
        <p className="text-gray-600 text-sm mb-4">
          احصل على تحليل مخصص مدعوم بالذكاء الاصطناعي
        </p>
        <Button onClick={() => generateAnalysis()}>
          <Brain className="h-4 w-4 ml-2" />
          تشغيل التحليل
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header with metadata */}
      <div className="bg-gradient-to-r from-blue-50 to-indigo-50 border border-blue-200 rounded-lg p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Brain className="h-5 w-5 text-blue-600" />
            <div>
              <h3 className="font-medium text-blue-900">
                تحليل مدعوم بـ {analysis.model}
                {analysis.model === 'demo-mock' && (
                  <span className="text-xs bg-yellow-100 text-yellow-800 px-2 py-0.5 rounded ml-2">
                    تجريبي
                  </span>
                )}
              </h3>
              <p className="text-xs text-blue-700">
                تم توليد التحليل في {formatters.dateTime(analysis.generatedAt || new Date().toISOString())}
                {analysis.usage && ` • ${analysis.usage.totalTokens} رمز`}
              </p>
            </div>
          </div>
          <Button 
            onClick={() => generateAnalysis(true)} 
            size="sm" 
            variant="outline"
            disabled={loading}
          >
            تجديد التحليل
          </Button>
        </div>
      </div>

      {/* Three-column analysis */}
      <div className="grid md:grid-cols-3 gap-6">
        {/* Strengths */}
        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-3">
            <TrendingUp className="h-5 w-5 text-green-600" />
            <h3 className="font-semibold text-green-900">نقاط القوة</h3>
          </div>
          <ul className="space-y-2">
            {analysis.analysis.strengths.map((strength, index) => (
              <li key={index} className="flex items-start gap-2 text-sm text-green-800">
                <span className="w-1 h-1 bg-green-600 rounded-full mt-2 flex-shrink-0"></span>
                <span>{strength}</span>
              </li>
            ))}
          </ul>
        </div>

        {/* Weaknesses */}
        <div className="bg-orange-50 border border-orange-200 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-3">
            <Target className="h-5 w-5 text-orange-600" />
            <h3 className="font-semibold text-orange-900">نقاط الضعف</h3>
          </div>
          <ul className="space-y-2">
            {analysis.analysis.weaknesses.map((weakness, index) => (
              <li key={index} className="flex items-start gap-2 text-sm text-orange-800">
                <span className="w-1 h-1 bg-orange-600 rounded-full mt-2 flex-shrink-0"></span>
                <span>{weakness}</span>
              </li>
            ))}
          </ul>
        </div>

        {/* Recommendations */}
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-3">
            <Award className="h-5 w-5 text-blue-600" />
            <h3 className="font-semibold text-blue-900">التوصيات</h3>
          </div>
          <ul className="space-y-2">
            {analysis.analysis.recommendations.map((recommendation, index) => (
              <li key={index} className="flex items-start gap-2 text-sm text-blue-800">
                <span className="w-1 h-1 bg-blue-600 rounded-full mt-2 flex-shrink-0"></span>
                <span>{recommendation}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>

      {/* Rationale */}
      {analysis.analysis.rationale && (
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-2">
            <Brain className="h-4 w-4 text-gray-600" />
            <h4 className="font-medium text-gray-900">التبرير</h4>
          </div>
          <p className="text-sm text-gray-700">{analysis.analysis.rationale}</p>
        </div>
      )}

      {/* Performance metrics */}
      {analysis.usage && (
        <div className="bg-gray-50 border border-gray-200 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-2">
            <TrendingUp className="h-4 w-4 text-gray-600" />
            <h4 className="font-medium text-gray-900">مقاييس الأداء</h4>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm text-gray-600">
            <div>
              <span className="block font-medium">الإدخال</span>
              <span>{analysis.usage.promptTokens} رمز</span>
            </div>
            <div>
              <span className="block font-medium">الإخراج</span>
              <span>{analysis.usage.completionTokens} رمز</span>
            </div>
            <div>
              <span className="block font-medium">الإجمالي</span>
              <span>{analysis.usage.totalTokens} رمز</span>
            </div>
            <div>
              <span className="block font-medium">الزمن</span>
              <span>{Math.round(analysis.usage.latencyMs)} مللي ثانية</span>
            </div>
          </div>
        </div>
      )}
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