import { RefreshCw, AlertCircle, BarChart3 } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface ErrorStateProps {
  /** Error message to display */
  message?: string
  /** Detailed error information */
  details?: string
  /** Retry callback function */
  onRetry?: () => void
  /** Additional CSS classes */
  className?: string
}

export function ErrorState({ 
  message = "حدث خطأ أثناء تحميل البيانات",
  details,
  onRetry,
  className = ''
}: ErrorStateProps) {
  return (
    <div className={`flex flex-col items-center justify-center py-12 ${className}`}>
      <div className="text-center">
        <AlertCircle className="mx-auto h-12 w-12 text-red-500 mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {message}
        </h3>
        {details && (
          <p className="text-sm text-gray-500 mb-4 max-w-md">
            {details}
          </p>
        )}
        {onRetry && (
          <Button 
            onClick={onRetry}
            className="flex items-center gap-2"
            variant="outline"
          >
            <RefreshCw className="h-4 w-4" />
            المحاولة مرة أخرى
          </Button>
        )}
      </div>
    </div>
  )
}

interface EmptyStateProps {
  /** Icon to display */
  icon?: 'chart' | 'table' | 'file'
  /** Empty state message */
  message?: string
  /** Optional description */
  description?: string
  /** Optional action button */
  action?: {
    label: string
    onClick: () => void
  }
  /** Additional CSS classes */
  className?: string
}

export function EmptyState({ 
  icon = 'chart',
  message = "لا توجد بيانات للعرض حاليًا",
  description,
  action,
  className = ''
}: EmptyStateProps) {
  const IconComponent = {
    chart: BarChart3,
    table: BarChart3,
    file: BarChart3
  }[icon]

  return (
    <div className={`flex flex-col items-center justify-center py-12 ${className}`}>
      <div className="text-center">
        <IconComponent className="mx-auto h-12 w-12 text-gray-400 mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          {message}
        </h3>
        {description && (
          <p className="text-sm text-gray-500 mb-4 max-w-md">
            {description}
          </p>
        )}
        {action && (
          <Button 
            onClick={action.onClick}
            variant="outline"
          >
            {action.label}
          </Button>
        )}
      </div>
    </div>
  )
}