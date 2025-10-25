import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useUserStore } from '../store/user';
import { apiFetchJson, ApiError } from '../lib/api';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Checkbox } from '../components/ui/checkbox';
import { Label } from '../components/ui/label';

export default function PrivacyPolicy() {
  const [agreed, setAgreed] = useState(false);
  const [isStarting, setIsStarting] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const navigate = useNavigate();
  const { nationalId, setSessionId, setTotalQuestions } = useUserStore();

  const handleContinue = async () => {
    if (isStarting) return;
    if (!nationalId) { navigate('/'); return; }
    if (!agreed) return;

    setIsStarting(true);
    setErrorMessage(null);

    try {
      interface SessionResponse {
        sessionId: string;
        totalQuestions: number;
      }

      const response = await apiFetchJson<SessionResponse>('/sessions/start', {
        method: 'POST',
        body: JSON.stringify({ nationalId }),
      });

      console.log('API Response:', JSON.stringify(response, null, 2));

      if (!response.sessionId) {
        console.error('Session ID not found in response:', response);
        setErrorMessage('حدث خطأ في الجلسة. يرجى المحاولة مرة أخرى.');
        return;
      }

      setSessionId(response.sessionId);
      setTotalQuestions(response.totalQuestions ?? 0);
      navigate('/exam');
    } catch (error) {
      if (error instanceof ApiError) setErrorMessage(error.message);
      else setErrorMessage('حدث خطأ غير متوقع.');
    } finally {
      setIsStarting(false);
    }
  };

  if (!nationalId) { navigate('/'); return null; }

  return (
    <div className="flex items-center justify-center min-h-screen bg-background p-4">
      <Card className="w-full max-w-2xl h-[80vh] flex flex-col">
        <CardHeader>
          <CardTitle className="text-xl text-center">سياسة الخصوصية</CardTitle>
        </CardHeader>
        <CardContent className="flex-grow flex flex-col">
          <div className="mb-4 flex-grow overflow-y-auto p-4 border rounded-md bg-muted/50 space-y-4 leading-8">
            <h3 className="font-bold">مقدمة</h3>
            <p>
              نحن نلتزم بحماية خصوصيتك وبياناتك الشخصية. هذه السياسة توضح كيفية جمع واستخدام ومشاركة معلوماتك الشخصية. نحن نستخدم هذه البيانات لتقديم أفضل تجربة لك في استخدام منصتنا.
            </p>
            <h3 className="font-bold">جمع البيانات</h3>
            <p>
              نقوم بجمع المعلومات الشخصية اللازمة لإجراء الاختبار النفسي وتحليل النتائج. تشمل هذه المعلومات الرقم الوطني والبيانات الديموغرافية الأساسية التي تساعدنا في تقديم تقييم دقيق.
            </p>
            <h3 className="font-bold">اتصل بنا</h3>
            <p>
              إذا كان لديك أي أسئلة أو استفسارات بخصوص سياسة الخصوصية، يمكنك التواصل معنا عبر البريد الإلكتروني التالي: privacy@example.com
            </p>
          </div>

          <div className="space-y-4">
            <div className="flex items-center space-x-2 space-x-reverse">
              <Checkbox id="agreement" checked={agreed} onCheckedChange={(checked) => setAgreed(checked as boolean)} />
              <Label htmlFor="agreement" className="text-sm">
                أوافق على سياسة الخصوصية
              </Label>
            </div>

            <Button onClick={handleContinue} disabled={!agreed || isStarting} className="w-full">
              {isStarting ? 'جاري بدء الاختبار...' : 'بدء الاختبار'}
            </Button>
            {errorMessage && (
              <p className="text-destructive text-sm text-center">{errorMessage}</p>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
