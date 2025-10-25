import { motion } from 'framer-motion'
import { Skeleton } from './skeleton'
import { Card, CardContent, CardHeader } from './card'
import { cn } from '../../lib/utils'

interface ProgressiveLoaderProps {
  stage: 'initial' | 'fetching' | 'processing' | 'complete'
  className?: string
  children?: React.ReactNode
}

/**
 * Progressive loader that shows different states based on loading stage
 * Provides smooth transitions between loading phases
 */
export function ProgressiveLoader({ stage, className, children }: ProgressiveLoaderProps) {
  return (
    <div className={cn("relative overflow-hidden", className)}>
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: stage === 'complete' ? 1 : 0 }}
        transition={{ duration: 0.3 }}
        className="relative z-10"
      >
        {children}
      </motion.div>

      {/* Loading overlays */}
      {stage !== 'complete' && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={{ opacity: 0 }}
          className="absolute inset-0 z-20 bg-background/80 backdrop-blur-sm"
        >
          <div className="flex items-center justify-center h-full">
            <LoadingIndicator stage={stage} />
          </div>
        </motion.div>
      )}
    </div>
  )
}

interface LoadingIndicatorProps {
  stage: 'initial' | 'fetching' | 'processing' | 'complete'
  size?: 'sm' | 'md' | 'lg'
}

function LoadingIndicator({ stage, size = 'md' }: LoadingIndicatorProps) {
  const sizeClasses = {
    sm: 'w-6 h-6',
    md: 'w-8 h-8', 
    lg: 'w-12 h-12'
  }

  const messages = {
    initial: 'جاري التحضير...',
    fetching: 'جاري جلب السؤال...',
    processing: 'جاري المعالجة...',
    complete: 'تم بنجاح!'
  }

  return (
    <div className="flex flex-col items-center gap-4">
      {/* Animated spinner */}
      <div className="relative">
        <motion.div
          animate={{ rotate: 360 }}
          transition={{ duration: 1, repeat: Infinity, ease: "linear" }}
          className={cn(
            "border-4 border-primary/20 border-t-primary rounded-full",
            sizeClasses[size]
          )}
        />
        
        {/* Pulsing dot in center */}
        <motion.div
          animate={{ scale: [1, 1.2, 1] }}
          transition={{ duration: 0.8, repeat: Infinity }}
          className="absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2 w-2 h-2 bg-primary rounded-full"
        />
      </div>

      {/* Stage message */}
      <motion.p
        initial={{ opacity: 0, y: 10 }}
        animate={{ opacity: 1, y: 0 }}
        key={stage}
        className="text-sm text-muted-foreground font-medium"
      >
        {messages[stage]}
      </motion.p>

      {/* Progress dots */}
      <div className="flex gap-2">
        {['initial', 'fetching', 'processing'].map((s, i) => (
          <motion.div
            key={s}
            animate={{
              scale: stage === s ? 1.2 : 1,
              backgroundColor: ['initial', 'fetching', 'processing'].indexOf(stage) >= i 
                ? 'hsl(var(--primary))' 
                : 'hsl(var(--muted))'
            }}
            transition={{ duration: 0.3 }}
            className="w-2 h-2 rounded-full"
          />
        ))}
      </div>
    </div>
  )
}

/**
 * Enhanced skeleton loader with wave animation
 */
export function SkeletonWave({ className, ...props }: React.ComponentProps<typeof Skeleton>) {
  return (
    <div className={cn("relative overflow-hidden", className)}>
      <Skeleton className="w-full h-full" {...props} />
      <motion.div
        animate={{ x: ['100%', '-100%'] }}
        transition={{ duration: 1.5, repeat: Infinity, ease: "linear" }}
        className="absolute inset-0 bg-gradient-to-r from-transparent via-white/40 to-transparent"
      />
    </div>
  )
}

/**
 * Card skeleton with progressive reveal
 */
export function CardSkeleton({ className, showHeader = true }: { className?: string; showHeader?: boolean }) {
  return (
    <Card className={cn("overflow-hidden", className)}>
      {showHeader && (
        <CardHeader>
          <motion.div
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.1 }}
          >
            <SkeletonWave className="h-6 w-48" />
          </motion.div>
          <motion.div
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.2 }}
          >
            <SkeletonWave className="h-4 w-32" />
          </motion.div>
        </CardHeader>
      )}
      <CardContent>
        <div className="space-y-3">
          {[0, 1, 2].map((i) => (
            <motion.div
              key={i}
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{ delay: 0.3 + i * 0.1 }}
            >
              <SkeletonWave 
                className={cn(
                  "h-4",
                  i === 0 ? "w-full" : i === 1 ? "w-3/4" : "w-1/2"
                )} 
              />
            </motion.div>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}

/**
 * Question loading skeleton for exam interface
 */
export function QuestionSkeleton({ className }: { className?: string }) {
  return (
    <div className={cn("space-y-6", className)}>
      {/* Question text skeleton */}
      <div className="space-y-4">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
        >
          <SkeletonWave className="h-8 w-full" />
        </motion.div>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
        >
          <SkeletonWave className="h-6 w-3/4" />
        </motion.div>
      </div>
      
      {/* Options skeleton */}
      <div className="space-y-3">
        {Array.from({ length: 5 }).map((_, i) => (
          <motion.div
            key={i}
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ delay: 0.3 + i * 0.1 }}
            className="flex items-center gap-3"
          >
            <SkeletonWave className="w-4 h-4 rounded-full" />
            <SkeletonWave className="h-4 flex-1" />
          </motion.div>
        ))}
      </div>
    </div>
  )
}