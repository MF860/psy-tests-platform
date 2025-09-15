import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { GuardedRoute } from './components/shared/GuardedRoute'
import TopBar from './components/layout/TopBar'
import Login from './pages/Login'
import ResultsList from './pages/ResultsList'
import ResultDetail from './pages/ResultDetail'
import AuditList from './pages/AuditList'
import NotFound from './pages/NotFound'

export default function App(){
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login/>} />
        <Route path="/" element={<Navigate to="/results" replace />} />
        <Route path="/results" element={
          <GuardedRoute>
            <div>
              <TopBar/><ResultsList/>
            </div>
          </GuardedRoute>
        } />
        <Route path="/results/:id" element={
          <GuardedRoute>
            <div>
              <TopBar/><ResultDetail/>
            </div>
          </GuardedRoute>
        } />
        <Route path="/audit" element={
          <GuardedRoute>
            <div>
              <TopBar/><AuditList/>
            </div>
          </GuardedRoute>
        } />
        <Route path="*" element={<NotFound/>} />
      </Routes>
    </BrowserRouter>
  )
}

