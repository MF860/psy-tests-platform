import { useState } from "react";
import { Loader2, Shield, Clock, FileText, CreditCard, ChevronLeft } from "lucide-react";
import UserHeader from "@/components/layout/UserHeader";
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
    <>
      {/* Animated Luxury Gradient Background */}
      <div className="luxury-gradient-bg" aria-hidden="true" />
      
      <UserHeader />
      
      <main className="luxury-main">
        <div style={{ width: '100%', maxWidth: '1200px', margin: '0 auto' }}>
          {/* Premium Login Card */}
          <section className="luxury-card">
            <header className="luxury-card-header">
              <h1 className="luxury-card-title">
                تسجيل الدخول
              </h1>
              <p className="luxury-card-subtitle">
                أدخل الرقم الوطني للبدء في الاختبار النفسي المخصص لك
              </p>
            </header>

            {/* Login Form */}
            <form onSubmit={onSubmit} className="luxury-form" noValidate>
              <div className="luxury-form-group">
                <label htmlFor="national-id" className="luxury-label">
                  الرقم الوطني
                  <span className="luxury-label-required" aria-label="مطلوب">*</span>
                </label>
                
                <div className="luxury-input-wrapper">
                  <input
                    id="national-id"
                    dir="ltr"
                    className="luxury-input"
                    type="tel"
                    inputMode="numeric"
                    pattern="[0-9]*"
                    maxLength={10}
                    placeholder="أدخل 10 أرقام"
                    aria-label="الرقم الوطني"
                    aria-describedby={error ? "nid-error" : "nid-help"}
                    aria-invalid={!!error}
                    value={value}
                    onChange={(e) => onChange(e.target.value)}
                    disabled={busy}
                    autoComplete="off"
                    style={{ paddingInlineEnd: '44px' }}
                  />
                  
                  {/* Icon */}
                  <div className="luxury-input-icon">
                    <CreditCard 
                      size={20}
                      strokeWidth={2}
                      aria-hidden="true"
                      style={{
                        color: error ? 'var(--error)' : isValid ? 'var(--success)' : 'inherit'
                      }}
                    />
                  </div>
                  
                  {/* Character Counter */}
                  <div className="luxury-input-counter" aria-live="polite">
                    {charCount}/10
                  </div>
                </div>

                {/* Helper / Error Text */}
                {error ? (
                  <div 
                    id="nid-error" 
                    className="luxury-helper-text luxury-error-text"
                    role="alert"
                  >
                    <span aria-hidden="true">⚠</span>
                    <span>{error}</span>
                  </div>
                ) : isValid ? (
                  <div 
                    id="nid-help" 
                    className="luxury-helper-text luxury-success-text"
                  >
                    <span aria-hidden="true">✓</span>
                    <span>الرقم صحيح</span>
                  </div>
                ) : (
                  <div id="nid-help" className="luxury-helper-text">
                    <span>يرجى التأكد من إدخال 10 أرقام صحيحة</span>
                  </div>
                )}
              </div>

              {/* Submit Button */}
              <button
                type="submit"
                className="luxury-btn"
                disabled={!isValid || busy}
                aria-describedby="submit-help"
              >
                {busy ? (
                  <>
                    <Loader2 size={20} className="luxury-btn-spinner" />
                    <span>جاري التحقق…</span>
                  </>
                ) : (
                  <>
                    <span>متابعة</span>
                    <ChevronLeft size={20} style={{ transform: 'scaleX(-1)' }} />
                  </>
                )}
              </button>

              <span id="submit-help" className="sr-only">
                سيتم التحقق من الرقم الوطني والانتقال إلى الاختبار
              </span>
            </form>

            {/* Dev Mode Notice */}
            {process.env.NODE_ENV === 'development' && (
              <div style={{
                marginTop: 'var(--space-6)',
                padding: 'var(--space-4)',
                background: 'rgba(245, 158, 11, 0.1)',
                border: '1px solid rgba(245, 158, 11, 0.3)',
                borderRadius: 'var(--radius-md)',
                fontSize: '0.875rem',
                color: '#d97706',
                textAlign: 'center'
              }}>
                <strong style={{ fontWeight: 600 }}>وضع التطوير:</strong> أدخل رقمك الوطني للمتابعة
              </div>
            )}
          </section>

          {/* Benefits / Features */}
          <ul className="luxury-features">
            {[
              {
                icon: Shield,
                title: 'آمن ومحمي',
                description: 'بياناتك محمية بأعلى معايير الأمان والخصوصية',
                color: 'var(--luxury-blue)'
              },
              {
                icon: Clock,
                title: 'مرن وسريع',
                description: 'يمكنك إيقاف الاختبار والعودة لاحقاً من حيث توقفت',
                color: 'var(--luxury-green)'
              },
              {
                icon: FileText,
                title: 'تقرير شامل',
                description: 'ستحصل على تقرير مفصل وتحليل كامل لشخصيتك',
                color: '#0d9488'
              }
            ].map((feature, index) => {
              const Icon = feature.icon;
              return (
                <li 
                  key={index}
                  className="luxury-feature-card"
                  style={{
                    animationDelay: `${index * 100}ms`
                  }}
                >
                  <div className="luxury-feature-icon">
                    <Icon 
                      size={28} 
                      strokeWidth={2}
                      style={{ color: feature.color }} 
                      aria-hidden="true" 
                    />
                  </div>
                  <h3 className="luxury-feature-title">{feature.title}</h3>
                  <p className="luxury-feature-description">{feature.description}</p>
                </li>
              );
            })}
          </ul>
        </div>
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