import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { TrendingUp, TrendingDown, Minus } from 'lucide-react'

interface BandBadgeProps {
  /** T-Score value */
  score: number
  /** Show full text or compact version */
  variant?: 'full' | 'compact'
  /** Show trend icon */
  showTrend?: boolean
  /** Additional CSS classes */
  className?: string
}

/**
 * Enhanced T-Score band badge with visual indicators
 * 
 * T-Score Bands:
 * - Exceptional: >= 70 (Purple)
 * - Above Average: 60-69.9 (Green)  
 * - Average: 40-59.9 (Blue)
 * - Below Average: 30-39.9 (Orange)
 * - Concerning: < 30 (Red)
 */
export function BandBadge({ 
  score, 
  variant = 'full', 
  showTrend = false,
  className = '' 
}: BandBadgeProps) {
  const getBand = (score: number) => {
    if (score >= 70) return {
      label: variant === 'full' ? 'استثنائي' : 'استثنائي',
      color: 'purple',
      bgClass: 'bg-purple-100 text-purple-800 border-purple-200',
      trend: 'up'
    }
    if (score >= 60) return {
      label: variant === 'full' ? 'فوق المتوسط' : 'عالي',
      color: 'green', 
      bgClass: 'bg-green-100 text-green-800 border-green-200',
      trend: 'up'
    }
    if (score >= 40) return {
      label: variant === 'full' ? 'متوسط' : 'متوسط',
      color: 'blue',
      bgClass: 'bg-blue-100 text-blue-800 border-blue-200', 
      trend: 'neutral'
    }
    if (score >= 30) return {
      label: variant === 'full' ? 'دون المتوسط' : 'منخفض',
      color: 'orange',
      bgClass: 'bg-orange-100 text-orange-800 border-orange-200',
      trend: 'down'
    }
    return {
      label: variant === 'full' ? 'يحتاج متابعة' : 'ضعيف',
      color: 'red',
      bgClass: 'bg-red-100 text-red-800 border-red-200',
      trend: 'down'
    }
  }

  const band = getBand(score)
  const formattedScore = new Intl.NumberFormat('ar-EG', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1
  }).format(score)

  const TrendIcon = band.trend === 'up' ? TrendingUp 
                  : band.trend === 'down' ? TrendingDown 
                  : Minus

  return (
    <div className={cn("flex items-center gap-2", className)}>
      <Badge 
        variant="outline"
        className={cn("text-xs font-medium border", band.bgClass)}
      >
        <span className="font-mono">{formattedScore}</span>
        {variant === 'full' && (
          <>
            <span className="mx-1">•</span>
            <span>{band.label}</span>
          </>
        )}
        {showTrend && (
          <TrendIcon className="h-3 w-3 mr-1" />
        )}
      </Badge>
      
      {variant === 'compact' && (
        <span className={cn(
          "text-xs font-medium",
          band.color === 'purple' && "text-purple-700",
          band.color === 'green' && "text-green-700",
          band.color === 'blue' && "text-blue-700",
          band.color === 'orange' && "text-orange-700",
          band.color === 'red' && "text-red-700"
        )}>
          {band.label}
        </span>
      )}
    </div>
  )
}

interface PercentileBadgeProps {
  /** Percentile value (0-100) */
  percentile: number
  /** Additional CSS classes */
  className?: string
}

/**
 * Percentile badge with Arabic formatting
 */
export function PercentileBadge({ percentile, className = '' }: PercentileBadgeProps) {
  const formattedPercentile = new Intl.NumberFormat('ar-EG', {
    style: 'percent',
    minimumFractionDigits: 0,
    maximumFractionDigits: 1
  }).format(percentile / 100)

  const getPercentileColor = (p: number) => {
    if (p >= 90) return 'text-purple-700 bg-purple-50 border-purple-200'
    if (p >= 75) return 'text-green-700 bg-green-50 border-green-200'
    if (p >= 25) return 'text-blue-700 bg-blue-50 border-blue-200'
    if (p >= 10) return 'text-orange-700 bg-orange-50 border-orange-200'
    return 'text-red-700 bg-red-50 border-red-200'
  }

  return (
    <Badge 
      variant="outline"
      className={cn(
        "text-xs font-medium border font-mono",
        getPercentileColor(percentile),
        className
      )}
    >
      {formattedPercentile}
    </Badge>
  )
}