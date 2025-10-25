import { Navigate } from 'react-router-dom'
import { useAuth } from '../../store/auth'
import { useEffect, useState } from 'react'
import { Loader2 } from 'lucide-react'

export function GuardedRoute({ children }: { children: JSX.Element }) {
  const { isAuthed, token } = useAuth()
  const [ready, setReady] = useState(false)
  useEffect(()=>{ 
    setReady(true)
    console.log('GuardedRoute: token =', token)
    console.log('GuardedRoute: isAuthed =', isAuthed())
  },[token, isAuthed])
  if (!ready) {
    return (
      <div className="min-h-screen grid place-items-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    )
  }
  return isAuthed() ? children : <Navigate to="/login" replace />
}
