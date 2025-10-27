import { useState } from "react";
import { CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2, Shield, Clock, FileText } from "lucide-react";
import UserHeader from "@/components/layout/UserHeader";
import ScreenContainer from "@/components/layout/ScreenContainer";
import SectionCard from "@/components/layout/SectionCard";
import { GuardedRoute } from "@/components/GuardedRoute";
import { ApiError } from "@/lib/api";
import { toEnglishDigits, isValidNationalId } from "@/lib/numerals";
import { useFlowState } from "@/lib/useFlowState";
import { startSession } from "@/lib/api-client";

function LoginContent() {
  const { setSession } = useFlowState();
  const [value, setValue] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const onChange = (inputValue: string) => {
    const clean = toEnglishDigits(inputValue).slice(0, 10);
    setValue(clean);
    if (error) setError(null);
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isValidNationalId(value)) {
      setError("الرقم الوطني غير صالح — يجب أن يتكوّن من 10 أرقام.");
      return;
    }
    setBusy(true);
    try {
      // Clear any existing session data before starting a new one
      const { clearSession } = useFlowState.getState();
      clearSession();
      
      // Use the new API client with contract validation
      const response = await startSession(value);
      
      // Update flow state - GuardedRoute will handle navigation
      setSession({
        nationalId: value,
        sessionId: response.sessionId,
        resume: response.resume,
        totalQuestions: response.totalQuestions
      });
      
      // Set additional flags if resuming
      if (response.resume) {
        const { setConsented, setInstructionsCompleted } = useFlowState.getState();
        setConsented(true);
        setInstructionsCompleted(true);
      }
      
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        // User not found - create session without backend sessionId
        setSession({
          nationalId: value,
          sessionId: "", // Empty sessionId for offline mode
        });
      } else {
        setError("تعذّر تسجيل الدخول. الرجاء المحاولة لاحقًا.");
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <UserHeader showStep stepLabel="تسجيل الدخول" />
      <main className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
        <ScreenContainer maxWidth="lg" className="section-spacing">
          <div className="space-2xl">
            {/* Welcome Section */}
            <div className="text-center space-lg">
              <h1 className="text-2xl sm:text-3xl lg:text-4xl font-bold text-slate-900 leading-tight">
                مرحباً بك في منصة الاختبارات القياسية
              </h1>
              <p className="text-base sm:text-lg text-muted-foreground max-w-2xl mx-auto leading-relaxed">
                أدخل الرقم الوطني للبدء في الاختبار النفسي المخصص لك
              </p>
            </div>

          {/* Main Login Card */}
          <SectionCard className="w-full max-w-md mx-auto shadow-lg">
            <CardHeader className="text-center space-md">
              <CardTitle className="text-xl sm:text-2xl font-bold text-slate-900">
                تسجيل الدخول
              </CardTitle>
              <p className="text-sm sm:text-base text-muted-foreground">
                ادخل الرقم الوطني للمتابعة
              </p>
            </CardHeader>
            <CardContent className="space-lg">
              <form onSubmit={onSubmit} className="space-lg">
                <div className="space-sm">
                  <Label 
                    htmlFor="nid" 
                    className="text-right text-sm sm:text-base font-medium text-slate-700"
                  >
                    الرقم الوطني <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="nid"
                    type="tel"
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    placeholder="أدخل الرقم الوطني (10 أرقام)"
                    inputMode="numeric"
                    pattern="[0-9]*"
                    dir="ltr"
                    maxLength={10}
                    className={`
                      touch-target text-center font-mono text-base sm:text-lg
                      transition-colors duration-200
                      ${error ? "border-destructive focus-visible:ring-destructive" : "border-input focus-visible:ring-primary"}
                    `}
                    disabled={busy}
                    aria-invalid={!!error}
                    aria-describedby={error ? "nid-error" : "nid-help"}
                    autoComplete="off"
                  />
                  {!error && (
                    <p id="nid-help" className="text-xs sm:text-sm text-muted-foreground text-right">
                      الرجاء التأكد من إدخال 10 أرقام صحيحة.
                    </p>
                  )}
                  {error && (
                    <p 
                      id="nid-error" 
                      className="text-xs sm:text-sm text-destructive text-right font-medium"
                      role="alert"
                    >
                      {error}
                    </p>
                  )}
                </div>
                <Button 
                  type="submit" 
                  className="w-full touch-target text-base sm:text-lg font-semibold bg-primary hover:bg-primary/90 transition-colors duration-200" 
                  disabled={busy || value.length !== 10}
                  aria-describedby="submit-help"
                >
                  {busy ? (
                    <>
                      <Loader2 className="me-2 h-4 w-4 sm:h-5 sm:w-5 animate-spin" />
                      جاري التحقق…
                    </>
                  ) : (
                    "متابعة"
                  )}
                </Button>
              </form>

           
    {process.env.NODE_ENV === 'development' && (
                <div className="mt-4 sm:mt-6 p-3 sm:p-4 bg-muted/50 rounded-xl text-xs sm:text-sm text-muted-foreground text-center">
                  <strong>وضع التطوير:</strong> ادخل الرقم الخاص بك للاستمرار.
                </div>
              )}
            </CardContent>
          </SectionCard>

          {/* Feature Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-md max-w-4xl mx-auto">
            <SectionCard 
              variant="ghost" 
              className="bg-gradient-to-br from-blue-50 to-blue-100/50 border-blue-200/60 text-center space-sm hover:shadow-md transition-shadow duration-200"
            >
              <Shield className="w-8 h-8 sm:w-10 sm:h-10 mx-auto text-blue-600" />
              <h3 className="text-sm sm:text-base font-semibold text-blue-900">
                آمن ومحمي
              </h3>
              <p className="text-xs sm:text-sm text-blue-700/90 leading-relaxed">
                بياناتك محمية بأعلى معايير الأمان والخصوصية
              </p>
            </SectionCard>
            
            <SectionCard 
              variant="ghost" 
              className="bg-gradient-to-br from-green-50 to-green-100/50 border-green-200/60 text-center space-sm hover:shadow-md transition-shadow duration-200"
            >
              <Clock className="w-8 h-8 sm:w-10 sm:h-10 mx-auto text-green-600" />
              <h3 className="text-sm sm:text-base font-semibold text-green-900">
                مرن وسريع
              </h3>
              <p className="text-xs sm:text-sm text-green-700/90 leading-relaxed">
                يمكنك إيقاف الاختبار والعودة لاحقاً من حيث توقفت
              </p>
            </SectionCard>
            
            <SectionCard 
              variant="ghost" 
              className="bg-gradient-to-br from-purple-50 to-purple-100/50 border-purple-200/60 text-center space-sm hover:shadow-md transition-shadow duration-200 sm:col-span-2 lg:col-span-1"
            >
              <FileText className="w-8 h-8 sm:w-10 sm:h-10 mx-auto text-purple-600" />
              <h3 className="text-sm sm:text-base font-semibold text-purple-900">
                تقرير شامل
              </h3>
              <p className="text-xs sm:text-sm text-purple-700/90 leading-relaxed">
                ستحصل على تقرير مفصل وتحليل شخصيتك
              </p>
            </SectionCard>
          </div>
        </div>
      </ScreenContainer>
      </main>
    </>
  );
}

export default function Login() {
  return (
    <GuardedRoute page="login">
      <LoginContent />
    </GuardedRoute>
  );
}