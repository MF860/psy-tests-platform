import { create } from 'zustand'

type AuthState = {
  token: string | null
  setToken: (t:string|null)=>void
  isAuthed: ()=>boolean
}

// Helper function to check if JWT token is expired
const isTokenExpired = (token: string): boolean => {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    const currentTime = Date.now() / 1000
    return payload.exp < currentTime
  } catch {
    return true // Invalid token format
  }
}

// Clear expired token on initial load
const initialToken = localStorage.getItem('admin_token')
if (initialToken && isTokenExpired(initialToken)) {
  console.log('[Auth] Clearing expired token from localStorage')
  localStorage.removeItem('admin_token')
}

export const useAuth = create<AuthState>((set, get) => ({
  token: initialToken && !isTokenExpired(initialToken) ? initialToken : null,
  setToken: (t) => {
    if (t) {
      // Validate token before storing
      if (isTokenExpired(t)) {
        console.warn('[Auth] Attempt to store expired token')
        localStorage.removeItem('admin_token')
        set({ token: null })
        return
      }
      localStorage.setItem('admin_token', t)
    } else {
      localStorage.removeItem('admin_token')
    }
    set({ token: t })
  },
  isAuthed: () => {
    const token = get().token
    return !!(token && !isTokenExpired(token))
  }
}))

