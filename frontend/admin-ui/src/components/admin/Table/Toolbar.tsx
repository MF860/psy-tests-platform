import { ReactNode } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Search, RotateCcw, Download, Filter, ChevronDown } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ToolbarProps {
  /** Search query value */
  searchQuery: string
  /** Search change handler */
  onSearchChange: (value: string) => void
  /** Date range filters */
  dateRange: {
    startDate: string
    endDate: string
  }
  /** Date range change handler */
  onDateRangeChange: (range: { startDate: string; endDate: string }) => void
  /** Score range filters */
  scoreRange: {
    minScore: string
    maxScore: string
  }
  /** Score range change handler */
  onScoreRangeChange: (range: { minScore: string; maxScore: string }) => void
  /** Page size */
  pageSize: number
  /** Page size change handler */
  onPageSizeChange: (size: number) => void
  /** Reset filters handler */
  onReset: () => void
  /** Refresh data handler */
  onRefresh: () => void
  /** Export CSV handler */
  onExport: () => void
  /** Loading state */
  isLoading?: boolean
  /** Results count */
  totalResults?: number
  /** Show advanced filters */
  showAdvancedFilters?: boolean
  /** Toggle advanced filters */
  onToggleAdvancedFilters?: () => void
}

export function Toolbar({
  searchQuery,
  onSearchChange,
  dateRange,
  onDateRangeChange,
  scoreRange,
  onScoreRangeChange,
  pageSize,
  onPageSizeChange,
  onReset,
  onRefresh,
  onExport,
  isLoading = false,
  totalResults,
  showAdvancedFilters = false,
  onToggleAdvancedFilters
}: ToolbarProps) {
  return (
    <Card>
      <CardHeader>
        <div className="flex flex-col sm:flex-row sm:justify-between sm:items-center gap-4">
          <div>
            <CardTitle>النتائج</CardTitle>
            <p className="text-sm text-gray-600">استعرض وفلتر نتائج الاختبارات</p>
          </div>
          {totalResults !== undefined && (
            <div className="text-sm text-gray-500">
              إجمالي: {new Intl.NumberFormat('ar-EG').format(totalResults)} نتيجة
            </div>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Primary row - Search and main actions */}
        <div className="flex flex-col sm:flex-row gap-3">
          {/* Search */}
          <div className="relative flex-1 max-w-md">
            <Search className="absolute right-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input
              placeholder="ابحث بالرقم الوطني أو الاسم..."
              value={searchQuery}
              onChange={(e) => onSearchChange(e.target.value)}
              className="pr-10"
            />
          </div>

          {/* Actions */}
          <div className="flex gap-2 flex-wrap">
            <Button
              variant="outline"
              size="sm"
              onClick={onToggleAdvancedFilters}
              className="flex items-center gap-2"
            >
              <Filter className="h-4 w-4" />
              فلاتر متقدمة
              <ChevronDown className={cn(
                "h-4 w-4 transition-transform",
                showAdvancedFilters && "rotate-180"
              )} />
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={onRefresh}
              disabled={isLoading}
              className="flex items-center gap-2"
            >
              <RotateCcw className={cn("h-4 w-4", isLoading && "animate-spin")} />
              تحديث
            </Button>

            <Button
              variant="outline"
              size="sm"
              onClick={onExport}
              className="flex items-center gap-2"
            >
              <Download className="h-4 w-4" />
              تصدير CSV
            </Button>

            <Button
              variant="ghost"
              size="sm"
              onClick={onReset}
              className="text-gray-500"
            >
              إعادة تعيين
            </Button>
          </div>
        </div>

        {/* Advanced filters row */}
        {showAdvancedFilters && (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 p-4 bg-gray-50 rounded-lg">
            {/* Date Range */}
            <div className="space-y-1">
              <label className="text-xs font-medium text-gray-700">من تاريخ</label>
              <Input
                type="date"
                value={dateRange.startDate}
                onChange={(e) => onDateRangeChange({ 
                  ...dateRange, 
                  startDate: e.target.value 
                })}
                className="text-sm"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-gray-700">إلى تاريخ</label>
              <Input
                type="date"
                value={dateRange.endDate}
                onChange={(e) => onDateRangeChange({ 
                  ...dateRange, 
                  endDate: e.target.value 
                })}
                className="text-sm"
              />
            </div>

            {/* Score Range */}
            <div className="space-y-1">
              <label className="text-xs font-medium text-gray-700">أقل درجة</label>
              <Input
                type="number"
                placeholder="0"
                value={scoreRange.minScore}
                onChange={(e) => onScoreRangeChange({ 
                  ...scoreRange, 
                  minScore: e.target.value 
                })}
                className="text-sm"
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-gray-700">أعلى درجة</label>
              <Input
                type="number"
                placeholder="100"
                value={scoreRange.maxScore}
                onChange={(e) => onScoreRangeChange({ 
                  ...scoreRange, 
                  maxScore: e.target.value 
                })}
                className="text-sm"
              />
            </div>
          </div>
        )}

        {/* Page size selector */}
        <div className="flex justify-between items-center text-sm">
          <div className="flex items-center gap-2">
            <span className="text-gray-600">عدد النتائج في الصفحة:</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="border border-gray-300 rounded px-2 py-1 bg-white text-sm"
            >
              <option value={10}>10</option>
              <option value={20}>20</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}