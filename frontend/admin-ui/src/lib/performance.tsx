import { lazy, Suspense, ComponentType, ReactNode, useState, useEffect, useRef, RefObject } from 'react'

// Performance monitoring utilities
export const performanceMetrics = {
  // Mark performance milestones
  mark: (name: string) => {
    if (typeof performance !== 'undefined' && performance.mark) {
      performance.mark(name)
    }
  },

  // Measure time between marks
  measure: (name: string, startMark: string, endMark?: string) => {
    if (typeof performance !== 'undefined' && performance.measure) {
      try {
        performance.measure(name, startMark, endMark)
        const measure = performance.getEntriesByName(name)[0]
        return measure ? measure.duration : 0
      } catch (e) {
        console.warn('Performance measurement failed:', e)
        return 0
      }
    }
    return 0
  },

  // Get navigation timing metrics
  getNavigationMetrics: () => {
    if (typeof performance !== 'undefined' && performance.getEntriesByType) {
      const navigation = performance.getEntriesByType('navigation')[0] as PerformanceNavigationTiming
      if (navigation) {
        return {
          domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
          loadComplete: navigation.loadEventEnd - navigation.loadEventStart,
          firstPaint: performance.getEntriesByName('first-paint')[0]?.startTime || 0,
          firstContentfulPaint: performance.getEntriesByName('first-contentful-paint')[0]?.startTime || 0,
          totalPageLoad: navigation.loadEventEnd - navigation.fetchStart
        }
      }
    }
    return null
  },

  // Log performance metrics to console (development only)
  logMetrics: () => {
    if (process.env.NODE_ENV === 'development') {
      const metrics = performanceMetrics.getNavigationMetrics()
      if (metrics) {
        console.group('🚀 Performance Metrics')
        console.log('DOM Content Loaded:', `${metrics.domContentLoaded.toFixed(2)}ms`)
        console.log('Load Complete:', `${metrics.loadComplete.toFixed(2)}ms`) 
        console.log('First Paint:', `${metrics.firstPaint.toFixed(2)}ms`)
        console.log('First Contentful Paint:', `${metrics.firstContentfulPaint.toFixed(2)}ms`)
        console.log('Total Page Load:', `${metrics.totalPageLoad.toFixed(2)}ms`)
        console.groupEnd()
      }
    }
  }
}

// Lazy loading wrapper with error boundaries and loading states
interface LazyComponentOptions {
  fallback?: ReactNode
  delay?: number
}

const DefaultFallback = () => (
  <div className="flex items-center justify-center p-8">
    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
  </div>
)



export function createLazyComponent<T extends ComponentType<any>>(
  componentLoader: () => Promise<{ default: T }>,
  options: LazyComponentOptions = {}
): ComponentType<any> {
  const {
    fallback = <DefaultFallback />,
    delay = 0
  } = options

  // Add artificial delay in development to test loading states
  const loaderWithDelay = () => {
    const baseLoader = componentLoader()
    
    if (process.env.NODE_ENV === 'development' && delay > 0) {
      return Promise.all([
        baseLoader,
        new Promise(resolve => setTimeout(resolve, delay))
      ]).then(([module]) => module)
    }
    
    return baseLoader
  }

  const LazyComponent = lazy(loaderWithDelay)

  return function LazyWrapper(props: any) {
    return (
      <Suspense fallback={fallback}>
        <LazyComponent {...props} />
      </Suspense>
    )
  }
}

// Image lazy loading component
interface LazyImageProps {
  src: string
  alt: string
  className?: string
  placeholder?: string
  blurDataURL?: string
  onLoad?: () => void
  onError?: () => void
}

export function LazyImage({
  src,
  alt,
  className = '',
  placeholder = 'data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMjAwIiBoZWlnaHQ9IjIwMCIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj48cmVjdCB3aWR0aD0iMTAwJSIgaGVpZ2h0PSIxMDAlIiBmaWxsPSIjZjNmNGY2Ii8+PC9zdmc+',
  blurDataURL,
  onLoad,
  onError
}: LazyImageProps) {
  const [isLoading, setIsLoading] = useState(true)
  const [hasError, setHasError] = useState(false)
  const [imgSrc, setImgSrc] = useState(placeholder)

  useEffect(() => {
    const img = new Image()
    img.onload = () => {
      setImgSrc(src)
      setIsLoading(false)
      onLoad?.()
    }
    img.onerror = () => {
      setHasError(true)
      setIsLoading(false)
      onError?.()
    }
    img.src = src
  }, [src, onLoad, onError])

  return (
    <div className={`relative overflow-hidden ${className}`}>
      <img
        src={imgSrc}
        alt={alt}
        className={`w-full h-full object-cover transition-opacity duration-300 ${
          isLoading ? 'opacity-0' : 'opacity-100'
        }`}
        style={{
          filter: isLoading && blurDataURL ? 'blur(20px)' : 'none'
        }}
      />
      
      {isLoading && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-100">
          <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-gray-400"></div>
        </div>
      )}
      
      {hasError && (
        <div className="absolute inset-0 flex items-center justify-center bg-gray-100 text-gray-400 text-sm">
          Failed to load image
        </div>
      )}
    </div>
  )
}

// Intersection Observer hook for lazy loading
export function useIntersectionObserver<T extends HTMLElement = HTMLDivElement>(
  options: IntersectionObserverInit = {}
): [RefObject<T>, boolean] {
  const [isVisible, setIsVisible] = useState(false)
  const elementRef = useRef<T>(null)

  useEffect(() => {
    const element = elementRef.current
    if (!element) return

    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setIsVisible(true)
          observer.disconnect()
        }
      },
      {
        threshold: 0.1,
        rootMargin: '50px',
        ...options
      }
    )

    observer.observe(element)
    return () => observer.disconnect()
  }, [options])

  return [elementRef, isVisible]
}

// Virtual scrolling utilities for large lists
export function useVirtualScrolling(
  itemCount: number,
  itemHeight: number,
  containerHeight: number,
  overscan: number = 5
) {
  const [scrollTop, setScrollTop] = useState(0)

  const startIndex = Math.max(0, Math.floor(scrollTop / itemHeight) - overscan)
  const endIndex = Math.min(
    itemCount - 1,
    Math.ceil((scrollTop + containerHeight) / itemHeight) + overscan
  )

  const visibleItems = []
  for (let i = startIndex; i <= endIndex; i++) {
    visibleItems.push({
      index: i,
      offsetTop: i * itemHeight
    })
  }

  const totalHeight = itemCount * itemHeight

  return {
    visibleItems,
    totalHeight,
    startIndex,
    endIndex,
    setScrollTop
  }
}

// Memory usage monitoring
export const memoryMonitor = {
  getUsage: () => {
    if ('memory' in performance) {
      const memory = (performance as any).memory
      return {
        usedJSHeapSize: memory.usedJSHeapSize,
        totalJSHeapSize: memory.totalJSHeapSize,
        jsHeapSizeLimit: memory.jsHeapSizeLimit,
        usedPercentage: (memory.usedJSHeapSize / memory.jsHeapSizeLimit) * 100
      }
    }
    return null
  },

  logUsage: () => {
    if (process.env.NODE_ENV === 'development') {
      const usage = memoryMonitor.getUsage()
      if (usage) {
        console.log('🧠 Memory Usage:', {
          used: `${(usage.usedJSHeapSize / 1024 / 1024).toFixed(2)} MB`,
          total: `${(usage.totalJSHeapSize / 1024 / 1024).toFixed(2)} MB`,
          limit: `${(usage.jsHeapSizeLimit / 1024 / 1024).toFixed(2)} MB`,
          percentage: `${usage.usedPercentage.toFixed(1)}%`
        })
      }
    }
  }
}

// Bundle size analyzer (development only)
export const bundleAnalyzer = {
  analyzeChunks: () => {
    if (process.env.NODE_ENV === 'development') {
      // This would typically integrate with webpack-bundle-analyzer
      console.log('📦 Bundle Analysis: Use npm run build:analyze to see detailed bundle analysis')
    }
  },

  logLoadedChunks: () => {
    if (process.env.NODE_ENV === 'development' && 'webpackChunkName' in window) {
      console.log('📦 Loaded Chunks:', (window as any).webpackChunkName)
    }
  }
}