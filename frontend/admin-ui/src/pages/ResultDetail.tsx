import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { AdminApi, ResultDetail } from '../api/psyAdmin'
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer } from 'recharts'
import dayjs from 'dayjs'

export default function ResultDetail(){
  const { id } = useParams()
  const [data,setData]=useState<ResultDetail|undefined>()
  const [loading,setLoading]=useState(false)
  const rid = Number(id)

  useEffect(()=>{
    const load=async()=>{
      setLoading(true)
      try{ setData(await AdminApi.resultDetail(rid)) }
      finally{ setLoading(false) }
    }
    load()
  },[rid])

  const downloadPdf = async ()=>{
    const blob = await AdminApi.resultPdf(rid)
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url; a.download = `result_${rid}.pdf`; a.click()
    URL.revokeObjectURL(url)
  }

  if (loading || !data) return <div className="container py-6">جاري التحميل...</div>

  return (
    <div className="container py-6">
      <div className="mb-4">
        <h1 className="text-2xl font-bold">تفاصيل النتيجة #{data.resultId}</h1>
        <div className="text-white/70 mt-1">
          {data.fullName || 'غير متوفر'} — {data.nationalId} — {dayjs(data.createdAt).format('YYYY/MM/DD HH:mm')}
        </div>
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <div className="rounded-2xl border border-white/10 p-4">
          <div className="font-bold mb-2">الدرجة الكلية</div>
          <div className="text-4xl">{data.totalScore}</div>
          <div className="text-white/60 mt-2">نموذج التحليل: {data.scoringModelVersion}</div>
          <button onClick={downloadPdf} className="mt-4 rounded-lg bg-indigo-600/90 hover:bg-indigo-600 px-4 py-2">تنزيل تقرير PDF</button>
        </div>

        <div className="rounded-2xl border border-white/10 p-4">
          <div className="font-bold mb-3">مقاييس الأبعاد</div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data.dimensions}>
                <XAxis dataKey="dimension" tick={{ fill:'#e6edf3' }} />
                <YAxis tick={{ fill:'#e6edf3' }} />
                <Tooltip />
                <Bar dataKey="t" />
              </BarChart>
            </ResponsiveContainer>
          </div>
          <div className="text-white/60 mt-2">الأعمدة تعرض T-Score لكل بُعد</div>
        </div>
      </div>

      <div className="rounded-2xl border border-white/10 p-4 mt-6 overflow-x-auto">
        <table className="min-w-full">
          <thead className="bg-white/5">
            <tr>
              <th className="p-3 text-right">البعد</th>
              <th className="p-3 text-right">Raw</th>
              <th className="p-3 text-right">Z</th>
              <th className="p-3 text-right">T</th>
              <th className="p-3 text-right">Percentile</th>
            </tr>
          </thead>
          <tbody>
            {data.dimensions.map((d)=>(
              <tr key={d.dimension} className="border-t border-white/10">
                <td className="p-3">{d.dimension}</td>
                <td className="p-3">{d.raw.toFixed(2)}</td>
                <td className="p-3">{d.z.toFixed(2)}</td>
                <td className="p-3">{d.t.toFixed(1)}</td>
                <td className="p-3">{d.percentile.toFixed(0)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

