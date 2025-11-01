import { CheckCircle, Home, FileText, Calendar } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { strings, formatSessionCode, formatTimestamp } from "../lib/strings";
import { toast } from "sonner";

function FinishContent() {
  const { sessionId, clearSession } = useFlowState();
  const sessionCode = sessionId || 'غير متوفر';
  const completionTime = new Date().toISOString();

  const handleGoHome = () => {
    // Clear all flow state and restart
    clearSession();
  };

  const handleViewReports = () => {
    // This could navigate to a reports page or external portal
    // For now, just show a toast
    toast.info('ستتم إضافة هذه الميزة قريباً');
  };

  return (
    <div className="relative min-h-screen w-screen overflow-y-auto bg-gradient-to-br from-gray-900 via-green-900 to-gray-900">
      {/* Animated background particles - Green theme */}
      <div className="absolute inset-0 opacity-20">
        <div className="absolute top-20 left-20 w-72 h-72 bg-green-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse"></div>
        <div className="absolute top-40 right-20 w-72 h-72 bg-emerald-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-2000"></div>
        <div className="absolute bottom-20 left-1/3 w-72 h-72 bg-teal-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-4000"></div>
      </div>

      {/* Header with logos - Responsive */}
      <header className="absolute top-0 w-full flex justify-between items-center p-4 sm:p-6 md:p-8 z-10" dir="rtl">
        <div className="flex-1 hidden sm:block"></div>
        <div className="flex-1 flex justify-center">
          <img 
            src="/STEST.png" 
            alt="شعار المنصة" 
            className="h-12 sm:h-14 md:h-16 object-contain drop-shadow-lg"
          />
        </div>
        <div className="flex-1 flex justify-end items-center gap-2 sm:gap-4">
          <div className="px-2.5 sm:px-3 md:px-4 py-1.5 sm:py-2 bg-green-500/20 backdrop-blur-sm border border-green-400/30 rounded-full text-green-300 text-xs sm:text-sm font-medium">
            اكتمل الاختبار
          </div>
          <img 
            src="/FB-ICON.png" 
            alt="أيقونة" 
            className="h-8 sm:h-10 md:h-12 object-contain drop-shadow-lg"
          />
        </div>
      </header>

      {/* Main content - Responsive */}
      <main className="relative z-10 pt-24 sm:pt-28 md:pt-32 pb-8 sm:pb-10 md:pb-12 px-3 sm:px-4">
        <div className="max-w-3xl mx-auto space-y-6 sm:space-y-8">
          {/* Success Hero Section - Responsive */}
          <div className="text-center space-y-4 sm:space-y-6">
            <div className="flex justify-center">
              <div className="w-20 h-20 sm:w-24 sm:h-24 bg-gradient-to-br from-green-400 to-green-600 rounded-full flex items-center justify-center shadow-2xl animate-pulse">
                <CheckCircle className="h-11 w-11 sm:h-14 sm:w-14 text-white" />
              </div>
            </div>
            
            <h1 className="text-2xl sm:text-3xl md:text-4xl font-bold text-green-300 leading-tight px-2" dir="rtl">
              {strings.finish.title}
            </h1>
            <p className="text-base sm:text-lg md:text-xl text-green-200 max-w-2xl mx-auto leading-relaxed px-2" dir="rtl">
              {strings.finish.subtitle}
            </p>
          </div>

          {/* Success Message Card - Responsive */}
          <div className="bg-gradient-to-r from-green-500/20 to-emerald-500/20 backdrop-blur-sm rounded-xl p-4 sm:p-5 md:p-6 border border-green-400/30">
            <p className="text-center text-white leading-relaxed font-medium text-sm sm:text-base" dir="rtl">
              {strings.finish.message}
            </p>
          </div>

          {/* Session Information - Responsive */}
          <div className="bg-white/10 backdrop-blur-lg rounded-xl sm:rounded-2xl border border-white/20 overflow-hidden">
            <div className="p-4 sm:p-5 md:p-6 border-b border-white/20">
              <h2 className="text-base sm:text-lg font-semibold text-white flex items-center gap-2" dir="rtl">
                <FileText className="h-4 w-4 sm:h-5 sm:w-5 text-green-400" />
                <span>تفاصيل الجلسة</span>
              </h2>
            </div>
            
            <div className="p-4 sm:p-5 md:p-6">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6">
                <div className="bg-white/5 backdrop-blur-sm rounded-lg p-3 sm:p-4 border border-white/10">
                  <div className="flex items-center gap-2 text-gray-400 mb-2" dir="rtl">
                    <FileText className="h-3.5 w-3.5 sm:h-4 sm:w-4" />
                    <span className="text-xs sm:text-sm font-medium">رقم الجلسة</span>
                  </div>
                  <p className="text-xs sm:text-sm font-mono text-white break-all">
                    {formatSessionCode(sessionCode)}
                  </p>
                </div>
                
                <div className="bg-white/5 backdrop-blur-sm rounded-lg p-3 sm:p-4 border border-white/10">
                  <div className="flex items-center gap-2 text-gray-400 mb-2" dir="rtl">
                    <Calendar className="h-3.5 w-3.5 sm:h-4 sm:w-4" />
                    <span className="text-xs sm:text-sm font-medium">تاريخ الإكمال</span>
                  </div>
                  <p className="text-xs sm:text-sm text-white">
                    {formatTimestamp(completionTime)}
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Action Buttons - Responsive */}
          <div className="space-y-4 sm:space-y-6">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 sm:gap-4 max-w-md mx-auto">
              <button 
                onClick={handleGoHome}
                className="bg-gradient-to-r from-green-500 to-emerald-400 text-white font-bold py-2.5 sm:py-3 px-4 sm:px-6 rounded-lg shadow-lg hover:scale-105 hover:shadow-2xl transition-all duration-300 flex items-center justify-center gap-2 text-sm sm:text-base"
              >
                <Home className="h-4 w-4 sm:h-5 sm:w-5" />
                <span>{strings.finish.actions.home}</span>
              </button>
              
              <button 
                onClick={handleViewReports}
                className="bg-white/10 backdrop-blur-sm border border-white/30 text-white font-bold py-2.5 sm:py-3 px-4 sm:px-6 rounded-lg hover:bg-white/20 transition-all duration-300 flex items-center justify-center gap-2 text-sm sm:text-base"
              >
                <FileText className="h-4 w-4 sm:h-5 sm:w-5" />
                <span>{strings.finish.actions.reports}</span>
              </button>
            </div>

            {/* Additional Info - Responsive */}
            <div className="bg-white/5 backdrop-blur-sm rounded-lg p-4 sm:p-5 md:p-6 border border-white/10">
              <div className="text-center space-y-2">
                <p className="text-xs sm:text-sm text-gray-300 leading-relaxed" dir="rtl">
                  يرجى الاحتفاظ برقم الجلسة للرجوع إليه عند الحاجة.
                </p>
                <p className="text-[10px] sm:text-xs text-gray-400" dir="rtl">
                  في حالة وجود أي استفسارات، يرجى التواصل مع فريق الدعم الفني.
                </p>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default function Finish() {
  return (
    <GuardedRoute page="thank-you">
      <FinishContent />
    </GuardedRoute>
  );
}