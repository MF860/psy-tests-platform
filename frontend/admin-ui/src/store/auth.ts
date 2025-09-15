import { create } from 'zustand'

type AuthState = {
  token: string | null
  setToken: (t:string|null)=>void
  isAuthed: ()=>boolean
}

export const useAuth = create<AuthState>((set, get) => ({
  token: localStorage.getItem('admin_token'),
  setToken: (t) => {
    if (t) localStorage.setItem('admin_token', t)
    else localStorage.removeItem('admin_token')
    set({ token: t })
  },
  isAuthed: () => !!get().token
}))

