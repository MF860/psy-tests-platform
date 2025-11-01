import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Loader2, Shield, Clock, FileText, CreditCard } from "lucide-react";
import UserHeader from "@/components/layout/UserHeader";
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

  const isValid = value.length === 10;
  const charCount = value.length;

  return (
    <>
      <UserHeader showStep stepLabel="تسجيل الدخول" />
      
      {/* Animated Background */}
      <div className="fixed inset-0 -z-10 overflow-hidden">
        {/* Base gradient */}
        <div className="absolute inset-0 bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950" />
        
        {/* Aurora gradient animations */}
        <div 
          className="absolute inset-0 opacity-30"
          style={{
            background: 'radial-gradient(circle at 20% 10%, rgba(124, 70, 255, 0.25) 0%, transparent 40%), radial-gradient(circle at 80% 30%, rgba(34, 211, 238, 0.25) 0%, transparent 40%)',
            animation: 'aurora 12s ease-in-out infinite'
          }}
        />
        <div 
          className="absolute inset-0 opacity-20"
          style={{
            background: 'radial-gradient(circle at 60% 70%, rgba(163, 255, 18, 0.15) 0%, transparent 30%)',
            animation: 'aurora-reverse 15s ease-in-out infinite'
          }}
        />
        
        {/* Noise texture overlay */}
        <div 
          className="absolute inset-0 opacity-[0.015]"
          style={{
            backgroundImage: 'url("data:image/svg+xml,%3Csvg viewBox=\'0 0 256 256\' xmlns=\'http://www.w3.org/2000/svg\'%3E%3Cfilter id=\'noiseFilter\'%3E%3CfeTurbulence type=\'fractalNoise\' baseFrequency=\'0.9\' numOctaves=\'4\' /%3E%3C/filter%3E%3Crect width=\'100%25\' height=\'100%25\' filter=\'url(%23noiseFilter)\' /%3E%3C/svg%3E")',
            backgroundRepeat: 'repeat'
          }}
        />
        
        {/* Floating particles */}
        <div className="absolute inset-0">
          {[...Array(20)].map((_, i) => (
            <div
              key={i}
              className="absolute w-1 h-1 bg-white/20 rounded-full"
              style={{
                left: `${Math.random() * 100}%`,
                top: `${Math.random() * 100}%`,
                animation: `float ${5 + Math.random() * 10}s ease-in-out infinite`,
                animationDelay: `${Math.random() * 5}s`
              }}
            />
          ))}
        </div>
      </div>

      <main className="relative min-h-[calc(100vh-56px)] sm:min-h-[calc(100vh-64px)] grid place-items-center py-8 sm:py-12 px-4">
        {/* Main Auth Card */}
        <section className="relative z-10 w-full max-w-xl mx-auto">
          {/* Glow effect behind card */}
          <div 
            className="absolute inset-0 bg-gradient-to-r from-violet-500/20 to-cyan-500/20 blur-3xl opacity-50"
            style={{ transform: 'scale(1.1)' }}
          />
          
          <div className="relative rounded-3xl border border-white/15 bg-white/10 backdrop-blur-xl shadow-[0_10px_50px_-10px_rgba(0,0,0,0.4)] p-6 sm:p-8 md:p-10">
            {/* Title Section */}
            <div className="text-center space-y-3 mb-8">
              <h1 className="text-2xl sm:text-3xl md:text-4xl font-bold bg-gradient-to-r from-white via-white to-white/80 bg-clip-text text-transparent leading-tight">
                مرحبًا بك في منصة الاختبارات القياسية
              </h1>
              <p className="text-base sm:text-lg text-white/80 max-w-md mx-auto leading-relaxed">
                أدخل الرقم الوطني للبدء في الاختبار النفسي المخصص لك
              </p>
            </div>

            {/* Form */}
            <form onSubmit={onSubmit} className="space-y-6">
              <div className="space-y-2">
                <label htmlFor="nid" className="block text-sm font-medium text-white/90 text-right">
                  الرقم الوطني <span className="text-rose-400">*</span>
                </label>
                
                <div className="relative group">
                  {/* Icon */}
                  <div className="absolute top-1/2 -translate-y-1/2 end-4 z-10 pointer-events-none">
                    <CreditCard 
                      className={`w-5 h-5 transition-colors duration-200 ${
                        error ? 'text-rose-400' : 
                        isValid ? 'text-emerald-400' : 
                        'text-white/40 group-focus-within:text-violet-400'
                      }`}
                      aria-hidden="true"
                    />
                  </div>
                  
                  {/* Input */}
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
                    disabled={busy}
                    aria-invalid={!!error}
                    aria-describedby={error ? "nid-error" : "nid-help"}
                    autoComplete="off"
                    className={`
                      w-full rounded-2xl px-4 py-3 pe-12 ps-4
                      text-center font-mono text-base sm:text-lg
                      bg-white/90 text-slate-900 placeholder:text-slate-500
                      border-2 outline-none
                      transition-all duration-200
                      focus:bg-white focus:scale-[1.01]
                      disabled:opacity-60 disabled:cursor-not-allowed
                      ${error 
                        ? 'border-rose-400 focus:border-rose-500 focus:ring-4 focus:ring-rose-500/20' 
                        : isValid
                        ? 'border-emerald-400 focus:border-emerald-500 focus:ring-4 focus:ring-emerald-500/20'
                        : 'border-transparent focus:border-violet-500 focus:ring-4 focus:ring-violet-500/20'
                      }
                    `}
                  />
                  
                  {/* Character counter */}
                  <div className="absolute bottom-1 start-3 text-xs text-white/50 pointer-events-none">
                    {charCount}/10
                  </div>
                </div>

                {/* Helper text / Error */}
                {!error ? (
                  <p id="nid-help" className="text-sm text-white/70 text-right pe-1">
                    {isValid ? '✓ الرقم صحيح' : 'يرجى التأكد من إدخال 10 أرقام صحيحة.'}
                  </p>
                ) : (
                  <p 
                    id="nid-error" 
                    className="text-sm text-rose-400 text-right pe-1 font-medium flex items-center justify-end gap-1"
                    role="alert"
                  >
                    <span>⚠</span>
                    <span>{error}</span>
                  </p>
                )}
              </div>

              {/* Submit Button */}
              <Button 
                type="submit" 
                disabled={busy || !isValid}
                className={`
                  w-full rounded-2xl py-6 text-base sm:text-lg font-bold
                  bg-gradient-to-r from-violet-500 to-cyan-500
                  text-white shadow-lg shadow-violet-500/25
                  hover:brightness-110 hover:shadow-xl hover:shadow-violet-500/30
                  active:scale-[0.99]
                  disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:brightness-100
                  transition-all duration-200
                  relative overflow-hidden
                  group
                `}
                aria-describedby="submit-help"
              >
                {/* Button shine effect */}
                <div className="absolute inset-0 bg-gradient-to-r from-transparent via-white/20 to-transparent -translate-x-full group-hover:translate-x-full transition-transform duration-1000" />
                
                <span className="relative flex items-center justify-center gap-2">
                  {busy ? (
                    <>
                      <Loader2 className="w-5 h-5 animate-spin" />
                      جاري التحقق…
                    </>
                  ) : (
                    <>
                      متابعة
                      <svg className="w-5 h-5 transition-transform group-hover:translate-x-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                      </svg>
                    </>
                  )}
                </span>
              </Button>
            </form>

            {/* Dev mode notice */}
            {process.env.NODE_ENV === 'development' && (
              <div className="mt-6 p-4 bg-amber-500/10 border border-amber-500/20 rounded-2xl text-sm text-amber-200 text-center backdrop-blur-sm">
                <strong className="font-semibold">وضع التطوير:</strong> ادخل الرقم الخاص بك للاستمرار.
              </div>
            )}
          </div>

          {/* Benefits Cards */}
          <ul className="mt-8 grid grid-cols-1 sm:grid-cols-3 gap-4">
            {[
              {
                icon: Shield,
                title: 'آمن ومحمي',
                description: 'بياناتك محمية بأعلى معايير الأمان والخصوصية',
                gradient: 'from-blue-500/10 to-blue-600/5',
                iconColor: 'text-blue-400',
                borderColor: 'border-blue-500/20',
                delay: '0s'
              },
              {
                icon: Clock,
                title: 'مرن وسريع',
                description: 'يمكنك إيقاف الاختبار والعودة لاحقاً من حيث توقفت',
                gradient: 'from-emerald-500/10 to-emerald-600/5',
                iconColor: 'text-emerald-400',
                borderColor: 'border-emerald-500/20',
                delay: '0.1s'
              },
              {
                icon: FileText,
                title: 'تقرير شامل',
                description: 'ستحصل على تقرير مفصل وتحليل شخصيتك',
                gradient: 'from-violet-500/10 to-violet-600/5',
                iconColor: 'text-violet-400',
                borderColor: 'border-violet-500/20',
                delay: '0.2s'
              }
            ].map((benefit, index) => {
              const Icon = benefit.icon;
              return (
                <li 
                  key={index}
                  className={`
                    relative rounded-2xl border backdrop-blur-md
                    bg-gradient-to-br ${benefit.gradient} ${benefit.borderColor}
                    p-5 text-center space-y-3
                    hover:scale-105 hover:shadow-lg
                    transition-all duration-300
                    group cursor-default
                  `}
                  style={{ 
                    animation: `fadeInUp 0.6s ease-out ${benefit.delay} both`,
                  }}
                >
                  <div className="flex justify-center">
                    <div className={`
                      p-3 rounded-xl bg-gradient-to-br ${benefit.gradient}
                      border ${benefit.borderColor}
                      group-hover:-translate-y-1 transition-transform duration-300
                    `}>
                      <Icon className={`w-7 h-7 ${benefit.iconColor}`} aria-hidden="true" />
                    </div>
                  </div>
                  <h3 className="text-base sm:text-lg font-bold text-white">
                    {benefit.title}
                  </h3>
                  <p className="text-sm text-white/80 leading-relaxed">
                    {benefit.description}
                  </p>
                </li>
              );
            })}
          </ul>
        </section>
      </main>

      {/* Add keyframe animations */}
      <style>{`
        @keyframes aurora {
          0%, 100% { transform: scale(1) rotate(0deg); }
          50% { transform: scale(1.1) rotate(5deg); }
        }
        
        @keyframes aurora-reverse {
          0%, 100% { transform: scale(1) rotate(0deg); }
          50% { transform: scale(1.15) rotate(-8deg); }
        }
        
        @keyframes float {
          0%, 100% { transform: translateY(0) translateX(0); opacity: 0.2; }
          50% { transform: translateY(-20px) translateX(10px); opacity: 0.5; }
        }
        
        @keyframes fadeInUp {
          from {
            opacity: 0;
            transform: translateY(20px);
          }
          to {
            opacity: 1;
            transform: translateY(0);
          }
        }
      `}</style>
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