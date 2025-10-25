import { Search, FileX, AlertCircle, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface EmptyStateProps {
  /** Type of empty state */
  type?: 'no-results' | 'no-data' | 'search'
  /** Custom message */
  message?: string
  /** Custom description */
  description?: string
  /** Action button */
  action?: {
    label: string
    onClick: () => void
  }
  /** Additional CSS classes */
  className?: string
}

export function EmptyState({
  type = 'no-results',
  message,
  description,
  action,
  className = ''
}: EmptyStateProps) {
  const configs = {
    'no-results': {
      icon: Search,
      defaultMessage: 'لا توجد نتائج مطابقة للبحث الحالي',
      defaultDescription: 'جرب تعديل معايير البحث أو الفلاتر المستخدمة'
    },
    'no-data': {
      icon: FileX,
      defaultMessage: 'لا توجد نتائج متاحة',
      defaultDescription: 'لم يتم العثور على أي نتائج في النظام'
    },
    'search': {
      icon: Search,
      defaultMessage: 'ابدأ البحث',
      defaultDescription: 'استخدم مربع البحث أعلاه للعثور على النتائج'
    }
  }

  const config = configs[type]
  const IconComponent = config.icon

  return (
    <tr>
      <td colSpan={6} className={`px-6 py-12 ${className}`}>
        <div className="text-center">
          <IconComponent className="mx-auto h-12 w-12 text-gray-400 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            {message || config.defaultMessage}
          </h3>
          <p className="text-sm text-gray-500 mb-4 max-w-md mx-auto">
            {description || config.defaultDescription}
          </p>
          {action && (
            <Button 
              onClick={action.onClick}
              variant="outline"
            >
              {action.label}
            </Button>
          )}
        </div>
      </td>
    </tr>
  )
}

interface ErrorStateProps {
  /** Error message */
  message?: string
  /** Error details */
  details?: string
  /** Retry handler */
  onRetry?: () => void
  /** Show technical details toggle */
  showDetails?: boolean
  /** Additional CSS classes */
  className?: string
}

export function ErrorState({
  message = 'حدث خطأ أثناء تحميل البيانات',
  details,
  onRetry,
  showDetails = false,
  className = ''
}: ErrorStateProps) {
  return (
    <tr>
      <td colSpan={6} className={`px-6 py-12 ${className}`}>
        <div className="text-center">
          <AlertCircle className="mx-auto h-12 w-12 text-red-500 mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            {message}
          </h3>
          {details && showDetails && (
            <div className="bg-gray-100 rounded p-3 mb-4 text-xs text-right max-w-md mx-auto">
              <summary className="font-medium cursor-pointer mb-2">تفاصيل تقنية:</summary>
              <code className="text-red-600 break-all">{details}</code>
            </div>
          )}
          {onRetry && (
            <Button 
              onClick={onRetry}
              className="flex items-center gap-2 mx-auto"
              variant="outline"
            >
              <RefreshCw className="h-4 w-4" />
              إعادة المحاولة
            </Button>
          )}
        </div>
      </td>
    </tr>
  )
}