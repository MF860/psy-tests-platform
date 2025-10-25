import { CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { CheckCircle, Home, FileText, Calendar } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { strings, formatSessionCode, formatTimestamp } from "../lib/strings";
import UserHeader from "../components/layout/UserHeader";
import ScreenContainer from "../components/layout/ScreenContainer";
import SectionCard from "../components/layout/SectionCard";
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
    <>
      <UserHeader showStep stepLabel="اكتمل الاختبار" />
      <main className="min-h-screen bg-gradient-to-b from-green-50 to-white">
        <ScreenContainer maxWidth="lg" className="section-spacing">
          <div className="space-2xl">
            {/* Success Hero Section */}
            <div className="text-center space-lg">
              <div className="flex justify-center">
                <div className="w-20 h-20 sm:w-24 sm:h-24 bg-gradient-to-br from-green-400 to-green-600 rounded-full flex items-center justify-center shadow-lg">
                  <CheckCircle className="h-10 w-10 sm:h-12 sm:w-12 text-white" />
                </div>
              </div>
              
              <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-green-800 leading-tight">
                {strings.finish.title}
              </h1>
              <p className="text-lg sm:text-xl text-green-700 max-w-2xl mx-auto leading-relaxed">
                {strings.finish.subtitle}
              </p>
            </div>

            {/* Success Message Card */}
            <SectionCard className="border-green-200 bg-gradient-to-br from-green-50 to-white">
              <CardContent className="text-center space-md">
                <p className="text-base text-green-800 leading-relaxed font-medium">
                  {strings.finish.message}
                </p>
              </CardContent>
            </SectionCard>

            {/* Session Information */}
            <SectionCard>
              <CardHeader className="space-sm">
                <CardTitle className="text-lg font-semibold text-slate-900 flex items-center space-xs">
                  <FileText className="h-5 w-5 text-primary" />
                  <span>تفاصيل الجلسة</span>
                </CardTitle>
              </CardHeader>
              
              <CardContent className="space-md">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-md">
                  <div className="space-sm p-4 bg-muted/30 rounded-xl">
                    <div className="flex items-center space-xs text-muted-foreground">
                      <FileText className="h-4 w-4" />
                      <span className="text-sm font-medium">رقم الجلسة</span>
                    </div>
                    <p className="text-sm font-mono text-slate-900 mt-1">
                      {formatSessionCode(sessionCode)}
                    </p>
                  </div>
                  
                  <div className="space-sm p-4 bg-muted/30 rounded-xl">
                    <div className="flex items-center space-xs text-muted-foreground">
                      <Calendar className="h-4 w-4" />
                      <span className="text-sm font-medium">تاريخ الإكمال</span>
                    </div>
                    <p className="text-sm text-slate-900 mt-1">
                      {formatTimestamp(completionTime)}
                    </p>
                  </div>
                </div>
              </CardContent>
            </SectionCard>

            {/* Action Buttons */}
            <div className="space-lg">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-md max-w-md mx-auto">
                <Button 
                  onClick={handleGoHome}
                  size="lg"
                  className="touch-target space-xs bg-primary hover:bg-primary/90 transition-colors duration-200"
                >
                  <Home className="h-5 w-5" />
                  <span>{strings.finish.actions.home}</span>
                </Button>
                
                <Button 
                  onClick={handleViewReports}
                  variant="outline"
                  size="lg" 
                  className="touch-target space-xs border-primary text-primary hover:bg-primary hover:text-white transition-colors duration-200"
                >
                  <FileText className="h-5 w-5" />
                  <span>{strings.finish.actions.reports}</span>
                </Button>
              </div>

              {/* Additional Info */}
              <SectionCard variant="ghost" className="bg-slate-50 border-slate-200">
                <CardContent className="text-center space-sm">
                  <p className="text-sm text-muted-foreground leading-relaxed">
                    يرجى الاحتفاظ برقم الجلسة للرجوع إليه عند الحاجة.
                  </p>
                  <p className="text-xs text-muted-foreground">
                    في حالة وجود أي استفسارات، يرجى التواصل مع فريق الدعم الفني.
                  </p>
                </CardContent>
              </SectionCard>
            </div>
          </div>
        </ScreenContainer>
      </main>
    </>
  );
}

export default function Finish() {
  return (
    <GuardedRoute page="thank-you">
      <FinishContent />
    </GuardedRoute>
  );
}