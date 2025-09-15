import { Navigate } from 'react-router-dom'
import { useAuth } from '../../store/auth'

export function GuardedRoute({ children }: { children: JSX.Element }) {
  const { isAuthed } = useAuth()
  return isAuthed() ? children : <Navigate to="/login" replace />
}

