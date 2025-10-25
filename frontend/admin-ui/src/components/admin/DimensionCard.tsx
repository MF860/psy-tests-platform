import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { BandBadge, PercentileBadge } from './BandBadge'
import { Progress } from '../ui/progress'
import { cn } from '@/lib/utils'
import { formatters } from '@/lib/adminContract'

interface DimensionScore {
  dimension: string
  raw: number
  z: number
  t: number
  percentile: number
}

interface DimensionCardProps {
  /** Dimension score data */
  dimension: DimensionScore
  /** Card variant */
  variant?: 'default' | 'compact' | 'detailed'
  /** Show raw score */
  showRaw?: boolean
  /** Show Z-score */
  showZ?: boolean
  /** Show percentile */
  showPercentile?: boolean
  /** Additional CSS classes */
  className?: string
}

/**
 * Enhanced dimension score card with multiple display formats
 */
export function DimensionCard({
  dimension,
  variant = 'default',
  showRaw = false,
  showZ = false, 
  showPercentile = true,
  className = ''
}: DimensionCardProps) {
  const { dimension: name, raw, z, t, percentile } = dimension

  // Calculate progress bar value (0-100 based on T-score range 0-100)
  const progressValue = Math.max(0, Math.min(100, t))

  if (variant === 'compact') {
    return (
      <Card className={cn("transition-all duration-200 hover:shadow-sm", className)}>
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex-1 min-w-0">
              <h4 className="font-medium text-sm text-gray-900 truncate">
                {name}
              </h4>
              {showRaw && (
                <p className="text-xs text-gray-500 mt-1">
                  خام: {formatters.number(raw)}
                </p>
              )}
            </div>
            <div className="flex flex-col items-end gap-1">
              <BandBadge score={t} variant="compact" />
              {showPercentile && <PercentileBadge percentile={percentile} />}
            </div>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (variant === 'detailed') {
    return (
      <Card className={cn("transition-all duration-200 hover:shadow-md", className)}>
        <CardHeader className="pb-3">
          <CardTitle className="text-base font-semibold text-gray-900">
            {name}
          </CardTitle>
        </CardHeader>
        <CardContent className="pt-0 space-y-4">
          {/* T-Score with progress bar */}
          <div>
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-600">T-Score</span>
              <BandBadge score={t} variant="full" />
            </div>
            <Progress 
              value={progressValue} 
              className="h-2"
              // Custom colors based on score band
              style={{
                '--progress-background': t >= 70 ? '#8b5cf6' 
                  : t >= 60 ? '#10b981' 
                  : t >= 40 ? '#3b82f6'
                  : t >= 30 ? '#f59e0b' 
                  : '#ef4444'
              } as React.CSSProperties}
            />
          </div>

          {/* Statistics grid */}
          <div className="grid grid-cols-2 gap-4 text-sm">
            {showRaw && (
              <div>
                <span className="text-gray-500">النتيجة الخام</span>
                <div className="font-medium font-mono">
                  {formatters.number(raw)}
                </div>
              </div>
            )}
            
            {showZ && (
              <div>
                <span className="text-gray-500">Z-Score</span>
                <div className="font-medium font-mono">
                  {formatters.tScore(z)}
                </div>
              </div>
            )}
            
            {showPercentile && (
              <div>
                <span className="text-gray-500">المئوي</span>
                <div className="font-medium">
                  <PercentileBadge percentile={percentile} />
                </div>
              </div>
            )}
            
            <div>
              <span className="text-gray-500">T-Score</span>
              <div className="font-medium font-mono">
                {formatters.tScore(t)}
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    )
  }

  // Default variant
  return (
    <Card className={cn("transition-all duration-200 hover:shadow-md", className)}>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium text-gray-900 leading-none">
          {name}
        </CardTitle>
      </CardHeader>
      <CardContent className="pt-0">
        <div className="flex items-center justify-between mb-3">
          <BandBadge score={t} variant="full" showTrend />
          {showPercentile && <PercentileBadge percentile={percentile} />}
        </div>
        
        <Progress 
          value={progressValue} 
          className="h-1.5"
        />
        
        {(showRaw || showZ) && (
          <div className="flex justify-between text-xs text-gray-500 mt-2">
            {showRaw && <span>خام: {formatters.number(raw)}</span>}
            {showZ && <span>Z: {formatters.tScore(z)}</span>}
          </div>
        )}
      </CardContent>
    </Card>
  )
}

interface DimensionsGridProps {
  /** Array of dimension scores */
  dimensions: DimensionScore[]
  /** Card variant to use */
  variant?: 'default' | 'compact' | 'detailed'
  /** Number of columns */
  columns?: 1 | 2 | 3
  /** Show raw scores */
  showRaw?: boolean
  /** Show Z-scores */
  showZ?: boolean
  /** Show percentiles */
  showPercentile?: boolean
  /** Additional CSS classes */
  className?: string
}

/**
 * Grid of dimension cards with responsive layout
 */
export function DimensionsGrid({
  dimensions,
  variant = 'default',
  columns = 2,
  showRaw = false,
  showZ = false,
  showPercentile = true,
  className = ''
}: DimensionsGridProps) {
  const gridClasses = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 md:grid-cols-2', 
    3: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3'
  }

  // Sort dimensions by T-score (highest first) for better UX
  const sortedDimensions = [...dimensions].sort((a, b) => b.t - a.t)

  return (
    <div className={cn("grid gap-4", gridClasses[columns], className)}>
      {sortedDimensions.map((dimension, index) => (
        <DimensionCard
          key={`${dimension.dimension}-${index}`}
          dimension={dimension}
          variant={variant}
          showRaw={showRaw}
          showZ={showZ}
          showPercentile={showPercentile}
        />
      ))}
    </div>
  )
}