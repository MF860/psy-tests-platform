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

// Swipeable card component
interface SwipeableCardProps {
  children: ReactNode
  onSwipeLeft?: () => void
  onSwipeRight?: () => void
  className?: string
  disabled?: boolean
}

export function SwipeableCard({
  children,
  onSwipeLeft,
  onSwipeRight,
  className,
  disabled = false
}: SwipeableCardProps) {
  const [isDragging, setIsDragging] = useState(false)
  const constraintsRef = useRef(null)
  
  const handleDragEnd = (_event: any, info: PanInfo) => {
    setIsDragging(false)
    
    if (disabled) return
    
    const swipeThreshold = 100
    const swipeVelocityThreshold = 500
    
    if (info.offset.x > swipeThreshold || info.velocity.x > swipeVelocityThreshold) {
      onSwipeRight?.()
    } else if (info.offset.x < -swipeThreshold || info.velocity.x < -swipeVelocityThreshold) {
      onSwipeLeft?.()
    }
  }
  
  return (
    <motion.div
      ref={constraintsRef}
      drag="x"
      dragConstraints={{ left: -50, right: 50 }}
      dragElastic={0.2}
      onDragStart={() => setIsDragging(true)}
      onDragEnd={handleDragEnd}
      whileDrag={{ scale: 0.98, rotateZ: isDragging ? 1 : 0 }}
      className={cn(
        'cursor-grab active:cursor-grabbing select-none',
        disabled && 'cursor-default',
        className
      )}
    >
      {children}
    </motion.div>
  )
}

// Bottom sheet component
interface BottomSheetProps {
  children: ReactNode
  isOpen: boolean
  onClose: () => void
  title?: string
  className?: string
}

export function BottomSheet({
  children,
  isOpen,
  onClose,
  title,
  className
}: BottomSheetProps) {
  const [dragY, setDragY] = useState(0)
  
  const handleDragEnd = (_event: any, info: PanInfo) => {
    if (info.offset.y > 100 || info.velocity.y > 500) {
      onClose()
    }
    setDragY(0)
  }
  
  return (
    <>
      {/* Backdrop */}
      {isOpen && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          onClick={onClose}
          className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm"
        />
      )}
      
      {/* Bottom Sheet */}
      {isOpen && (
        <motion.div
          initial={{ y: '100%' }}
          animate={{ y: 0 }}
          exit={{ y: '100%' }}
          transition={{ type: 'spring', damping: 30, stiffness: 300 }}
          drag="y"
          dragConstraints={{ top: 0, bottom: 300 }}
          dragElastic={0.1}
          onDragEnd={handleDragEnd}
          style={{ y: dragY }}
          className={cn(
            'fixed bottom-0 left-0 right-0 z-50',
            'bg-background border-t border-border rounded-t-2xl',
            'max-h-[90vh] overflow-hidden',
            safeArea.bottom,
            className
          )}
        >
          {/* Drag Handle */}
          <div className="flex justify-center pt-3 pb-2">
            <div className="w-8 h-1 bg-muted rounded-full" />
          </div>
          
          {/* Header */}
          {title && (
            <div className="px-6 pb-4">
              <h3 className="text-lg font-semibold">{title}</h3>
            </div>
          )}
          
          {/* Content */}
          <div className="px-6 pb-6 overflow-y-auto max-h-full">
            {children}
          </div>
        </motion.div>
      )}
    </>
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

// Mobile-optimized input
interface MobileInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  icon?: ReactNode
}

export function MobileInput({
  label,
  error,
  icon,
  className,
  ...props
}: MobileInputProps) {
  return (
    <div className="space-y-2">
      {label && (
        <label className="block text-sm font-medium text-gray-700">
          {label}
        </label>
      )}
      <div className="relative">
        {icon && (
          <div className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400">
            {icon}
          </div>
        )}
        <input
          className={cn(
            'w-full rounded-xl border border-gray-300',
            'px-4 py-3 text-base', // Larger text prevents iOS zoom
            'focus:ring-2 focus:ring-primary focus:border-transparent',
            'placeholder-gray-400',
            touchTargets.medium,
            icon && 'pl-10',
            error && 'border-red-500 focus:ring-red-500',
            className
          )}
          {...props}
        />
      </div>
      {error && (
        <p className="text-sm text-red-600">{error}</p>
      )}
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