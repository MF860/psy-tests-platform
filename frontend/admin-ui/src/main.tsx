import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'
import './index.css'
import { ToastProvider } from './components/ui/use-toast'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { Toaster } from 'sonner'
import { ThemeProvider } from './components/theme-provider'
import { performanceMetrics, memoryMonitor } from './lib/performance'
import { PerformanceMonitor } from './components/dev/PerformanceMonitor'

// Mark critical timing points
performanceMetrics.mark('main-start')

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      refetchOnWindowFocus: true,
      retry: 1,
    },
  },
})

// Performance monitoring setup
const root = ReactDOM.createRoot(document.getElementById('root')!)

// Mark render start
performanceMetrics.mark('render-start')

root.render(
  <React.StrictMode>
    <ThemeProvider defaultTheme="light" storageKey="admin-ui-theme">
      <QueryClientProvider client={queryClient}>
        <ToastProvider>
          <App />
          <Toaster richColors position="top-center" dir="rtl" />
          <PerformanceMonitor />
        </ToastProvider>
      </QueryClientProvider>
    </ThemeProvider>
  </React.StrictMode>,
)

// Log performance metrics after initial render (development only)
if (process.env.NODE_ENV === 'development') {
  setTimeout(() => {
    performanceMetrics.mark('initial-render-complete')
    performanceMetrics.measure('app-to-render', 'app-start', 'render-start')
    performanceMetrics.measure('initial-render-time', 'render-start', 'initial-render-complete')
    
    // Log all metrics
    performanceMetrics.logMetrics()
    memoryMonitor.logUsage()
    
    console.log('🎯 Admin UI Performance Baseline Established')
  }, 100)
}
