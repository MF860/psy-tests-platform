import { useEffect, useState } from 'react'
import { AdminApi, AuditItem } from '../api/psyAdmin'
import dayjs from 'dayjs'

export default function AuditList(){
  const [data,setData]=useState<AuditItem[]>([])
  const [total,setTotal]=useState(0)
  const [page,setPage]=useState(1)
  const [pageSize,setPageSize]=useState(25)
  const [search,setSearch]=useState('')
  const [from,setFrom]=useState('')
  const [to,setTo]=useState('')
  const [loading,setLoading]=useState(false)
  const pages=Math.ceil(total/pageSize)||1

  const load=async(p=page, ps=pageSize)=>{
    setLoading(true)
    try{
      const r=await AdminApi.audit(p,ps,search,from||undefined,to||undefined)
      setData(r.data); setTotal(r.total)
    }finally{ setLoading(false) }
  }

  useEffect(()=>{ load(1,pageSize) },[])

  return (
    <div className="container py-6">
      <div className="flex flex-wrap items-end gap-3 mb-4">
        <div>
          <label className="block mb-1">بحث (action أو المستخدم)</label>
          <input className="rounded-lg bg-white/10 border border-white/20 p-2" value={search} onChange={e=>setSearch(e.target.value)} />
        </div>
        <div>
          <label className="block mb-1">من</label>
          <input type="date" className="rounded-lg bg-white/10 border border-white/20 p-2" value={from} onChange={e=>setFrom(e.target.value)} />
        </div>
        <div>
          <label className="block mb-1">إلى</label>
          <input type="date" className="rounded-lg bg-white/10 border border-white/20 p-2" value={to} onChange={e=>setTo(e.target.value)} />
        </div>
        <button onClick={()=>load(1,pageSize)} className="rounded-lg bg-blue-600/90 hover:bg-blue-600 px-4 py-2">تحديث</button>
      </div>

      <div className="overflow-x-auto rounded-2xl border border-white/10">
        <table className="min-w-full">
          <thead className="bg-white/5">
            <tr>
              <th className="p-3 text-right">#</th>
              <th className="p-3 text-right">العملية</th>
              <th className="p-3 text-right">المستخدم</th>
              <th className="p-3 text-right">IP</th>
              <th className="p-3 text-right">التفاصيل</th>
              <th className="p-3 text-right">التاريخ</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td className="p-4" colSpan={6}>جاري التحميل...</td></tr>
            ) : data.length===0 ? (
              <tr><td className="p-4" colSpan={6}>لا توجد سجلات</td></tr>
            ) : data.map((a)=>(
              <tr key={a.id} className="border-t border-white/10">
                <td className="p-3">{a.id}</td>
                <td className="p-3">{a.action}</td>
                <td className="p-3">{a.adminUsername}</td>
                <td className="p-3">{a.ipAddress || '-'}</td>
                <td className="p-3">{a.details || '-'}</td>
                <td className="p-3">{dayjs(a.createdAt).format('YYYY/MM/DD HH:mm')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between mt-4">
        <div>إجمالي: {total}</div>
        <div className="flex items-center gap-2">
          <button disabled={page<=1} onClick={()=>{setPage(p=>p-1); load(page-1,pageSize)}} className="px-3 py-1 rounded bg-white/10 disabled:opacity-40">السابق</button>
          <div>{page} / {pages}</div>
          <button disabled={page>=pages} onClick={()=>{setPage(p=>p+1); load(page+1,pageSize)}} className="px-3 py-1 rounded bg-white/10 disabled:opacity-40">التالي</button>
        </div>
      </div>
    </div>
  )
}

