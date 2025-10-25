import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { ChevronDown, ChevronUp } from 'lucide-react'
import { ReactNode, useState } from 'react'
import { cn } from '@/lib/utils'

interface SectionProps {
  /** Arabic section title */
  title: string
  /** Section subtitle/description */
  subtitle?: string
  /** Section content */
  children: ReactNode
  /** Whether section can be collapsed */
  collapsible?: boolean
  /** Default collapsed state */
  defaultCollapsed?: boolean
  /** Loading state */
  isLoading?: boolean
  /** Action buttons for section header */
  actions?: ReactNode
  /** Additional CSS classes */
  className?: string
  /** Header CSS classes */
  headerClassName?: string
  /** Content CSS classes */
  contentClassName?: string
}

/**
 * Reusable section component for Result Detail page
 * Provides consistent spacing, typography, and optional collapsing
 */
export function Section({
  title,
  subtitle,
  children,
  collapsible = false,
  defaultCollapsed = false,
  isLoading = false,
  actions,
  className = '',
  headerClassName = '',
  contentClassName = ''
}: SectionProps) {
  const [collapsed, setCollapsed] = useState(defaultCollapsed)

  if (isLoading) {
    return (
      <Card className={className}>
        <CardHeader className={cn("pb-4", headerClassName)}>
          <div className="flex items-center justify-between">
            <div className="space-y-2">
              <Skeleton className="h-6 w-32" />
              {subtitle && <Skeleton className="h-4 w-48" />}
            </div>
            {actions && <Skeleton className="h-8 w-20" />}
          </div>
        </CardHeader>
        <CardContent className={cn("pt-0", contentClassName)}>
          <div className="space-y-4">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-32 w-full" />
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={cn("transition-all duration-200", className)}>
      <CardHeader className={cn("pb-4", headerClassName)}>
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <div className="flex items-center gap-2">
              <CardTitle className="text-lg font-semibold text-gray-900">
                {title}
              </CardTitle>
              {collapsible && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setCollapsed(!collapsed)}
                  className="h-6 w-6 p-0 hover:bg-gray-100"
                >
                  {collapsed ? (
                    <ChevronDown className="h-4 w-4" />
                  ) : (
                    <ChevronUp className="h-4 w-4" />
                  )}
                </Button>
              )}
            </div>
            {subtitle && (
              <p className="text-sm text-gray-600 mt-1">
                {subtitle}
              </p>
            )}
          </div>
          {actions && (
            <div className="flex items-center gap-2">
              {actions}
            </div>
          )}
        </div>
      </CardHeader>
      
      {(!collapsible || !collapsed) && (
        <CardContent className={cn("pt-0", contentClassName)}>
          {children}
        </CardContent>
      )}
    </Card>
  )
}

interface GridSectionProps {
  /** Section title */
  title: string
  /** Section subtitle */
  subtitle?: string
  /** Grid items */
  children: ReactNode
  /** Number of columns for desktop */
  columns?: 1 | 2 | 3 | 4
  /** Loading state */
  isLoading?: boolean
  /** Action buttons */
  actions?: ReactNode
  /** Additional CSS classes */
  className?: string
}

/**
 * Grid-based section for displaying cards/items in responsive columns
 */
export function GridSection({
  title,
  subtitle,
  children,
  columns = 2,
  isLoading = false,
  actions,
  className = ''
}: GridSectionProps) {
  const gridClasses = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 md:grid-cols-2',
    3: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-4'
  }

  return (
    <Section
      title={title}
      subtitle={subtitle}
      actions={actions}
      isLoading={isLoading}
      className={className}
      contentClassName="px-6 pb-6"
    >
      <div className={cn("grid gap-4", gridClasses[columns])}>
        {children}
      </div>
    </Section>
  )
}

interface StatsGridProps {
  /** Stats items */
  stats: Array<{
    label: string
    value: string | number
    sublabel?: string
    trend?: {
      value: string
      type: 'positive' | 'negative' | 'neutral'
    }
  }>
  /** Number of columns */
  columns?: 2 | 3 | 4
  /** Loading state */
  isLoading?: boolean
  /** Additional CSS classes */
  className?: string
}

/**
 * Pre-configured stats grid using KpiCard components
 */
export function StatsGrid({ 
  stats, 
  columns = 3, 
  isLoading = false,
  className = '' 
}: StatsGridProps) {
  const gridClasses = {
    2: 'grid-cols-1 sm:grid-cols-2',
    3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4'
  }

  if (isLoading) {
    return (
      <div className={cn("grid gap-4", gridClasses[columns], className)}>
        {Array.from({ length: columns }).map((_, i) => (
          <Card key={i}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <Skeleton className="h-4 w-24" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-8 w-16 mb-1" />
              <Skeleton className="h-3 w-20" />
            </CardContent>
          </Card>
        ))}
      </div>
    )
  }

  return (
    <div className={cn("grid gap-4", gridClasses[columns], className)}>
      {stats.map((stat, index) => (
        <Card key={index} className="transition-all duration-200 hover:shadow-md">
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium text-gray-600 leading-none">
              {stat.label}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-gray-900 leading-none mb-1">
              {stat.value}
            </div>
            {stat.sublabel && (
              <p className="text-xs text-gray-500 leading-none">
                {stat.sublabel}
              </p>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  )
}