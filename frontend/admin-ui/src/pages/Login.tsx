import { useState } from 'react'
import { AdminApi } from '../lib/apiAdmin'
import { useAuth } from '../store/auth'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Label } from '../components/ui/label'
import { Input } from '../components/ui/input'
import { Button } from '../components/ui/button'

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
      location.href='/dashboard'
    }catch(err:any){
      setError(err?.response?.data?.error || 'فشل تسجيل الدخول')
    }finally{ setLoading(false) }
  }

  return (
    <div className="min-h-screen grid place-items-center p-6">
      <Card className="w-full max-w-md">
        <CardHeader>
          <CardTitle className="text-center">تسجيل الدخول</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={submit} className="space-y-4">
            {error && <div className="text-red-600">{error}</div>}
            <div>
              <Label className="mb-1 block">اسم المستخدم</Label>
              <Input value={username} onChange={e=>setUsername(e.target.value)} />
            </div>
            <div>
              <Label className="mb-1 block">كلمة المرور</Label>
              <Input type="password" value={password} onChange={e=>setPassword(e.target.value)} />
            </div>
            <Button disabled={loading} className="w-full">{loading ? 'جاري الدخول...' : 'دخول'}</Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
