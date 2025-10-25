import { useNavigate } from 'react-router-dom';
import { useEffect, useState, useCallback } from 'react';
import { motion } from 'framer-motion';
import { Button } from '../components/ui/button';
import { CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Badge } from '../components/ui/badge';
import { 
  CheckCircle, 
  Home, 
  FileText, 
  Copy, 
  Clock, 
  User, 
  Calendar,
  Hash,
  Loader2,
  Download,
  Share2
} from 'lucide-react';
import UserHeader from '../components/layout/UserHeader';
import ScreenContainer from "../components/layout/ScreenContainer";
import SectionCard from "../components/layout/SectionCard";
import { useUserStore } from '../store/user';
import { sessionStorage } from '../lib/session';
import { ToastContainer, addToast } from '../components/ui/toast';
import { apiFetchJson } from '../lib/api';
import { normalizeReportStatusResponse } from '@/lib/contract-bridge';

interface ReportStatus {
  isReady: boolean;
  resultId?: string;
  isLoading: boolean;
  error?: string;
}

export default function ThankYou() {
  const navigate = useNavigate();
  const { nationalId, sessionId } = useUserStore();
  const [reportStatus, setReportStatus] = useState<ReportStatus>({ isReady: false, isLoading: false });
  const [, setSessionData] = useState<any>(null);
  const [pollCount, setPollCount] = useState(0);

  // Get session data
  useEffect(() => {
    const session = sessionStorage.get();
    setSessionData(session);
  }, []);

  // Poll for report readiness
  const pollReportStatus = useCallback(async () => {
    if (!sessionId || pollCount >= 5) return;
    
    setReportStatus(prev => ({ ...prev, isLoading: true }));
    
    try {
      const rawResponse = await apiFetchJson(`/sessions/${sessionId}/report-status`);
      const response = normalizeReportStatusResponse(rawResponse);
      
      if (response.isReady) {
        setReportStatus({
          isReady: true,
          resultId: response.resultId,
          isLoading: false
        });
      } else {
        setPollCount(prev => prev + 1);
        if (pollCount < 4) {
          setTimeout(() => pollReportStatus(), 3000);
        } else {
          setReportStatus({
            isReady: false,
            isLoading: false,
            error: 'سيصل التقرير إلى لوحة الإدارة قريبًا'
          });
        }
      }
    } catch (error) {
      setPollCount(prev => prev + 1);
      if (pollCount < 4) {
        setTimeout(() => pollReportStatus(), 3000);
      } else {
        setReportStatus({
          isReady: false,
          isLoading: false,
          error: 'سيصل التقرير إلى لوحة الإدارة قريبًا'
        });
      }
    }
  }, [sessionId, pollCount]);

  useEffect(() => {
    if (sessionId) {
      setTimeout(() => {
        pollReportStatus();
      }, 2000);
    }
  }, [sessionId]);

  const handleGoHome = () => {
    navigate('/');
  };

  const handleViewReport = () => {
    if (reportStatus.isReady && reportStatus.resultId) {
      window.open(`/admin/results/${reportStatus.resultId}/pdf`, '_blank');
    }
  };

  const copyToClipboard = async (text: string, successMessage: string) => {
    try {
      await navigator.clipboard.writeText(text);
      addToast(successMessage, 'success');
    } catch (error) {
      addToast('فشل في النسخ', 'error');
    }
  };

  const copySessionId = () => {
    if (sessionId) {
      copyToClipboard(sessionId, 'تم نسخ رقم الجلسة');
    }
  };

  const copyNationalId = () => {
    if (nationalId) {
      copyToClipboard(nationalId, 'تم نسخ الرقم الوطني');
    }
  };

  const formatSessionId = (id: string) => {
    if (!id) return '';
    return id.toUpperCase().replace(/(.{4})/g, '$1 ').trim();
  };

  const formatDate = (date: Date) => {
    return new Intl.DateTimeFormat('ar-SA', {
      weekday: 'long',
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: 'numeric',
      minute: 'numeric',
    }).format(date);
  };

  return (
    <div className="min-h-screen bg-gradient-to-b from-green-50 to-white">
      <ToastContainer />
      
      <UserHeader 
        showStep={true}
        stepLabel="اكتمل الاختبار"
      />

      <ScreenContainer maxWidth="2xl" className="py-6 sm:py-8 lg:py-12">
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5 }}
          className="space-y-6 sm:space-y-8"
        >
          {/* Success Hero Card */}
          <SectionCard className="bg-gradient-to-br from-emerald-50 to-green-50 border-emerald-200 text-center">
            <CardHeader className="pb-4 sm:pb-6">
              <motion.div
                initial={{ scale: 0, rotate: 0 }}
                animate={{ 
                  scale: [0, 1, 1.2, 1],
                  rotate: [0, 180, 360]
                }}
                transition={{
                  duration: 1.2,
                  times: [0, 0.3, 0.8, 1],
                  ease: [0.4, 0, 0.2, 1]
                }}
                className="flex justify-center mb-4 sm:mb-6"
              >
                <div className="relative">
                  <div className="bg-emerald-100 p-4 sm:p-6 rounded-full">
                    <CheckCircle className="h-12 w-12 sm:h-16 sm:w-16 text-emerald-600" />
                  </div>
                  {/* Confetti dots */}
                  <motion.div
                    initial={{ scale: 0, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    transition={{ delay: 0.5, duration: 0.3 }}
                    className="absolute -top-2 -right-2 w-3 h-3 sm:w-4 sm:h-4 bg-yellow-400 rounded-full"
                  />
                  <motion.div
                    initial={{ scale: 0, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    transition={{ delay: 0.7, duration: 0.3 }}
                    className="absolute -bottom-1 -left-3 w-2 h-2 sm:w-3 sm:h-3 bg-pink-400 rounded-full"
                  />
                  <motion.div
                    initial={{ scale: 0, opacity: 0 }}
                    animate={{ scale: 1, opacity: 1 }}
                    transition={{ delay: 0.9, duration: 0.3 }}
                    className="absolute top-1 -left-2 w-2 h-2 sm:w-3 sm:h-3 bg-blue-400 rounded-full"
                  />
                </div>
              </motion.div>
              
              <CardTitle className="text-[clamp(20px,2.4vw,28px)] font-bold text-emerald-900 mb-2 sm:mb-3">
                تم إرسال إجاباتك بنجاح
              </CardTitle>
              <p className="text-emerald-700 leading-relaxed text-sm sm:text-base px-4 sm:px-0">
                تم حفظ إجاباتك وسيتم توليد التقرير خلال لحظات. يمكنك الرجوع لاحقًا باستخدام رقم الجلسة.
              </p>
            </CardHeader>
          </SectionCard>

          {/* Session Details */}
          <SectionCard>
            <CardHeader className="pb-4 sm:pb-6">
              <CardTitle className="text-[clamp(18px,2vw,22px)] flex items-center gap-2 sm:gap-3">
                <Hash className="h-5 w-5 sm:h-6 sm:w-6" />
                تفاصيل الجلسة
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4 sm:space-y-6">
              {/* Details Grid - Responsive */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 sm:gap-6">
                {/* Session ID */}
                <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-3 sm:p-4 bg-slate-50 rounded-xl gap-2 sm:gap-3">
                  <div className="flex items-center gap-2 sm:gap-3 min-w-0">
                    <Hash className="h-4 w-4 sm:h-5 sm:w-5 text-muted-foreground flex-shrink-0" />
                    <span className="text-sm sm:text-base font-medium">رقم الجلسة</span>
                  </div>
                  <div className="flex items-center gap-2 w-full sm:w-auto">
                    <Badge variant="outline" className="font-mono text-xs sm:text-sm flex-1 sm:flex-none justify-center">
                      {formatSessionId(sessionId || '')}
                    </Badge>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={copySessionId}
                      className="h-8 w-8 sm:h-9 sm:w-9 p-0 flex-shrink-0 min-w-[32px] min-h-[32px]"
                      aria-label="نسخ رقم الجلسة"
                    >
                      <Copy className="h-3 w-3 sm:h-4 sm:w-4" />
                    </Button>
                  </div>
                </div>

                {/* National ID */}
                <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-3 sm:p-4 bg-slate-50 rounded-xl gap-2 sm:gap-3">
                  <div className="flex items-center gap-2 sm:gap-3 min-w-0">
                    <User className="h-4 w-4 sm:h-5 sm:w-5 text-muted-foreground flex-shrink-0" />
                    <span className="text-sm sm:text-base font-medium">الرقم الوطني</span>
                  </div>
                  <div className="flex items-center gap-2 w-full sm:w-auto">
                    <Badge variant="outline" className="font-mono text-xs sm:text-sm flex-1 sm:flex-none justify-center">
                      {nationalId || ''}
                    </Badge>
                    <Button
                      size="sm"
                      variant="ghost"
                      onClick={copyNationalId}
                      className="h-8 w-8 sm:h-9 sm:w-9 p-0 flex-shrink-0 min-w-[32px] min-h-[32px]"
                      aria-label="نسخ الرقم الوطني"
                    >
                      <Copy className="h-3 w-3 sm:h-4 sm:w-4" />
                    </Button>
                  </div>
                </div>

                {/* Date & Time */}
                <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-3 sm:p-4 bg-slate-50 rounded-xl gap-2 sm:gap-3 md:col-span-2">
                  <div className="flex items-center gap-2 sm:gap-3 min-w-0">
                    <Calendar className="h-4 w-4 sm:h-5 sm:w-5 text-muted-foreground flex-shrink-0" />
                    <span className="text-sm sm:text-base font-medium">تاريخ الإنجاز</span>
                  </div>
                  <Badge variant="outline" className="text-xs sm:text-sm">
                    {formatDate(new Date())}
                  </Badge>
                </div>
              </div>
            </CardContent>
          </SectionCard>

          {/* Report Status */}
          <SectionCard>
            <CardHeader className="pb-4 sm:pb-6">
              <CardTitle className="text-[clamp(18px,2vw,22px)] flex items-center gap-2 sm:gap-3">
                <FileText className="h-5 w-5 sm:h-6 sm:w-6" />
                حالة التقرير
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4 sm:space-y-6">
              {reportStatus.isLoading && (
                <div className="flex items-center gap-3 p-4 sm:p-6 bg-blue-50 rounded-xl">
                  <Loader2 className="h-5 w-5 sm:h-6 sm:w-6 animate-spin text-blue-600 flex-shrink-0" />
                  <div>
                    <p className="text-sm sm:text-base font-medium text-blue-900">
                      جاري إعداد التقرير...
                    </p>
                    <p className="text-xs sm:text-sm text-blue-700">
                      يرجى الانتظار لحظات قليلة
                    </p>
                  </div>
                </div>
              )}

              {reportStatus.isReady && (
                <div className="space-y-4">
                  <div className="flex items-center gap-3 p-4 sm:p-6 bg-green-50 rounded-xl">
                    <CheckCircle className="h-5 w-5 sm:h-6 sm:w-6 text-green-600 flex-shrink-0" />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm sm:text-base font-medium text-green-900">
                        التقرير جاهز للعرض
                      </p>
                      <p className="text-xs sm:text-sm text-green-700">
                        يمكنك الآن عرض وتحميل تقريرك الشخصي
                      </p>
                    </div>
                  </div>
                  
                  <div className="flex flex-col sm:flex-row gap-3">
                    <Button
                      onClick={handleViewReport}
                      className="flex-1 sm:flex-none gap-2 h-11 sm:h-12 min-h-[44px]"
                    >
                      <FileText className="h-4 w-4 sm:h-5 sm:w-5" />
                      عرض التقرير
                    </Button>
                  </div>
                </div>
              )}

              {reportStatus.error && (
                <div className="flex items-center gap-3 p-4 sm:p-6 bg-amber-50 rounded-xl">
                  <Clock className="h-5 w-5 sm:h-6 sm:w-6 text-amber-600 flex-shrink-0" />
                  <div>
                    <p className="text-sm sm:text-base font-medium text-amber-900">
                      التقرير قيد المعالجة
                    </p>
                    <p className="text-xs sm:text-sm text-amber-700">
                      {reportStatus.error}
                    </p>
                  </div>
                </div>
              )}
            </CardContent>
          </SectionCard>

          {/* Action Buttons */}
          <SectionCard className="bg-slate-50/50">
            <CardContent className="text-center space-y-4 sm:space-y-6">
              <div className="flex flex-col sm:flex-row gap-3 sm:gap-4">
                <Button
                  onClick={handleGoHome}
                  variant="outline"
                  className="flex-1 sm:flex-none gap-2 h-11 sm:h-12 min-h-[44px]"
                >
                  <Home className="h-4 w-4 sm:h-5 sm:w-5" />
                  العودة للرئيسية
                </Button>
                
                <Button
                  onClick={() => window.print()}
                  variant="outline"
                  className="flex-1 sm:flex-none gap-2 h-11 sm:h-12 min-h-[44px]"
                >
                  <Download className="h-4 w-4 sm:h-5 sm:w-5" />
                  طباعة الصفحة
                </Button>
                
                <Button
                  onClick={() => {
                    if (navigator.share) {
                      navigator.share({
                        title: 'اكتمل الاختبار النفسي',
                        text: `تم إنجاز الاختبار بنجاح. رقم الجلسة: ${sessionId}`,
                      });
                    }
                  }}
                  variant="outline"
                  className="flex-1 sm:flex-none gap-2 h-11 sm:h-12 min-h-[44px]"
                >
                  <Share2 className="h-4 w-4 sm:h-5 sm:w-5" />
                  مشاركة
                </Button>
              </div>
              
              <p className="text-xs sm:text-sm text-muted-foreground leading-relaxed max-w-2xl mx-auto">
                يرجى الاحتفاظ برقم الجلسة للرجوع إليه عند الحاجة. في حال وجود أي استفسارات، يرجى التواصل مع فريق الدعم.
              </p>
            </CardContent>
          </SectionCard>
        </motion.div>
      </ScreenContainer>
    </div>
  );
}