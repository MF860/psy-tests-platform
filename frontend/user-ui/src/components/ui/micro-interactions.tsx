import { motion, MotionProps } from 'framer-motion'
import { ReactNode, forwardRef } from 'react'
import { cn } from '../../lib/utils'

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
    transition: { duration: 0.3, ease: "easeOut" }
  },
  
  slideDown: {
    initial: { opacity: 0, y: -20 },
    animate: { opacity: 1, y: 0 },
    exit: { opacity: 0, y: 20 },
    transition: { duration: 0.3, ease: "easeOut" }
  },
  
  slideInLeft: {
    initial: { opacity: 0, x: -20 },
    animate: { opacity: 1, x: 0 },
    exit: { opacity: 0, x: 20 },
    transition: { duration: 0.3, ease: "easeOut" }
  },
  
  slideInRight: {
    initial: { opacity: 0, x: 20 },
    animate: { opacity: 1, x: 0 },
    exit: { opacity: 0, x: -20 },
    transition: { duration: 0.3, ease: "easeOut" }
  },
  
  scaleIn: {
    initial: { opacity: 0, scale: 0.95 },
    animate: { opacity: 1, scale: 1 },
    exit: { opacity: 0, scale: 0.95 },
    transition: { duration: 0.2, ease: "easeOut" }
  }
} as const

export const interactionAnimations = {
  // Hover interactions
  hover: {
    scale: 1.02
  },
  
  hoverScale: {
    scale: 1.05
  },
  
  // Tap interactions
  tap: {
    scale: 0.98
  },
  
  tapScale: {
    scale: 0.95
  }
} as const

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

// Question transition animation
export function QuestionTransition({ 
  children, 
  questionId, 
  className 
}: { 
  children: ReactNode
  questionId: string | number
  className?: string 
}) {
  return (
    <motion.div
      key={questionId}
      initial={{ opacity: 0, x: 30 }}
      animate={{ opacity: 1, x: 0 }}
      exit={{ opacity: 0, x: -30 }}
      transition={{ 
        duration: 0.3, 
        ease: [0.4, 0, 0.2, 1] 
      }}
      className={className}
    >
      {children}
    </motion.div>
  )
}

// Answer option hover effect
export function AnswerOption({ 
  children, 
  selected = false, 
  onClick,
  className 
}: { 
  children: ReactNode
  selected?: boolean
  onClick?: () => void
  className?: string 
}) {
  return (
    <motion.div
      whileHover={{ scale: 1.02, backgroundColor: 'rgba(59, 130, 246, 0.05)' }}
      whileTap={{ scale: 0.98 }}
      onClick={onClick}
      className={cn(
        "cursor-pointer rounded-lg border-2 p-4 transition-all duration-200",
        selected 
          ? "border-primary bg-primary/5 shadow-md" 
          : "border-gray-200 hover:border-gray-300",
        className
      )}
    >
      {children}
    </motion.div>
  )
}