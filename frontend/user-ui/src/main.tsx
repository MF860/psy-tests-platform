import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import './styles/globals.css'
import './index.css'
import { Toaster } from 'sonner'
import { ThemeProvider } from './components/theme-provider'
import { performanceMetrics, memoryMonitor } from './lib/performance'
import { PerformanceMonitor } from './components/dev/PerformanceMonitor'

// Mark critical timing points
performanceMetrics.mark('main-start')

// Performance monitoring setup
const root = ReactDOM.createRoot(document.getElementById('root')!)

// Mark render start
performanceMetrics.mark('render-start')

root.render(
  <React.StrictMode>
    <ThemeProvider defaultTheme="light" storageKey="user-ui-theme">
      <App />
      <Toaster richColors position="top-center" dir="rtl" />
      <PerformanceMonitor />
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
    
    console.log('🎯 User UI Performance Baseline Established')
  }, 100)
}