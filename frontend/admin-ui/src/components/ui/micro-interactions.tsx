import { motion, MotionProps } from 'framer-motion'
import { ReactNode, forwardRef } from 'react'
import { cn } from '@/lib/utils'

// Animation presets for consistent micro-interactions
export const entranceAnimations = {
  fadeIn: {
    initial: { opacity: 0 },
    animate: { opacity: 1 },
    exit: { opacity: 0 },
    transition: { duration: 0.2 }
  },
  
  slideUp: {
    initial: { opacity: 0, y: 20 },
    animate: { opacity: 1, y: 0 },
    exit: { opacity: 0, y: -20 },
    transition: { duration: 0.3, ease: [0.4, 0, 0.2, 1] }
  },
  
  slideDown: {
    initial: { opacity: 0, y: -20 },
    animate: { opacity: 1, y: 0 },
    exit: { opacity: 0, y: 20 },
    transition: { duration: 0.3, ease: [0.4, 0, 0.2, 1] }
  },
  
  slideInLeft: {
    initial: { opacity: 0, x: -20 },
    animate: { opacity: 1, x: 0 },
    exit: { opacity: 0, x: 20 },
    transition: { duration: 0.3, ease: [0.4, 0, 0.2, 1] }
  },
  
  slideInRight: {
    initial: { opacity: 0, x: 20 },
    animate: { opacity: 1, x: 0 },
    exit: { opacity: 0, x: -20 },
    transition: { duration: 0.3, ease: [0.4, 0, 0.2, 1] }
  },
  
  scaleIn: {
    initial: { opacity: 0, scale: 0.95 },
    animate: { opacity: 1, scale: 1 },
    exit: { opacity: 0, scale: 0.95 },
    transition: { duration: 0.2, ease: [0.4, 0, 0.2, 1] }
  }
}

export const interactionAnimations = {
  // Hover interactions
  hover: {
    scale: 1.02,
    transition: { duration: 0.2, ease: [0.4, 0, 0.2, 1] }
  },
  
  hoverScale: {
    scale: 1.05,
    transition: { duration: 0.2, ease: [0.4, 0, 0.2, 1] }
  },
  
  // Tap interactions
  tap: {
    scale: 0.98,
    transition: { duration: 0.1 }
  },
  
  tapScale: {
    scale: 0.95,
    transition: { duration: 0.1 }
  }
}

export const staggerAnimations = {
  container: {
    animate: {
      transition: {
        staggerChildren: 0.1
      }
    }
  },
  
  item: {
    initial: { opacity: 0, y: 20 },
    animate: { opacity: 1, y: 0 },
    transition: { duration: 0.3, ease: [0.4, 0, 0.2, 1] }
  }
}

// Interactive wrapper component
interface InteractiveProps extends MotionProps {
  children: ReactNode
  className?: string
  hover?: boolean
  tap?: boolean
  scale?: boolean
  disabled?: boolean
}

export const Interactive = forwardRef<HTMLDivElement, InteractiveProps>(
  ({ children, className, hover = true, tap = true, scale = false, disabled = false, ...props }, ref) => {
    const whileHover = !disabled && hover ? (scale ? interactionAnimations.hoverScale : interactionAnimations.hover) : undefined
    const whileTap = !disabled && tap ? (scale ? interactionAnimations.tapScale : interactionAnimations.tap) : undefined
    
    return (
      <motion.div
        ref={ref}
        whileHover={whileHover}
        whileTap={whileTap}
        className={cn(
          "transition-colors duration-200",
          !disabled && "cursor-pointer",
          disabled && "opacity-50 cursor-not-allowed",
          className
        )}
        {...props}
      >
        {children}
      </motion.div>
    )
  }
)

Interactive.displayName = "Interactive"

// Animated entrance wrapper
interface AnimatedEntranceProps {
  children: ReactNode
  animation?: keyof typeof entranceAnimations
  delay?: number
  className?: string
}

export function AnimatedEntrance({ 
  children, 
  animation = 'fadeIn', 
  delay = 0,
  className 
}: AnimatedEntranceProps) {
  const animationConfig = entranceAnimations[animation]
  
  return (
    <motion.div
      initial={animationConfig.initial}
      animate={animationConfig.animate}
      exit={animationConfig.exit}
      transition={{ ...animationConfig.transition, delay }}
      className={className}
    >
      {children}
    </motion.div>
  )
}

// Staggered list animations
interface StaggeredListProps {
  children: ReactNode[]
  className?: string
  itemClassName?: string
  staggerDelay?: number
}

export function StaggeredList({ 
  children, 
  className, 
  itemClassName,
  staggerDelay = 0.1 
}: StaggeredListProps) {
  return (
    <motion.div
      initial="initial"
      animate="animate"
      variants={staggerAnimations.container}
      className={className}
    >
      {children.map((child, index) => (
        <motion.div
          key={index}
          variants={staggerAnimations.item}
          transition={{ delay: index * staggerDelay }}
          className={itemClassName}
        >
          {child}
        </motion.div>
      ))}
    </motion.div>
  )
}

// Floating action button with micro-interactions
interface FloatingActionButtonProps {
  onClick: () => void
  children: ReactNode
  className?: string
  disabled?: boolean
  variant?: 'primary' | 'secondary' | 'success' | 'warning' | 'error'
}

export function FloatingActionButton({ 
  onClick, 
  children, 
  className,
  disabled = false,
  variant = 'primary'
}: FloatingActionButtonProps) {
  const variants = {
    primary: 'bg-primary text-primary-foreground hover:bg-primary/90',
    secondary: 'bg-secondary text-secondary-foreground hover:bg-secondary/80',
    success: 'bg-green-500 text-white hover:bg-green-600',
    warning: 'bg-amber-500 text-white hover:bg-amber-600',
    error: 'bg-red-500 text-white hover:bg-red-600'
  }
  
  return (
    <motion.button
      onClick={onClick}
      disabled={disabled}
      whileHover={!disabled ? { scale: 1.05, y: -2 } : undefined}
      whileTap={!disabled ? { scale: 0.95 } : undefined}
      initial={{ opacity: 0, scale: 0 }}
      animate={{ opacity: 1, scale: 1 }}
      exit={{ opacity: 0, scale: 0 }}
      transition={{ duration: 0.2, ease: [0.4, 0, 0.2, 1] }}
      className={cn(
        "fixed bottom-6 right-6 z-50",
        "w-14 h-14 rounded-full shadow-lg",
        "flex items-center justify-center",
        "transition-all duration-200",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2",
        variants[variant],
        disabled && "opacity-50 cursor-not-allowed",
        className
      )}
    >
      {children}
    </motion.button>
  )
}

// Progress indicator with smooth animations
interface AnimatedProgressProps {
  value: number
  max?: number
  className?: string
  showPercentage?: boolean
  color?: 'primary' | 'success' | 'warning' | 'error'
}

export function AnimatedProgress({ 
  value, 
  max = 100, 
  className,
  showPercentage = false,
  color = 'primary'
}: AnimatedProgressProps) {
  const percentage = Math.min((value / max) * 100, 100)
  
  const colorClasses = {
    primary: 'bg-primary',
    success: 'bg-green-500',
    warning: 'bg-amber-500', 
    error: 'bg-red-500'
  }
  
  return (
    <div className={cn("relative", className)}>
      {/* Track */}
      <div className="w-full bg-muted rounded-full h-2 overflow-hidden">
        {/* Progress bar */}
        <motion.div
          initial={{ width: 0 }}
          animate={{ width: `${percentage}%` }}
          transition={{ 
            duration: 0.5, 
            ease: [0.4, 0, 0.2, 1] 
          }}
          className={cn("h-full rounded-full", colorClasses[color])}
        />
      </div>
      
      {/* Percentage text */}
      {showPercentage && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.3 }}
          className="absolute -top-6 left-1/2 transform -translate-x-1/2 text-xs font-medium text-muted-foreground"
        >
          {Math.round(percentage)}%
        </motion.div>
      )}
    </div>
  )
}

// Notification badge with bounce animation
interface NotificationBadgeProps {
  count: number
  className?: string
  maxCount?: number
}

export function NotificationBadge({ 
  count, 
  className,
  maxCount = 99 
}: NotificationBadgeProps) {
  const displayCount = count > maxCount ? `${maxCount}+` : count.toString()
  
  if (count === 0) return null
  
  return (
    <motion.div
      initial={{ scale: 0 }}
      animate={{ scale: 1 }}
      exit={{ scale: 0 }}
      transition={{ 
        type: "spring", 
        stiffness: 500, 
        damping: 30 
      }}
      className={cn(
        "absolute -top-1 -right-1 min-w-[18px] h-[18px]",
        "bg-red-500 text-white text-xs font-bold",
        "rounded-full flex items-center justify-center",
        "border-2 border-background",
        className
      )}
    >
      {displayCount}
    </motion.div>
  )
}

// Pulse animation for loading states
export function PulseLoader({ className, size = 'md' }: { 
  className?: string
  size?: 'sm' | 'md' | 'lg' 
}) {
  const sizes = {
    sm: 'w-4 h-4',
    md: 'w-6 h-6',
    lg: 'w-8 h-8'
  }
  
  return (
    <div className={cn("flex items-center justify-center", className)}>
      <motion.div
        animate={{
          scale: [1, 1.2, 1],
          opacity: [0.5, 1, 0.5]
        }}
        transition={{
          duration: 1.5,
          repeat: Infinity,
          ease: "easeInOut"
        }}
        className={cn(
          "bg-primary rounded-full",
          sizes[size]
        )}
      />
    </div>
  )
}