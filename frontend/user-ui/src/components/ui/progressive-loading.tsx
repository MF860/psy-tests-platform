import { useState, useEffect, ReactNode } from 'react'
import { useIntersectionObserver } from '../../lib/performance'

interface ProgressiveLoadingProps {
  children: ReactNode
  fallback?: ReactNode
  delay?: number
  rootMargin?: string
  threshold?: number
  className?: string
}

// Progressive loading component that waits for intersection + optional delay
export function ProgressiveLoading({
  children,
  fallback = <div className="animate-pulse bg-gray-200 rounded h-32"></div>,
  delay = 0,
  rootMargin = '50px',
  threshold = 0.1,
  className = ''
}: ProgressiveLoadingProps) {
  const [elementRef, isVisible] = useIntersectionObserver({
    rootMargin,
    threshold
  })
  const [shouldLoad, setShouldLoad] = useState(false)

  useEffect(() => {
    if (isVisible) {
      if (delay > 0) {
        const timer = setTimeout(() => setShouldLoad(true), delay)
        return () => clearTimeout(timer)
      } else {
        setShouldLoad(true)
      }
    }
  }, [isVisible, delay])

  return (
    <div ref={elementRef} className={className}>
      {shouldLoad ? children : fallback}
    </div>
  )
}

// Skeleton components for different content types
export function ChartSkeleton({ className = '' }: { className?: string }) {
  return (
    <div className={`animate-pulse space-y-4 ${className}`} dir="rtl">
      <div className="h-4 bg-gray-200 rounded w-1/4"></div>
      <div className="h-64 bg-gray-200 rounded"></div>
      <div className="flex space-x-4">
        <div className="h-4 bg-gray-200 rounded w-16"></div>
        <div className="h-4 bg-gray-200 rounded w-16"></div>
        <div className="h-4 bg-gray-200 rounded w-16"></div>
      </div>
    </div>
  )
}

export function CardSkeleton({ className = '' }: { className?: string }) {
  return (
    <div className={`animate-pulse p-6 bg-white rounded-lg border ${className}`} dir="rtl">
      <div className="flex items-center space-x-4 mb-4">
        <div className="h-12 w-12 bg-gray-200 rounded-full"></div>
        <div className="space-y-2 flex-1">
          <div className="h-4 bg-gray-200 rounded w-1/2"></div>
          <div className="h-3 bg-gray-200 rounded w-1/4"></div>
        </div>
      </div>
      <div className="space-y-3">
        <div className="h-4 bg-gray-200 rounded w-full"></div>
        <div className="h-4 bg-gray-200 rounded w-3/4"></div>
        <div className="h-4 bg-gray-200 rounded w-1/2"></div>
      </div>
    </div>
  )
}

export function FormSkeleton({ className = '' }: { className?: string }) {
  return (
    <div className={`animate-pulse space-y-6 ${className}`} dir="rtl">
      <div className="space-y-2">
        <div className="h-4 bg-gray-200 rounded w-1/4"></div>
        <div className="h-10 bg-gray-200 rounded w-full"></div>
      </div>
      <div className="space-y-2">
        <div className="h-4 bg-gray-200 rounded w-1/3"></div>
        <div className="h-10 bg-gray-200 rounded w-full"></div>
      </div>
      <div className="space-y-2">
        <div className="h-4 bg-gray-200 rounded w-1/4"></div>
        <div className="h-24 bg-gray-200 rounded w-full"></div>
      </div>
      <div className="flex space-x-4">
        <div className="h-10 bg-gray-200 rounded w-24"></div>
        <div className="h-10 bg-gray-200 rounded w-24"></div>
      </div>
    </div>
  )
}

// Progressive image loading with blur-up effect
interface ProgressiveImageProps {
  src: string
  alt: string
  className?: string
  placeholderSrc?: string
  onLoad?: () => void
}

export function ProgressiveImage({
  src,
  alt,
  className = '',
  placeholderSrc,
  onLoad
}: ProgressiveImageProps) {
  const [isLoaded, setIsLoaded] = useState(false)
  const [currentSrc, setCurrentSrc] = useState(placeholderSrc || 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjNmNGY2Ii8+PC9zdmc+')

  useEffect(() => {
    const img = new Image()
    img.onload = () => {
      setCurrentSrc(src)
      setIsLoaded(true)
      onLoad?.()
    }
    img.src = src
  }, [src, onLoad])

  return (
    <div className={`relative overflow-hidden ${className}`}>
      <img
        src={currentSrc}
        alt={alt}
        className={`w-full h-full object-cover transition-all duration-700 ${
          isLoaded ? 'opacity-100 blur-0' : 'opacity-70 blur-sm'
        }`}
      />
      {!isLoaded && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-100 bg-opacity-50">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-400"></div>
        </div>
      )}
    </div>
  )
}

// Content prioritization - load above-the-fold first
interface ContentPriorityProps {
  priority: 'high' | 'medium' | 'low'
  children: ReactNode
  fallback?: ReactNode
  className?: string
}

export function ContentPriority({
  priority,
  children,
  fallback = <div className="animate-pulse bg-gray-200 rounded h-24"></div>,
  className = ''
}: ContentPriorityProps) {
  const [shouldLoad, setShouldLoad] = useState(priority === 'high')

  useEffect(() => {
    if (priority === 'medium') {
      // Load medium priority after a short delay
      const timer = setTimeout(() => setShouldLoad(true), 100)
      return () => clearTimeout(timer)
    } else if (priority === 'low') {
      // Load low priority when the page is idle
      const timer = setTimeout(() => setShouldLoad(true), 500)
      return () => clearTimeout(timer)
    }
  }, [priority])

  return (
    <div className={className}>
      {shouldLoad ? children : fallback}
    </div>
  )
}