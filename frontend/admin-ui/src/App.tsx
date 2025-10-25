import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Suspense } from 'react'
import { GuardedRoute } from './components/shared/GuardedRoute'
import { AdminLayout } from './components/admin/AdminLayout'
import DemoBadge from './components/DemoBadge'
import { createLazyComponent, performanceMetrics } from './lib/performance'

// Performance monitoring - mark app start
performanceMetrics.mark('app-start')

// Always-loaded components (critical path)
import Login from './pages/Login'
import NotFound from './pages/NotFound'

// Lazy-loaded page components with custom loading states
const Dashboard = createLazyComponent(
  () => import('./pages/Dashboard'),
  { 
    fallback: <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
        <p className="text-gray-600">Loading Dashboard...</p>
      </div>
    </div>
  }
)

const ResultsList = createLazyComponent(
  () => import('./pages/ResultsList'),
  {
    fallback: <div className="flex items-center justify-center p-8">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3"></div>
        <p className="text-gray-600">Loading Results...</p>
      </div>
    </div>
  }
)

const ResultDetailPage = createLazyComponent(
  () => import('./pages/ResultDetail'),
  {
    fallback: <div className="flex items-center justify-center p-8">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3"></div>
        <p className="text-gray-600">Loading Result Details...</p>
      </div>
    </div>
  }
)

const QuestionValidationPage = createLazyComponent(
  () => import('./pages/QuestionValidation'),
  {
    fallback: <div className="flex items-center justify-center p-8">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3"></div>
        <p className="text-gray-600">Loading Question Validation...</p>
      </div>
    </div>
  }
)

const AuditList = createLazyComponent(
  () => import('./pages/AuditList'),
  {
    fallback: <div className="flex items-center justify-center p-8">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3"></div>
        <p className="text-gray-600">Loading Audit Log...</p>
      </div>
    </div>
  }
)

const Settings = createLazyComponent(
  () => import('./pages/Settings'),
  {
    fallback: <div className="flex items-center justify-center p-8">
      <div className="text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto mb-3"></div>
        <p className="text-gray-600">Loading Settings...</p>
      </div>
    </div>
  }
)

export default function App(){
  return (
    <BrowserRouter>
      <DemoBadge />
      <Routes>
        <Route path="/login" element={<Login/>} />
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        
        {/* All admin pages use AdminLayout wrapper */}
        <Route path="/dashboard" element={
          <GuardedRoute>
            <AdminLayout>
              <Dashboard />
            </AdminLayout>
          </GuardedRoute>
        } />
        
        <Route path="/results" element={
          <GuardedRoute>
            <AdminLayout>
              <ResultsList />
            </AdminLayout>
          </GuardedRoute>
        } />
        <Route path="/results/:id" element={
          <GuardedRoute>
            <AdminLayout>
              <ResultDetailPage />
            </AdminLayout>
          </GuardedRoute>
        } />
        <Route path="/questions" element={
          <GuardedRoute>
            <AdminLayout>
              <QuestionValidationPage />
            </AdminLayout>
          </GuardedRoute>
        } />
        <Route path="/audit" element={
          <GuardedRoute>
            <AdminLayout>
              <AuditList />
            </AdminLayout>
          </GuardedRoute>
        } />
        <Route path="/settings" element={
          <GuardedRoute>
            <AdminLayout>
              <Settings />
            </AdminLayout>
          </GuardedRoute>
        } />
        <Route path="*" element={<NotFound/>} />
      </Routes>
    </BrowserRouter>
  )
}

