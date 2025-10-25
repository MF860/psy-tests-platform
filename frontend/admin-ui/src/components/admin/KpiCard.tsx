import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Badge } from '@/components/ui/badge'

interface KpiCardProps {
  /** Arabic label for the KPI */
  label: string
  /** Formatted value to display */
  value: string | number
  /** Optional sublabel/description */
  sublabel?: string
  /** Optional trend indicator */
  trend?: {
    value: string
    type: 'positive' | 'negative' | 'neutral'
  }
  /** Loading state */
  isLoading?: boolean
  /** Additional CSS classes */
  className?: string
}

export function KpiCard({ 
  label, 
  value, 
  sublabel, 
  trend, 
  isLoading = false,
  className = '' 
}: KpiCardProps) {
  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <Skeleton className="h-4 w-24" />
          {trend && <Skeleton className="h-4 w-12" />}
        </CardHeader>
        <CardContent>
          <Skeleton className="h-8 w-16 mb-1" />
          {sublabel && <Skeleton className="h-3 w-20" />}
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={`transition-all duration-200 hover:shadow-md ${className}`}>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-gray-600 leading-none">
          {label}
        </CardTitle>
        {trend && (
          <Badge 
            variant={trend.type === 'positive' ? 'default' : trend.type === 'negative' ? 'destructive' : 'secondary'}
            className="text-xs"
          >
            {trend.value}
          </Badge>
        )}
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold text-gray-900 leading-none mb-1">
          {value}
        </div>
        {sublabel && (
          <p className="text-xs text-gray-500 leading-none">
            {sublabel}
          </p>
        )}
      </CardContent>
    </Card>
  )
}