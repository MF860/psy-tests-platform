import { useState, useEffect } from 'react'
import { AdminApi, type AuditItem } from '../lib/apiAdmin'
import dayjs from 'dayjs'
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card'
import { Input } from '../components/ui/input'
import { Button } from '../components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../components/ui/table'

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
    <div className="container mx-auto p-6 space-y-4">
      <Card>
        <CardHeader>
          <CardTitle>سجل العمليات</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap items-end gap-3 mb-4">
            <div>
              <div className="mb-1 text-sm">بحث (action أو المستخدم)</div>
              <Input value={search} onChange={e=>setSearch(e.target.value)} />
            </div>
            <div>
              <div className="mb-1 text-sm">من</div>
              <Input type="date" value={from} onChange={e=>setFrom(e.target.value)} />
            </div>
            <div>
              <div className="mb-1 text-sm">إلى</div>
              <Input type="date" value={to} onChange={e=>setTo(e.target.value)} />
            </div>
            <Button onClick={()=>load(1,pageSize)}>تحديث</Button>
          </div>

          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>#</TableHead>
                  <TableHead>العملية</TableHead>
                  <TableHead>المستخدم</TableHead>
                  <TableHead>IP</TableHead>
                  <TableHead>التفاصيل</TableHead>
                  <TableHead>التاريخ</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {loading ? (
                  <TableRow><TableCell colSpan={6}>جاري التحميل...</TableCell></TableRow>
                ) : data.length===0 ? (
                  <TableRow><TableCell colSpan={6}>لا توجد سجلات</TableCell></TableRow>
                ) : data.map((a)=>(
                  <TableRow key={a.id}>
                    <TableCell>{a.id}</TableCell>
                    <TableCell>{a.action}</TableCell>
                    <TableCell>{a.adminUsername}</TableCell>
                    <TableCell>{a.ipAddress || '-'}</TableCell>
                    <TableCell>{a.details || '-'}</TableCell>
                    <TableCell>{dayjs(a.createdAt).format('YYYY/MM/DD HH:mm')}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          <div className="flex items-center justify-between mt-4">
            <div>إجمالي: {total}</div>
            <div className="flex items-center gap-2">
              <Button variant="outline" disabled={page<=1} onClick={()=>{setPage(p=>p-1); load(page-1,pageSize)}}>السابق</Button>
              <div>{page} / {pages}</div>
              <Button variant="outline" disabled={page>=pages} onClick={()=>{setPage(p=>p+1); load(page+1,pageSize)}}>التالي</Button>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
