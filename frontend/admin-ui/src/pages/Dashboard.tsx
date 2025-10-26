import { useMemo, useState, useEffect } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { AdminApi } from '@/lib/apiAdmin'
import { formatters, type AnalyticsSummary } from '@/lib/adminContract'
import { OverviewCards, OverviewCardsSkeleton } from '@/components/charts/OverviewCards'
import { DimensionRadarChart, ScoreDistributionChart, CategoryDonutChart } from '@/components/charts/ApexCharts'
import { ErrorState, EmptyState } from '@/components/admin/States'
import { CsvExport } from '@/components/admin/CsvExport'
import { ProgressiveLoader, CardSkeleton, TableSkeleton } from '@/components/ui/progressive-loader'
import { AnimatedEntrance, Interactive, StaggeredList } from '@/components/ui/micro-interactions'
import { MobileTable, ResponsiveGrid } from '@/components/ui/mobile-responsive'
import { RefreshCw, TrendingUp } from 'lucide-react'
import { toast } from 'sonner'

export default function Dashboard() {
  const navigate = useNavigate()
  const [loadingStage, setLoadingStage] = useState<'initial' | 'fetching' | 'processing' | 'complete'>('initial')
  
  const { data, isLoading, error, refetch } = useQuery<AnalyticsSummary>({
    queryKey: ['adminAnalytics'],
    queryFn: async () => {
      setLoadingStage('fetching')
      const result = await AdminApi.getAnalytics()
      setLoadingStage('processing')
      // Simulate processing delay for smooth UX
      await new Promise(resolve => setTimeout(resolve, 300))
      setLoadingStage('complete')
      return result
    },
    staleTime: 0,
    refetchOnWindowFocus: true,
  })

  // Enhanced refetch with user feedback
  const handleRefresh = async () => {
    setLoadingStage('fetching')
    try {
      await refetch()
      toast.success('تم تحديث البيانات بنجاح')
    } catch (error) {
      toast.error('فشل في تحديث البيانات')
    }
  }

  useEffect(() => {
    if (!isLoading) {
      setLoadingStage('complete')
    }
  }, [isLoading])

  // Formatted helpers with memoization
  const formattedMetrics = useMemo(() => {
    if (!data) return null

    // Calculate average response time from type accuracy data
    const avgResponseMs = data.byTypeAccuracy.length > 0
      ? data.byTypeAccuracy.reduce((sum, item) => sum + (item.avgTimeMs || 0), 0) / data.byTypeAccuracy.length
      : 0

    return {
      totalUsers: formatters.number(data.totalUsers),
      totalSessions: formatters.number(data.totalSessions), 
      totalResults: formatters.number(data.totalResults),
      resultsToday: formatters.number(data.resultsToday),
      avgTotalScore: formatters.tScore(data.avgTotalScore),
      avgResponseTime: formatters.seconds(avgResponseMs)
    }
  }, [data])

  if (error) {
    return (
      <ErrorState 
        message="حدث خطأ أثناء تحميل بيانات لوحة التحكم"
        details={error instanceof Error ? error.message : 'خطأ غير محدد'}
        onRetry={() => refetch()}
        className="h-96"
      />
    )
  }

  return (
    <ProgressiveLoader stage={loadingStage} className="min-h-screen">
      <div className="space-y-6">
        {/* Header */}
        <AnimatedEntrance animation="slideDown" className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">لوحة التحكم</h1>
            <p className="text-gray-600">نظرة عامة على الاستخدام والأداء</p>
          </div>
          <Interactive>
            <Button 
              onClick={handleRefresh}
              variant="outline"
              disabled={isLoading}
              className="gap-2"
            >
              <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
              تحديث البيانات
            </Button>
          </Interactive>
        </AnimatedEntrance>

        {/* KPI Cards Grid */}
        <AnimatedEntrance animation="slideUp" delay={0.1}>
          {isLoading ? (
            <OverviewCardsSkeleton />
          ) : data ? (
            <OverviewCards
              totalTests={data.totalResults || 0}
              activeUsers={data.totalUsers || 0}
              completionRate={data.totalSessions > 0 ? Math.round((data.totalResults || 0) / data.totalSessions * 100) : 0}
              avgTime={formattedMetrics?.avgResponseTime || '0.0 ث'}
            />
          ) : null}
        </AnimatedEntrance>

        {/* Charts Row */}
        <AnimatedEntrance animation="slideUp" delay={0.2}>
          <ResponsiveGrid cols={{ xs: 1, lg: 2 }} gap="md">
            <Interactive hover tap>
              <Card className="p-6 transition-all duration-200 hover:shadow-lg">
                <CardHeader className="pb-4">
                  <div className="flex items-center gap-2">
                    <TrendingUp className="h-5 w-5 text-primary" />
                    <CardTitle>الأداء حسب البعد</CardTitle>
                  </div>
                  <CardDescription>نتائج T-Score لكل بُعد</CardDescription>
                </CardHeader>
                <CardContent>
                  {isLoading ? (
                    <CardSkeleton showHeader={false} />
                  ) : data?.byDimension && data.byDimension.length > 0 ? (
                    <DimensionRadarChart 
                      data={data.byDimension.map(d => ({ 
                        dimension: d.dimension, 
                        score: d.tScore 
                      }))} 
                    />
                  ) : (
                    <EmptyState message="لا توجد بيانات للعرض" />
                  )}
                </CardContent>
              </Card>
            </Interactive>

            <Interactive hover tap>
              <Card className="p-6 transition-all duration-200 hover:shadow-lg">
                <CardHeader className="pb-4">
                  <div className="flex items-center gap-2">
                    <TrendingUp className="h-5 w-5 text-primary" />
                    <CardTitle>توزيع الدرجات</CardTitle>
                  </div>
                  <CardDescription>توزيع نتائج T-Score</CardDescription>
                </CardHeader>
                <CardContent>
                  {isLoading ? (
                    <CardSkeleton showHeader={false} />
                  ) : data?.scoreDistribution && data.scoreDistribution.length > 0 ? (
                    <ScoreDistributionChart 
                      data={data.scoreDistribution}
                    />
                  ) : (
                    <EmptyState message="لا توجد بيانات للعرض" />
                  )}
                </CardContent>
              </Card>
            </Interactive>
          </ResponsiveGrid>
        </AnimatedEntrance>

        {/* Recent Sessions Table */}
        <AnimatedEntrance animation="slideUp" delay={0.3}>
          <Interactive hover>
            <Card className="transition-all duration-200 hover:shadow-lg">
              <CardHeader>
                <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-2">
                  <div>
                    <CardTitle>أحدث الجلسات</CardTitle>
                    <CardDescription>آخر 10 جلسات مكتملة</CardDescription>
                  </div>
                  {data?.recentSessions?.length ? (
                    <Interactive>
                      <CsvExport
                        data={data.recentSessions}
                        filename="recent-sessions"
                        headers={{
                          sessionId: 'معرف الجلسة',
                          nationalId: 'الهوية الوطنية', 
                          score: 'الدرجة',
                          createdAt: 'تاريخ الإنشاء'
                        }}
                      />
                    </Interactive>
                  ) : null}
                </div>
              </CardHeader>
              <CardContent>
                {isLoading ? (
                  <TableSkeleton rows={5} columns={4} />
                ) : !data?.recentSessions?.length ? (
                  <EmptyState 
                    icon="table"
                    message="لا توجد جلسات حديثة"
                    description="ستظهر آخر الجلسات المكتملة هنا"
                    action={{
                      label: "تحديث",
                      onClick: handleRefresh
                    }}
                  />
                ) : (
                  <MobileTable
                    headers={['المعرف', 'الهوية الوطنية', 'الدرجة (T)', 'التاريخ']}
                    data={data.recentSessions.map(session => ({
                      sessionId: session.sessionId,
                      nationalId: session.nationalId,
                      score: formatters.tScore(session.score),
                      createdAt: formatters.dateTime(session.createdAt)
                    }))}
                    onRowClick={(item) => navigate(`/results/${item.sessionId}`)}
                  />
                )}
              </CardContent>
            </Card>
          </Interactive>
        </AnimatedEntrance>
      </div>
    </ProgressiveLoader>
  )
}

