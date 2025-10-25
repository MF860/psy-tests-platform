import { useState, useEffect } from 'react'
import { AdminApi } from '../lib/apiAdmin'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Badge } from '@/components/ui/badge'

interface ValidationResult {
  itemId: number
  type: string
  messages: string[]
}

interface ValidationResponse {
  ok: boolean
  count: number
  validCount: number
  violations: ValidationResult[]
}

interface FixResponse {
  totalQuestions: number
  fixedCount: number
  details: FixedQuestion[]
}

interface FixedQuestion {
  itemId: number
  textAr: string
  originalType: string
  newType: string
  originalOptions: string[]
  newOptions: string[]
}

export default function QuestionValidationPage() {
  const [data, setData] = useState<ValidationResponse | null>(null)
  const [fixData, setFixData] = useState<FixResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [fixing, setFixing] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const validateQuestions = async () => {
    setLoading(true)
    setError(null)
    try {
      const response = await AdminApi.validateQuestions()
      setData(response)
    } catch (err) {
      setError('فشل التحقق من صحة الأسئلة')
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  const fixQuestions = async () => {
    setFixing(true)
    setError(null)
    try {
      const response = await AdminApi.fixQuestions()
      setFixData(response)
      // After fixing, re-validate to show updated status
      await validateQuestions()
    } catch (err) {
      setError('فشل إصلاح الأسئلة')
      console.error(err)
    } finally {
      setFixing(false)
    }
  }

  useEffect(() => {
    validateQuestions()
  }, [])

  return (
    <div className="container mx-auto p-6 space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold">فحص الأسئلة</h1>
          <p className="text-gray-600">التحقق من صحة الأسئلة في قاعدة البيانات</p>
        </div>
        <div className="flex space-x-2 space-x-reverse">
          <Button onClick={validateQuestions} disabled={loading || fixing}>
            {loading ? 'جاري الفحص...' : 'إعادة الفحص'}
          </Button>
          <Button 
            onClick={fixQuestions} 
            disabled={fixing || loading || !data || data.violations.length === 0}
            variant="destructive"
          >
            {fixing ? 'جاري الإصلاح...' : 'إصلاح الأسئلة'}
          </Button>
        </div>
      </div>

      {error && (
        <Card className="bg-red-50">
          <CardContent className="pt-6">
            <p className="text-red-700">{error}</p>
          </CardContent>
        </Card>
      )}

      {data && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium">إجمالي الأسئلة</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{data.count}</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium">الأسئلة الصالحة</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-green-600">{data.count - data.violations.length}</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm font-medium">الأسئلة غير الصالحة</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold text-red-600">{data.violations.length}</div>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>تفاصيل الفحص</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>رقم السؤال</TableHead>
                      <TableHead>النوع</TableHead>
                      <TableHead>الحالة</TableHead>
                      <TableHead>الأخطاء</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.violations.map((item) => (
                      <TableRow key={item.itemId}>
                        <TableCell>{item.itemId}</TableCell>
                        <TableCell>{item.type}</TableCell>
                        <TableCell>
                          <Badge variant="destructive">غير صالح</Badge>
                        </TableCell>
                        <TableCell>
                          <ul className="list-disc pr-5">
                            {item.messages.map((error, index) => (
                              <li key={index} className="text-red-600">{error}</li>
                            ))}
                          </ul>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
          
          {fixData && (
            <Card className="bg-green-50">
              <CardHeader>
                <CardTitle className="text-green-800">نتائج الإصلاح</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                  <div>
                    <p className="text-sm text-gray-600">إجمالي الأسئلة</p>
                    <p className="text-xl font-bold">{fixData.totalQuestions}</p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-600">الأسئلة التي تم إصلاحها</p>
                    <p className="text-xl font-bold text-green-600">{fixData.fixedCount}</p>
                  </div>
                </div>
                
                {fixData.fixedCount > 0 && (
                  <div className="mt-4">
                    <h4 className="font-medium mb-2">تفاصيل الأسئلة التي تم إصلاحها:</h4>
                    <div className="overflow-x-auto">
                      <Table>
                        <TableHeader>
                          <TableRow>
                            <TableHead>رقم السؤال</TableHead>
                            <TableHead>النص</TableHead>
                            <TableHead>النوع الأصلي</TableHead>
                            <TableHead>النوع الجديد</TableHead>
                          </TableRow>
                        </TableHeader>
                        <TableBody>
                          {fixData.details.map((item) => (
                            <TableRow key={item.itemId}>
                              <TableCell>{item.itemId}</TableCell>
                              <TableCell className="max-w-xs truncate">{item.textAr}</TableCell>
                              <TableCell>{item.originalType}</TableCell>
                              <TableCell className="font-medium text-green-600">{item.newType}</TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </div>
                  </div>
                )}
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  )
}
