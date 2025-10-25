import { useAuth } from '../../store/auth'
import { Link } from 'react-router-dom'

export default function TopBar(){
  const { setToken } = useAuth()
  return (
    <header className="w-full border-b border-white/10 bg-[#0f182a]/80 backdrop-blur">
      <div className="container flex items-center justify-between py-3">
        <div className="text-xl font-bold"><Link to="/results">{import.meta.env.VITE_APP_NAME}</Link></div>
        <nav className="flex items-center gap-3">
          <Link className="hover:underline" to="/results">النتائج</Link>
          <Link className="hover:underline" to="/audit">السجل</Link>
          <button className="rounded-lg px-3 py-1 bg-red-600/80 hover:bg-red-600"
            onClick={()=>{ setToken(null); location.href='/login' }}>
            خروج
          </button>
        </nav>
      </div>
    </header>
  )
}
