import { useParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { AdminApi } from '../lib/apiAdmin';
import { HorizontalBarChart } from '../components/charts/HorizontalBarChart';
import { SdjTrackCard } from '../components/SdjTrackCard';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '../components/ui/accordion';
import { Badge } from '../components/ui/badge';
import { Button } from '../components/ui/button';
import { ArrowRight, Download, Loader2 } from 'lucide-react';

export default function ResultDetailSDJ() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        const result = await AdminApi.resultDetail(Number(id));
        setData(result);
        setError(null);
      } catch (err: any) {
        console.error('Failed to load result:', err);
        setError(err.message || 'فشل في تحميل النتيجة');
      } finally {
        setLoading(false);
      }
    };
    
    if (id) {
      load();
    }
  }, [id]);

  const handleDownloadPdf = async () => {
    try {
      const blob = await AdminApi.resultPdf(Number(id));
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `result_${id}_sdj.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Failed to download PDF:', err);
      alert('فشل في تحميل PDF');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <Loader2 className="h-8 w-8 animate-spin text-primary" />
      </div>
    );
  }

  if (error || !data || !data.sdjData) {
    return (
      <div className="container mx-auto px-4 py-6">
        <Card className="border-red-200 bg-red-50">
          <CardContent className="py-6">
            <p className="text-red-700" dir="rtl">
              {error || 'لا توجد بيانات SDJ لهذه النتيجة'}
            </p>
            <Button variant="outline" onClick={() => navigate('/results')} className="mt-4">
              <ArrowRight className="h-4 w-4 ml-2" />
              العودة إلى القائمة
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  const { sdjData } = data;

  // Prepare chart data
  const chartData = sdjData.Dimensions.map((d: any) => ({
    name: d.Dimension,
    value: d.T,
    band: d.Band
  }));

  // Get top 3 strengths and weaknesses
  const sortedSubDims = [...sdjData.SubDimensions].sort((a, b) => b.T - a.T);
  const topStrengths = sortedSubDims.slice(0, 3);
  const topWeaknesses = sortedSubDims.slice(-3).reverse();

  return (
    <div className="container mx-auto px-4 py-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div dir="rtl">
          <h1 className="text-2xl font-bold text-slate-900">نتائج إطار التنمية المستدامة (SDJ)</h1>
          <p className="text-sm text-slate-600 mt-1">
            {data.fullName || data.nationalId} • {new Date(data.createdAt).toLocaleDateString('ar-EG')}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => navigate('/results')}>
            <ArrowRight className="h-4 w-4 ml-2" />
            رجوع
          </Button>
          <Button onClick={handleDownloadPdf}>
            <Download className="h-4 w-4 ml-2" />
            تحميل PDF
          </Button>
        </div>
      </div>

      {/* Top 3 Strengths and Weaknesses */}
      <div className="grid md:grid-cols-2 gap-6">
        <Card className="border-green-200 bg-green-50/50">
          <CardHeader>
            <CardTitle className="text-green-900" dir="rtl">أعلى 3 نقاط قوة</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {topStrengths.map((sub: any, idx: number) => (
              <div key={idx} className="flex items-center justify-between p-3 bg-white rounded-lg border border-green-200" dir="rtl">
                <span className="font-medium text-slate-900">{sub.SubDimension}</span>
                <Badge className="bg-green-100 text-green-800 border-green-300">
                  T: {sub.T.toFixed(1)}
                </Badge>
              </div>
            ))}
          </CardContent>
        </Card>

        <Card className="border-red-200 bg-red-50/50">
          <CardHeader>
            <CardTitle className="text-red-900" dir="rtl">أهم 3 مجالات للتطوير</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {topWeaknesses.map((sub: any, idx: number) => (
              <div key={idx} className="flex items-center justify-between p-3 bg-white rounded-lg border border-red-200" dir="rtl">
                <span className="font-medium text-slate-900">{sub.SubDimension}</span>
                <Badge className="bg-red-100 text-red-800 border-red-300">
                  T: {sub.T.toFixed(1)}
                </Badge>
              </div>
            ))}
          </CardContent>
        </Card>
      </div>

      {/* SDJ Dimension Tree */}
      <Card>
        <CardHeader>
          <CardTitle dir="rtl">شجرة الأبعاد - التنمية المستدامة</CardTitle>
        </CardHeader>
        <CardContent>
          <Accordion type="multiple" className="w-full">
            {sdjData.Dimensions.map((dim: any, idx: number) => {
              const subDims = sdjData.SubDimensions.filter(
                (sub: any) => sub.Dimension === dim.Dimension
              );
              
              const bandColor = 
                dim.Band === 'Excellent' ? 'text-green-700 bg-green-50' :
                dim.Band === 'Weak' ? 'text-red-700 bg-red-50' :
                'text-amber-700 bg-amber-50';
              
              return (
                <AccordionItem key={idx} value={`dim-${idx}`} className="border-b border-slate-200">
                  <AccordionTrigger className="text-lg font-semibold hover:no-underline py-4">
                    <div className="flex items-center gap-3 w-full" dir="rtl">
                      <span className="flex-1 text-right">{dim.Dimension}</span>
                      <Badge className={`${bandColor} px-2 py-1 text-sm`}>
                        T: {dim.T.toFixed(1)} • {dim.Band}
                      </Badge>
                    </div>
                  </AccordionTrigger>
                  <AccordionContent>
                    <div className="space-y-2 pr-6" dir="rtl">
                      {subDims.map((sub: any, subIdx: number) => {
                        const subBandColor = 
                          sub.Band === 'Excellent' ? 'bg-green-100 text-green-800 border-green-300' :
                          sub.Band === 'Weak' ? 'bg-red-100 text-red-800 border-red-300' :
                          'bg-amber-100 text-amber-800 border-amber-300';
                        
                        return (
                          <div key={subIdx} className="flex items-center justify-between p-3 bg-slate-50 rounded-lg hover:bg-slate-100 transition-colors">
                            <span className="font-medium text-slate-900">{sub.SubDimension}</span>
                            <div className="flex items-center gap-2">
                              <span className="text-sm text-slate-600">T: {sub.T.toFixed(1)}</span>
                              <Badge className={subBandColor}>
                                {sub.Band}
                              </Badge>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </AccordionContent>
                </AccordionItem>
              );
            })}
          </Accordion>
        </CardContent>
      </Card>

      {/* Horizontal Bar Chart */}
      <Card>
        <CardHeader>
          <CardTitle dir="rtl">توزيع الدرجات (T-Scores) - ترتيب تصاعدي</CardTitle>
        </CardHeader>
        <CardContent>
          <HorizontalBarChart data={chartData} />
        </CardContent>
      </Card>

      {/* SDJ Tracks */}
      <div>
        <h2 className="text-xl font-bold text-slate-900 mb-4" dir="rtl">المسارات المهنية الموصى بها</h2>
        <div className="grid md:grid-cols-3 gap-6">
          {sdjData.TrackFits.map((track: any, idx: number) => (
            <SdjTrackCard key={idx} track={track} />
          ))}
        </div>
      </div>
    </div>
  );
}
