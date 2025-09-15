import { useAuth } from '../../store/auth'

export default function TopBar(){
  const { setToken } = useAuth()
  return (
    <header className="w-full border-b border-white/10 bg-[#0f182a]/80 backdrop-blur">
      <div className="container flex items-center justify-between py-3">
        <div className="text-xl font-bold">{import.meta.env.VITE_APP_NAME}</div>
        <nav className="flex items-center gap-3">
          <a className="hover:underline" href="/results">النتائج</a>
          <a className="hover:underline" href="/audit">السجل</a>
          <button className="rounded-lg px-3 py-1 bg-red-600/80 hover:bg-red-600"
            onClick={()=>{ setToken(null); location.href='/login' }}>
            خروج
          </button>
        </nav>
      </div>
    </header>
  )
}

