import { cn } from '../../lib/utils'
import { motion, PanInfo } from 'framer-motion'
import { ReactNode, useState, useRef } from 'react'

// Mobile breakpoints and utilities
export const breakpoints = {
  xs: '320px',
  sm: '640px', 
  md: '768px',
  lg: '1024px',
  xl: '1280px',
  '2xl': '1536px'
}

// Touch target size recommendations (minimum 44px for iOS, 48px for Android)
export const touchTargets = {
  small: 'min-h-[44px] min-w-[44px]',
  medium: 'min-h-[48px] min-w-[48px]', 
  large: 'min-h-[52px] min-w-[52px]'
}

// Safe area utilities for notches and rounded corners
export const safeArea = {
  top: 'pt-[env(safe-area-inset-top)]',
  bottom: 'pb-[env(safe-area-inset-bottom)]',
  left: 'pl-[env(safe-area-inset-left)]', 
  right: 'pr-[env(safe-area-inset-right)]',
  all: 'p-[env(safe-area-inset-top)_env(safe-area-inset-right)_env(safe-area-inset-bottom)_env(safe-area-inset-left)]'
}

// Mobile-first responsive container
interface MobileContainerProps {
  children: ReactNode
  className?: string
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | 'full'
  padding?: 'none' | 'sm' | 'md' | 'lg'
  safeArea?: boolean
}

export function MobileContainer({ 
  children, 
  className,
  maxWidth = 'lg',
  padding = 'md',
  safeArea: useSafeArea = true
}: MobileContainerProps) {
  const maxWidthClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md', 
    lg: 'max-w-4xl',
    xl: 'max-w-6xl',
    full: 'max-w-none'
  }
  
  const paddingClasses = {
    none: '',
    sm: 'px-3 sm:px-4',
    md: 'px-4 sm:px-6',
    lg: 'px-6 sm:px-8'
  }
  
  return (
    <div className={cn(
      'w-full mx-auto',
      maxWidthClasses[maxWidth],
      paddingClasses[padding],
      useSafeArea && safeArea.left,
      useSafeArea && safeArea.right,
      className
    )}>
      {children}
    </div>
  )
}

// Touch-optimized button
interface TouchButtonProps {
  children: ReactNode
  onClick?: () => void
  disabled?: boolean
  variant?: 'primary' | 'secondary' | 'outline' | 'ghost'
  size?: 'sm' | 'md' | 'lg'
  className?: string
  fullWidth?: boolean
}

export function TouchButton({
  children,
  onClick,
  disabled = false,
  variant = 'primary',
  size = 'md',
  className,
  fullWidth = false
}: TouchButtonProps) {
  const baseClasses = cn(
    'inline-flex items-center justify-center gap-2',
    'font-medium rounded-xl transition-all duration-200',
    'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2',
    'active:scale-[0.98] disabled:opacity-50 disabled:cursor-not-allowed',
    touchTargets.medium,
    fullWidth && 'w-full'
  )
  
  const variants = {
    primary: 'bg-primary text-primary-foreground hover:bg-primary/90 focus-visible:ring-primary',
    secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80 focus-visible:ring-secondary',
    outline: 'border-2 border-primary text-primary hover:bg-primary/5 focus-visible:ring-primary',
    ghost: 'text-primary hover:bg-primary/10 focus-visible:ring-primary'
  }
  
  const sizes = {
    sm: 'text-sm px-4 py-2 min-h-[40px]',
    md: 'text-base px-6 py-3 min-h-[48px]', 
    lg: 'text-lg px-8 py-4 min-h-[52px]'
  }
  
  return (
    <motion.button
      whileTap={{ scale: disabled ? 1 : 0.98 }}
      onClick={onClick}
      disabled={disabled}
      className={cn(
        baseClasses,
        variants[variant],
        sizes[size],
        className
      )}
    >
      {children}
    </motion.button>
  )
}

// Mobile navigation tabs
interface MobileTabsProps {
  tabs: { id: string; label: string; icon?: ReactNode }[]
  activeTab: string
  onChange: (tabId: string) => void
  className?: string
}

export function MobileTabs({
  tabs,
  activeTab,
  onChange,
  className
}: MobileTabsProps) {
  return (
    <div className={cn(
      'flex bg-muted rounded-xl p-1',
      'overflow-x-auto scrollbar-hide',
      className
    )}>
      {tabs.map((tab) => (
        <TouchButton
          key={tab.id}
          onClick={() => onChange(tab.id)}
          variant={activeTab === tab.id ? 'primary' : 'ghost'}
          size="sm"
          className={cn(
            'flex-1 min-w-0 whitespace-nowrap',
            'text-xs sm:text-sm',
            activeTab === tab.id ? 'bg-background shadow-sm' : 'hover:bg-transparent'
          )}
        >
          {tab.icon && <span className="flex-shrink-0">{tab.icon}</span>}
          <span className="truncate">{tab.label}</span>
        </TouchButton>
      ))}
    </div>
  )
}

// Mobile-optimized table
interface MobileTableProps {
  headers: string[]
  data: any[]
  onRowClick?: (item: any, index: number) => void
  className?: string
}

export function MobileTable({
  headers,
  data,
  onRowClick,
  className
}: MobileTableProps) {
  return (
    <div className={cn('space-y-2', className)}>
      {/* Desktop Table - Hidden on mobile */}
      <div className="hidden md:block overflow-x-auto">
        <table className="min-w-full">
          <thead className="bg-gray-50">
            <tr>
              {headers.map((header, index) => (
                <th key={index} className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                  {header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {data.map((item, index) => (
              <motion.tr
                key={index}
                whileHover={{ backgroundColor: 'rgba(59, 130, 246, 0.05)' }}
                onClick={() => onRowClick?.(item, index)}
                className={cn(
                  'cursor-pointer transition-colors',
                  onRowClick && 'hover:bg-blue-50'
                )}
              >
                {Object.values(item).map((value, cellIndex) => (
                  <td key={cellIndex} className="px-6 py-4 whitespace-nowrap text-sm">
                    {String(value)}
                  </td>
                ))}
              </motion.tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile Cards - Shown on mobile */}
      <div className="md:hidden space-y-3">
        {data.map((item, index) => (
          <motion.div
            key={index}
            whileTap={{ scale: onRowClick ? 0.98 : 1 }}
            onClick={() => onRowClick?.(item, index)}
            className={cn(
              'bg-white rounded-xl border border-gray-200 p-4',
              'shadow-sm transition-shadow',
              onRowClick && 'cursor-pointer hover:shadow-md active:shadow-lg',
              touchTargets.medium
            )}
          >
            {Object.entries(item).map(([key, value], fieldIndex) => (
              <div key={fieldIndex} className="flex justify-between items-center py-2 first:pt-0 last:pb-0">
                <span className="text-sm font-medium text-gray-600">
                  {headers[fieldIndex] || key}
                </span>
                <span className="text-sm text-gray-900 font-mono">
                  {String(value)}
                </span>
              </div>
            ))}
          </motion.div>
        ))}
      </div>
    </div>
  )
}

// Sticky mobile header
interface StickyMobileHeaderProps {
  children: ReactNode
  className?: string
  blur?: boolean
}

export function StickyMobileHeader({
  children,
  className,
  blur = true
}: StickyMobileHeaderProps) {
  return (
    <div className={cn(
      'sticky top-0 z-40 w-full border-b border-border',
      blur ? 'bg-background/80 backdrop-blur-md supports-[backdrop-filter]:bg-background/60' : 'bg-background',
      safeArea.top,
      className
    )}>
      {children}
    </div>
  )
}

// Mobile-responsive grid
interface ResponsiveGridProps {
  children: ReactNode
  cols?: {
    xs?: number
    sm?: number  
    md?: number
    lg?: number
    xl?: number
  }
  gap?: 'sm' | 'md' | 'lg'
  className?: string
}

export function ResponsiveGrid({
  children,
  cols = { xs: 1, sm: 2, md: 3, lg: 4 },
  gap = 'md',
  className
}: ResponsiveGridProps) {
  const colClasses = [
    cols.xs && `grid-cols-${cols.xs}`,
    cols.sm && `sm:grid-cols-${cols.sm}`,
    cols.md && `md:grid-cols-${cols.md}`,
    cols.lg && `lg:grid-cols-${cols.lg}`,
    cols.xl && `xl:grid-cols-${cols.xl}`
  ].filter(Boolean).join(' ')
  
  const gapClasses = {
    sm: 'gap-2',
    md: 'gap-4',
    lg: 'gap-6'
  }
  
  return (
    <div className={cn(
      'grid',
      colClasses,
      gapClasses[gap],
      className
    )}>
      {children}
    </div>
  )
}