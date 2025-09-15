import { useEffect, useState } from 'react'
import { AdminApi, ResultListItem } from '../api/psyAdmin'
import dayjs from 'dayjs'

export default function ResultsList(){
  const [data,setData]=useState<ResultListItem[]>([])
  const [total,setTotal]=useState(0)
  const [page,setPage]=useState(1)
  const [pageSize,setPageSize]=useState(20)
  const [search,setSearch]=useState('')
  const [loading,setLoading]=useState(false)
  const pages=Math.ceil(total/pageSize)||1

  const load=async(p=page, ps=pageSize, q=search)=>{
    setLoading(true)
    try{
      const r=await AdminApi.results(p,ps,q)
      setData(r.data); setTotal(r.total)
    }finally{ setLoading(false) }
  }

  useEffect(()=>{ load(1,pageSize,search) },[])

  return (
    <div className="container py-6">
      <div className="flex items-center gap-2 mb-4">
        <input placeholder="ابحث بالرقم الوطني أو الاسم..." className="rounded-lg bg-white/10 border border-white/20 p-2 w-80"
           value={search} onChange={e=>setSearch(e.target.value)} />
        <button onClick={()=>load(1,pageSize,search)} className="rounded-lg bg-blue-600/90 hover:bg-blue-600 px-4 py-2">بحث</button>
      </div>

      <div className="overflow-x-auto rounded-2xl border border-white/10">
        <table className="min-w-full">
          <thead className="bg-white/5">
            <tr>
              <th className="p-3 text-right">#</th>
              <th className="p-3 text-right">الرقم الوطني</th>
              <th className="p-3 text-right">الاسم</th>
              <th className="p-3 text-right">الدرجة</th>
              <th className="p-3 text-right">التاريخ</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td className="p-4" colSpan={6}>جاري التحميل...</td></tr>
            ) : data.length===0 ? (
              <tr><td className="p-4" colSpan={6}>لا توجد نتائج</td></tr>
            ) : data.map((r)=>(
              <tr key={r.resultId} className="border-t border-white/10 hover:bg-white/5">
                <td className="p-3">{r.resultId}</td>
                <td className="p-3">{r.nationalId}</td>
                <td className="p-3">{r.fullName||'غير متوفر'}</td>
                <td className="p-3">{r.totalScore}</td>
                <td className="p-3">{dayjs(r.createdAt).format('YYYY/MM/DD HH:mm')}</td>
                <td className="p-3">
                  <a className="rounded-lg bg-emerald-600/90 hover:bg-emerald-600 px-3 py-1" href={`/results/${r.resultId}`}>عرض</a>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between mt-4">
        <div>إجمالي: {total}</div>
        <div className="flex items-center gap-2">
          <button disabled={page<=1} onClick={()=>{setPage(p=>p-1); load(page-1,pageSize,search)}} className="px-3 py-1 rounded bg-white/10 disabled:opacity-40">السابق</button>
          <div>{page} / {pages}</div>
          <button disabled={page>=pages} onClick={()=>{setPage(p=>p+1); load(page+1,pageSize,search)}} className="px-3 py-1 rounded bg-white/10 disabled:opacity-40">التالي</button>
        </div>
      </div>
    </div>
  )
}

