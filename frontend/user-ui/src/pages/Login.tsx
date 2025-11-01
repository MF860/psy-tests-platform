import { useState } from "react";
import { Loader2, Shield, Clock, FileText } from "lucide-react";
import { GuardedRoute } from "@/components/GuardedRoute";
import { ApiError } from "@/lib/api";
import { toEnglishDigits, isValidNationalId } from "@/lib/numerals";
import { useFlowState } from "@/lib/useFlowState";
import { startSession } from "@/lib/api-client";
import "@/styles/globals.css";

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
      const { clearSession } = useFlowState.getState();
      clearSession();
      
      const response = await startSession(value);
      
      setSession({
        nationalId: value,
        sessionId: response.sessionId,
        resume: response.resume,
        totalQuestions: response.totalQuestions
      });
      
      if (response.resume) {
        const { setConsented, setInstructionsCompleted } = useFlowState.getState();
        setConsented(true);
        setInstructionsCompleted(true);
      }
      
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setSession({
          nationalId: value,
          sessionId: "",
        });
      } else {
        setError("تعذّر تسجيل الدخول. الرجاء المحاولة لاحقًا.");
      }
    } finally {
      setBusy(false);
    }
  };

  const isValid = value.length === 10;
  const charCount = value.length;

  return (
    <div className="relative h-screen w-screen overflow-hidden bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900">
      {/* Animated background particles/pattern */}
      <div className="absolute inset-0 opacity-20">
        <div className="absolute top-20 left-20 w-72 h-72 bg-blue-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse"></div>
        <div className="absolute top-40 right-20 w-72 h-72 bg-purple-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-2000"></div>
        <div className="absolute bottom-20 left-1/3 w-72 h-72 bg-teal-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-4000"></div>
      </div>

      {/* Header with logos */}
      <header className="absolute top-0 w-full flex justify-between items-center p-8 z-10" dir="rtl">
        <div className="flex-1"></div>
        <div className="flex-1 flex justify-center">
          <img 
            src="/STEST.png" 
            alt="شعار المنصة" 
            className="h-16 object-contain drop-shadow-lg"
          />
        </div>
        <div className="flex-1 flex justify-end">
          <img 
            src="/FB-ICON.png" 
            alt="أيقونة" 
            className="h-12 object-contain drop-shadow-lg"
          />
        </div>
      </header>

      {/* Main content - centered */}
      <main className="relative z-10 h-full flex flex-col items-center justify-center px-4 py-8">
        {/* Glassmorphism login card */}
        <div className="w-full max-w-md bg-white/10 backdrop-blur-lg rounded-2xl shadow-xl border border-white/20 p-6 sm:p-10 mb-6 sm:mb-10">
          <h1 className="text-white font-bold text-2xl sm:text-3xl mb-2 text-center" dir="rtl">
            تسجيل الدخول
          </h1>
          <p className="text-gray-300 text-sm sm:text-base mb-6 sm:mb-8 text-center" dir="rtl">
            أدخل الرقم الوطني للبدء في الاختبار النفسي المخصص لك
          </p>

          <form onSubmit={onSubmit} noValidate>
            <div className="mb-4 sm:mb-6">
              <label 
                htmlFor="national-id" 
                className="block text-white text-sm font-medium mb-2 text-right"
                dir="rtl"
              >
                الرقم الوطني <span className="text-blue-400">*</span>
              </label>
              
              <div className="relative">
                <input
                  id="national-id"
                  dir="ltr"
                  type="tel"
                  inputMode="numeric"
                  pattern="[0-9]*"
                  maxLength={10}
                  placeholder="0000000000"
                  className={`w-full bg-transparent border-b-2 ${
                    error 
                      ? 'border-red-500' 
                      : isValid 
                      ? 'border-teal-400' 
                      : 'border-gray-400'
                  } text-white text-lg py-3 px-2 focus:outline-none focus:border-blue-500 transition-all duration-300`}
                  value={value}
                  onChange={(e) => onChange(e.target.value)}
                  disabled={busy}
                  autoComplete="off"
                  aria-describedby={error ? "nid-error" : "nid-help"}
                  aria-invalid={!!error}
                />
                <span className="absolute left-2 bottom-3 text-gray-400 text-sm">
                  {charCount}/10
                </span>
              </div>

              {/* Validation messages below input */}
              <div className="min-h-[24px] mt-2">
                {error ? (
                  <div 
                    id="nid-error" 
                    className="text-red-400 text-sm text-right animate-in fade-in slide-in-from-top-1 duration-200"
                    role="alert"
                    dir="rtl"
                  >
                    ⚠ {error}
                  </div>
                ) : isValid ? (
                  <div 
                    id="nid-help" 
                    className="text-teal-400 text-sm text-right animate-in fade-in slide-in-from-top-1 duration-200"
                    dir="rtl"
                  >
                    ✓ الرقم صحيح
                  </div>
                ) : (
                  <div id="nid-help" className="text-gray-400 text-sm text-right" dir="rtl">
                    يرجى التأكد من إدخال 10 أرقام صحيحة
                  </div>
                )}
              </div>
            </div>

            <button
              type="submit"
              disabled={!isValid || busy}
              className="w-full bg-gradient-to-r from-blue-500 to-teal-400 text-white font-bold py-3 px-6 rounded-lg shadow-lg hover:scale-105 hover:shadow-2xl transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100 flex items-center justify-center gap-2"
            >
              {busy ? (
                <>
                  <Loader2 size={20} className="animate-spin" />
                  <span>جاري التحقق…</span>
                </>
              ) : (
                <span>متابعة</span>
              )}
            </button>
          </form>

          {process.env.NODE_ENV === 'development' && (
            <div className="mt-6 p-4 bg-yellow-500/10 border border-yellow-500/30 rounded-lg text-yellow-300 text-sm text-center" dir="rtl">
              <strong>وضع التطوير:</strong> أدخل رقمك الوطني للمتابعة
            </div>
          )}
        </div>

        {/* Features section */}
        <div className="w-full max-w-4xl grid grid-cols-1 md:grid-cols-3 gap-6 px-4">
          {[
            {
              icon: FileText,
              title: 'تقرير شامل',
              description: 'ستحصل على تقرير مفصل وتحليل كامل لشخصيتك'
            },
            {
              icon: Clock,
              title: 'مرن وسريع',
              description: 'يمكنك إيقاف الاختبار والعودة لاحقاً من حيث توقفت'
            },
            {
              icon: Shield,
              title: 'آمن ومحمي',
              description: 'بياناتك محمية بأعلى معايير الأمان والخصوصية'
            }
          ].map((feature, index) => {
            const Icon = feature.icon;
            return (
              <div 
                key={index}
                className="bg-white/5 backdrop-blur-sm rounded-lg p-4 border border-white/10 hover:bg-white/10 transition-all duration-300"
                style={{ animationDelay: `${index * 100}ms` }}
              >
                <div className="flex flex-col items-center text-center" dir="rtl">
                  <div className="mb-3 text-blue-400">
                    <Icon size={32} strokeWidth={2} />
                  </div>
                  <h3 className="text-white font-semibold text-lg mb-2">
                    {feature.title}
                  </h3>
                  <p className="text-gray-300 text-sm">
                    {feature.description}
                  </p>
                </div>
              </div>
            );
          })}
        </div>
      </main>
    </div>
  );
}

export default function Login() {
  return (
    <GuardedRoute page="login">
      <LoginContent />
    </GuardedRoute>
  );
}