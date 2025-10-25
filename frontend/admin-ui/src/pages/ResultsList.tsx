import { useState, useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { Eye } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { AdminApi } from '@/lib/apiAdmin'
import { mapResultListResponse, formatters, type ResultListItemUI } from '@/lib/adminContract'
import { useDebounce } from '@/hooks/useDebounce'
import { Toolbar } from '@/components/admin/Table/Toolbar'
import { ColumnHeader, LoadingRows } from '@/components/admin/Table/ColumnHeader'
import { EmptyState, ErrorState } from '@/components/admin/Table/States'
import { ScoreBadge, CopyableNationalId, SessionStatusBadge } from '@/components/admin/ResultBadges'
import { CsvExport } from '@/components/admin/CsvExport'

export default function ResultsList() {
  const navigate = useNavigate()

  // State management
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(20)
  const [q, setQ] = useState('')
  const [debouncedQ] = useDebounce(q, 350)
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false)
  
  const [filters, setFilters] = useState({
    startDate: '',
    endDate: '',
    minScore: '',
    maxScore: ''
  })
  
  const [sort, setSort] = useState<{ by?: string; dir?: 'asc' | 'desc' }>({})

  // Data fetching with React Query
  const { data, isLoading, isError, error, refetch } = useQuery<{
    data: ResultListItemUI[]
    total: number
  }>({
    queryKey: ['adminResults', page, pageSize, debouncedQ, filters, sort],
    queryFn: async () => {
      const raw = await AdminApi.results(page, pageSize, debouncedQ, {
        ...filters,
        sortBy: sort.by,
        sortDir: sort.dir
      })
      return mapResultListResponse(raw)
    },
    staleTime: 0,
    refetchOnWindowFocus: true,
  })

  // Formatting helpers
  const fmtDate = (dateString: string) => {
    if (!dateString) return '-'
    try {
      return new Intl.DateTimeFormat('ar-SA', {
        dateStyle: 'short',
        timeStyle: 'short'
      }).format(new Date(dateString))
    } catch {
      return dateString
    }
  }

  // Pagination
  const totalPages = Math.ceil((data?.total || 0) / pageSize) || 1

  // Handlers
  const handleSortChange = (sortKey: string) => {
    setSort(prev => ({
      by: sortKey,
      dir: prev.by === sortKey && prev.dir === 'asc' ? 'desc' : 'asc'
    }))
  }

  const handlePageSizeChange = (newPageSize: number) => {
    const newPage = Math.ceil((page - 1) * pageSize / newPageSize) + 1
    setPageSize(newPageSize)
    setPage(Math.min(newPage, Math.ceil((data?.total || 0) / newPageSize) || 1))
  }

  const handleReset = () => {
    setQ('')
    setFilters({
      startDate: '',
      endDate: '',
      minScore: '',
      maxScore: ''
    })
    setSort({})
    setPage(1)
    setShowAdvancedFilters(false)
  }

  const handleExport = () => {
    if (!data?.data?.length) return
    
    const csvData = data.data.map((item: ResultListItemUI) => ({
      resultId: item.resultId,
      nationalId: item.nationalId,
      fullName: item.fullName || 'غير متوفر',
      totalScore: item.totalScore,
      createdAt: item.createdAt,
      sessionId: item.sessionId || ''
    }))

    // Create CSV content manually for immediate download
    const headers = [
      'معرف النتيجة',
      'الرقم الوطني', 
      'الاسم الكامل',
      'الدرجة الإجمالية',
      'تاريخ الإنشاء',
      'معرف الجلسة'
    ]

    const csvRows = [
      headers.join(','),
      ...csvData.map(row => [
        row.resultId,
        row.nationalId,
        row.fullName,
        row.totalScore,
        row.createdAt,
        row.sessionId
      ].join(','))
    ]

    const csvContent = csvRows.join('\n')
    const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' })
    const link = document.createElement('a')
    
    if (link.download !== undefined) {
      const url = URL.createObjectURL(blob)
      link.setAttribute('href', url)
      link.setAttribute('download', `results_page${page}_${Date.now()}.csv`)
      link.style.visibility = 'hidden'
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(url)
    }
  }

  const handleRowClick = (resultId: string | number) => {
    navigate(`/results/${resultId}`)
  }

  return (
    <div className="mx-auto max-w-[1400px] px-4 lg:px-6 py-6 space-y-6">
      {/* Toolbar */}
      <Toolbar
        searchQuery={q}
        onSearchChange={setQ}
        dateRange={filters}
        onDateRangeChange={(range) => setFilters(prev => ({ ...prev, ...range }))}
        scoreRange={filters}
        onScoreRangeChange={(range) => setFilters(prev => ({ ...prev, ...range }))}
        pageSize={pageSize}
        onPageSizeChange={handlePageSizeChange}
        onReset={handleReset}
        onRefresh={() => refetch()}
        onExport={handleExport}
        isLoading={isLoading}
        totalResults={data?.total}
        showAdvancedFilters={showAdvancedFilters}
        onToggleAdvancedFilters={() => setShowAdvancedFilters(!showAdvancedFilters)}
      />

      {/* Results Table */}
      <Card>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <Table>
              <caption className="sr-only">قائمة النتائج</caption>
              <TableHeader className="sticky top-0 bg-white">
                <TableRow>
                  <ColumnHeader
                    title="#"
                    sortKey="resultId"
                    currentSort={sort}
                    onSortChange={handleSortChange}
                    sortable
                  />
                  <ColumnHeader
                    title="الرقم الوطني"
                    sortKey="nationalId"
                    currentSort={sort}
                    onSortChange={handleSortChange}
                    sortable
                  />
                  <ColumnHeader
                    title="الاسم"
                    sortKey="fullName"
                    currentSort={sort}
                    onSortChange={handleSortChange}
                    sortable
                  />
                  <ColumnHeader
                    title="الدرجة (T)"
                    sortKey="totalScore"
                    currentSort={sort}
                    onSortChange={handleSortChange}
                    sortable
                  />
                  <ColumnHeader
                    title="التاريخ"
                    sortKey="createdAt"
                    currentSort={sort}
                    onSortChange={handleSortChange}
                    sortable
                  />
                  <ColumnHeader title="" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <LoadingRows columns={6} rows={pageSize} />
                ) : isError ? (
                  <ErrorState
                    message="فشل تحميل النتائج"
                    details={error instanceof Error ? error.message : 'خطأ غير محدد'}
                    onRetry={() => refetch()}
                  />
                ) : !data?.data?.length ? (
                  <EmptyState
                    type={debouncedQ || Object.values(filters).some(v => v) ? 'no-results' : 'no-data'}
                    action={{
                      label: 'إعادة تحميل',
                      onClick: () => refetch()
                    }}
                  />
                ) : (
                  data.data.map((result: ResultListItemUI, index: number) => (
                    <TableRow
                      key={result.resultId}
                      className={`hover:bg-gray-50 cursor-pointer text-sm ${
                        index % 2 === 0 ? 'bg-white' : 'bg-gray-50/50'
                      }`}
                      onClick={() => handleRowClick(result.resultId)}
                    >
                      <TableCell className="px-6 py-4 font-mono text-gray-600">
                        {result.resultId}
                      </TableCell>
                      <TableCell className="px-6 py-4">
                        <CopyableNationalId nationalId={result.nationalId} />
                      </TableCell>
                      <TableCell className="px-6 py-4 text-gray-900">
                        <div className="flex items-center gap-2">
                          <span>{result.fullName || 'غير متوفر'}</span>
                          <SessionStatusBadge sessionId={result.sessionId} />
                        </div>
                      </TableCell>
                      <TableCell className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <ScoreBadge score={result.totalScore} />
                          {result.hasSdjData && result.sdjProfile && (
                            <span 
                              className="inline-flex items-center px-2 py-1 rounded-md text-xs font-medium bg-blue-50 text-blue-700 border border-blue-200"
                              title="إطار التنمية المستدامة"
                            >
                              SDJ: {result.sdjProfile}
                            </span>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className="px-6 py-4 text-gray-500 font-mono text-xs">
                        {fmtDate(result.createdAt)}
                      </TableCell>
                      <TableCell className="px-6 py-4">
                        <Button
                          size="sm"
                          variant="ghost"
                          className="flex items-center gap-1 text-blue-600 hover:text-blue-700"
                          onClick={(e) => {
                            e.stopPropagation()
                            handleRowClick(result.resultId)
                          }}
                        >
                          <Eye className="h-4 w-4" />
                          عرض
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </CardContent>
      </Card>

      {/* Pagination */}
      {data && data.total > 0 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-gray-600">
            إجمالي: {formatters.number(data.total)} نتيجة
            {data.data.length > 0 && (
              <span className="text-gray-400 mr-2">
                (عرض {((page - 1) * pageSize) + 1}-{Math.min(page * pageSize, data.total)})
              </span>
            )}
          </div>
          
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={page <= 1}
              onClick={() => setPage(Math.max(1, page - 1))}
            >
              السابق
            </Button>
            
            <div className="flex items-center gap-1 text-sm">
              <span>صفحة</span>
              <span className="font-medium">{formatters.number(page)}</span>
              <span>من</span>
              <span className="font-medium">{formatters.number(totalPages)}</span>
            </div>
            
            <Button
              variant="outline"
              size="sm"
              disabled={page >= totalPages}
              onClick={() => setPage(Math.min(totalPages, page + 1))}
            >
              التالي
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
