import { useState, useEffect } from 'react'
import { performanceMetrics, memoryMonitor } from '../../lib/performance'

interface PerformanceMetrics {
  domContentLoaded: number
  loadComplete: number
  firstPaint: number
  firstContentfulPaint: number
  totalPageLoad: number
}

interface MemoryUsage {
  usedJSHeapSize: number
  totalJSHeapSize: number
  jsHeapSizeLimit: number
  usedPercentage: number
}

export function PerformanceMonitor() {
  const [metrics, setMetrics] = useState<PerformanceMetrics | null>(null)
  const [memory, setMemory] = useState<MemoryUsage | null>(null)
  const [isVisible, setIsVisible] = useState(false)

  useEffect(() => {
    // Only show in development
    if (process.env.NODE_ENV !== 'development') return

    // Collect metrics after page load
    const timer = setTimeout(() => {
      const navMetrics = performanceMetrics.getNavigationMetrics()
      const memoryUsage = memoryMonitor.getUsage()
      
      setMetrics(navMetrics)
      setMemory(memoryUsage)
    }, 1000)

    return () => clearTimeout(timer)
  }, [])

  // Don't render in production
  if (process.env.NODE_ENV !== 'development') return null

  if (!isVisible) {
    return (
      <button
        onClick={() => setIsVisible(true)}
        className="fixed bottom-4 right-4 bg-blue-600 text-white px-3 py-2 rounded-lg text-xs font-medium shadow-lg hover:bg-blue-700 transition-colors z-50"
        title="عرض مقاييس الأداء"
      >
        📊 الأداء
      </button>
    )
  }

  return (
    <div className="fixed bottom-4 right-4 bg-white border border-gray-200 rounded-lg shadow-lg p-4 max-w-sm z-50 text-sm" dir="rtl">
      <div className="flex items-center justify-between mb-3">
        <h3 className="font-semibold text-gray-900">مقاييس الأداء</h3>
        <button
          onClick={() => setIsVisible(false)}
          className="text-gray-400 hover:text-gray-600"
          title="إخفاء"
        >
          ✕
        </button>
      </div>

      {metrics && (
        <div className="space-y-2 mb-4">
          <h4 className="font-medium text-gray-800">أوقات التحميل</h4>
          <div className="space-y-1 text-xs">
            <div className="flex justify-between">
              <span className="text-gray-600">تحميل المحتوى:</span>
              <span className={`font-mono ${metrics.domContentLoaded > 1000 ? 'text-red-600' : 'text-green-600'}`}>
                {metrics.domContentLoaded.toFixed(0)}ms
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">أول رسم:</span>
              <span className={`font-mono ${metrics.firstPaint > 1500 ? 'text-red-600' : 'text-green-600'}`}>
                {metrics.firstPaint.toFixed(0)}ms
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">أول محتوى:</span>
              <span className={`font-mono ${metrics.firstContentfulPaint > 1800 ? 'text-red-600' : 'text-green-600'}`}>
                {metrics.firstContentfulPaint.toFixed(0)}ms
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">إجمالي التحميل:</span>
              <span className={`font-mono ${metrics.totalPageLoad > 3000 ? 'text-red-600' : 'text-green-600'}`}>
                {metrics.totalPageLoad.toFixed(0)}ms
              </span>
            </div>
          </div>
        </div>
      )}

      {memory && (
        <div className="space-y-2">
          <h4 className="font-medium text-gray-800">استخدام الذاكرة</h4>
          <div className="space-y-1 text-xs">
            <div className="flex justify-between">
              <span className="text-gray-600">الذاكرة المستخدمة:</span>
              <span className="font-mono">
                {(memory.usedJSHeapSize / 1024 / 1024).toFixed(1)} MB
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">إجمالي الذاكرة:</span>
              <span className="font-mono">
                {(memory.totalJSHeapSize / 1024 / 1024).toFixed(1)} MB
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-600">نسبة الاستخدام:</span>
              <span className={`font-mono ${memory.usedPercentage > 70 ? 'text-red-600' : 'text-green-600'}`}>
                {memory.usedPercentage.toFixed(1)}%
              </span>
            </div>
          </div>
          
          {/* Memory usage bar */}
          <div className="mt-2">
            <div className="w-full bg-gray-200 rounded-full h-2">
              <div
                className={`h-2 rounded-full ${
                  memory.usedPercentage > 70 ? 'bg-red-500' : 
                  memory.usedPercentage > 50 ? 'bg-yellow-500' : 'bg-green-500'
                }`}
                style={{ width: `${Math.min(memory.usedPercentage, 100)}%` }}
              />
            </div>
          </div>
        </div>
      )}

      <div className="mt-3 pt-3 border-t border-gray-100">
        <button
          onClick={() => {
            performanceMetrics.logMetrics()
            memoryMonitor.logUsage()
          }}
          className="w-full bg-gray-100 hover:bg-gray-200 text-gray-700 text-xs py-1 px-2 rounded transition-colors"
        >
          عرض في وحدة التحكم
        </button>
      </div>
    </div>
  )
}