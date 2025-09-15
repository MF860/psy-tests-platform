import { useState } from 'react'
import { AdminApi } from '../api/psyAdmin'
import { useAuth } from '../store/auth'

export default function Login(){
  const [username,setUsername]=useState('root')
  const [password,setPassword]=useState('')
  const [loading,setLoading]=useState(false)
  const [error,setError]=useState<string|undefined>()
  const { setToken } = useAuth()

  const submit=async(e:React.FormEvent)=>{
    e.preventDefault()
    setLoading(true); setError(undefined)
    try{
      const { token } = await AdminApi.login(username,password)
      setToken(token)
      location.href='/results'
    }catch(err:any){
      setError(err?.response?.data?.error || 'فشل تسجيل الدخول')
    }finally{ setLoading(false) }
  }

  return (
    <div className="min-h-screen grid place-items-center">
      <form onSubmit={submit} className="w-full max-w-md bg-white/5 rounded-2xl p-6 border border-white/10">
        <h1 className="text-2xl font-bold mb-4 text-center">تسجيل الدخول</h1>
        {error && <div className="mb-3 text-red-400">{error}</div>}
        <label className="block mb-2">اسم المستخدم</label>
        <input className="w-full mb-4 rounded-lg bg-white/10 border border-white/20 p-2" value={username} onChange={e=>setUsername(e.target.value)} />
        <label className="block mb-2">كلمة المرور</label>
        <input type="password" className="w-full mb-6 rounded-lg bg-white/10 border border-white/20 p-2" value={password} onChange={e=>setPassword(e.target.value)} />
        <button disabled={loading} className="w-full rounded-lg bg-blue-600/90 hover:bg-blue-600 py-2 font-bold">
          {loading ? 'جاري الدخول...' : 'دخول'}
        </button>
      </form>
    </div>
  )
}

