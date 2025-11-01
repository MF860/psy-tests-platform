import { useState } from "react";
import { Loader2, Shield, Clock, FileText, CreditCard } from "lucide-react";
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
      <UserHeader showStep stepLabel="تسجيل الدخول" />
      
      {/* Animated Background */}
      <div className="hero-bg" aria-hidden="true" />
      
      {/* Floating Particles */}
      <div className="particles" aria-hidden="true">
        {[...Array(15)].map((_, i) => (
          <div
            key={i}
            className="particle"
            style={{
              left: `${Math.random() * 100}%`,
              top: `${Math.random() * 100}%`,
              animationDelay: `${Math.random() * 5}s`,
              animationDuration: `${5 + Math.random() * 10}s`
            }}
          />
        ))}
      </div>

      <main className="main">
        <section className="container container-narrow" style={{ position: 'relative', zIndex: 1 }}>
          {/* Glass Card with Glow */}
          <div style={{ position: 'relative' }}>
            {/* Glow effect */}
            <div 
              style={{
                position: 'absolute',
                inset: 0,
                background: 'linear-gradient(90deg, rgba(124, 70, 255, 0.2), rgba(34, 211, 238, 0.2))',
                filter: 'blur(40px)',
                transform: 'scale(1.1)',
                opacity: 0.5,
                zIndex: -1
              }}
              aria-hidden="true"
            />
            
            <div className="glass" style={{ padding: '28px 28px 24px' }}>
              {/* Title Section */}
              <h1 style={{ 
                textAlign: 'center', 
                margin: '0 0 8px', 
                fontSize: 'clamp(1.5rem, 4vw, 2rem)',
                fontWeight: 700,
                lineHeight: 1.2,
                background: 'linear-gradient(135deg, #fff 0%, rgba(255,255,255,0.8) 100%)',
                WebkitBackgroundClip: 'text',
                WebkitTextFillColor: 'transparent',
                backgroundClip: 'text'
              }}>
                مرحبًا بك في منصة الاختبارات القياسية
              </h1>
              
              <p style={{ 
                textAlign: 'center', 
                opacity: 0.85, 
                margin: '0 0 20px',
                fontSize: 'clamp(0.9rem, 2vw, 1.1rem)',
                lineHeight: 1.6
              }}>
                أدخل الرقم الوطني للبدء في الاختبار النفسي المخصص لك
              </p>

              {/* Form */}
              <form onSubmit={onSubmit} noValidate>
                <label style={{ display: 'block', marginBottom: 8 }}>
                  <span style={{
                    display: 'block',
                    marginBottom: '6px',
                    fontSize: '0.9rem',
                    fontWeight: 500,
                    opacity: 0.9,
                    textAlign: 'right'
                  }}>
                    الرقم الوطني <span style={{ color: '#ef4444' }}>*</span>
                  </span>
                  
                  <div style={{ position: 'relative' }}>
                    {/* Icon */}
                    <div style={{
                      position: 'absolute',
                      top: '50%',
                      transform: 'translateY(-50%)',
                      insetInlineEnd: '12px',
                      zIndex: 10,
                      pointerEvents: 'none'
                    }}>
                      <CreditCard 
                        size={20}
                        style={{
                          color: error ? '#ef4444' : isValid ? '#22c55e' : 'rgba(255,255,255,0.4)',
                          transition: 'color 0.2s'
                        }}
                        aria-hidden="true"
                      />
                    </div>
                    
                    {/* Input */}
                    <input
                      dir="ltr"
                      className="input"
                      type="tel"
                      inputMode="numeric"
                      pattern="[0-9]*"
                      maxLength={10}
                      placeholder="أدخل الرقم الوطني (10 أرقام)"
                      aria-label="الرقم الوطني"
                      aria-describedby={error ? "nid-error" : "nid-help"}
                      aria-invalid={!!error}
                      value={value}
                      onChange={(e) => onChange(e.target.value)}
                      disabled={busy}
                      autoComplete="off"
                      style={{
                        paddingInlineEnd: '44px',
                        paddingInlineStart: '44px',
                        textAlign: 'center',
                        fontFamily: 'ui-monospace, monospace',
                        fontSize: 'clamp(0.95rem, 2vw, 1.1rem)'
                      }}
                    />
                    
                    {/* Character Counter */}
                    <div style={{
                      position: 'absolute',
                      bottom: '6px',
                      insetInlineStart: '12px',
                      fontSize: '0.75rem',
                      color: 'rgba(255,255,255,0.5)',
                      pointerEvents: 'none',
                      fontFamily: 'ui-monospace, monospace'
                    }}>
                      {charCount}/10
                    </div>
                  </div>
                </label>

                {/* Helper Text / Error */}
                <div 
                  id={error ? "nid-error" : "nid-help"}
                  role={error ? "alert" : "status"}
                  style={{ 
                    fontSize: '0.85rem', 
                    marginTop: 6,
                    textAlign: 'right',
                    color: error ? '#ef4444' : isValid ? '#22c55e' : 'rgba(255,255,255,0.85)',
                    fontWeight: error ? 500 : 400,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'flex-end',
                    gap: '4px'
                  }}
                >
                  {error ? (
                    <>
                      <span>⚠</span>
                      <span>{error}</span>
                    </>
                  ) : isValid ? (
                    <>
                      <span>✓</span>
                      <span>الرقم صحيح</span>
                    </>
                  ) : (
                    <span>يرجى التأكد من إدخال 10 أرقام صحيحة.</span>
                  )}
                </div>

                {/* Submit Button */}
                <button
                  type="submit"
                  className="btn"
                  disabled={!isValid || busy}
                  style={{ 
                    width: '100%', 
                    marginTop: 16,
                    fontSize: 'clamp(0.95rem, 2vw, 1.1rem)',
                    position: 'relative',
                    overflow: 'hidden'
                  }}
                  aria-describedby="submit-help"
                >
                  {/* Button shine effect */}
                  <div 
                    style={{
                      position: 'absolute',
                      inset: 0,
                      background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.2), transparent)',
                      transform: 'translateX(-100%)',
                      animation: busy ? 'none' : 'shine 3s ease-in-out infinite'
                    }}
                    aria-hidden="true"
                  />
                  
                  <span style={{ position: 'relative', display: 'flex', alignItems: 'center', gap: '8px' }}>
                    {busy ? (
                      <>
                        <Loader2 size={20} style={{ animation: 'spin 1s linear infinite' }} />
                        جاري التحقق…
                      </>
                    ) : (
                      <>
                        متابعة
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ transform: 'scaleX(-1)' }}>
                          <polyline points="9 18 15 12 9 6" />
                        </svg>
                      </>
                    )}
                  </span>
                </button>
              </form>

              {/* Dev Mode Notice */}
              {process.env.NODE_ENV === 'development' && (
                <div style={{
                  marginTop: 16,
                  padding: '12px 16px',
                  background: 'rgba(245, 158, 11, 0.1)',
                  border: '1px solid rgba(245, 158, 11, 0.2)',
                  borderRadius: '16px',
                  fontSize: '0.85rem',
                  color: '#fbbf24',
                  textAlign: 'center'
                }}>
                  <strong style={{ fontWeight: 600 }}>وضع التطوير:</strong> ادخل الرقم الخاص بك للاستمرار.
                </div>
              )}
            </div>
          </div>

          {/* Benefits / Features */}
          <ul className="grid-3" style={{ maxWidth: 980, margin: '24px auto 0' }}>
            {[
              {
                icon: Shield,
                title: 'آمن ومحمي',
                description: 'بياناتك محمية بأعلى معايير الأمان والخصوصية',
                color: '#3b82f6'
              },
              {
                icon: Clock,
                title: 'مرن وسريع',
                description: 'يمكنك إيقاف الاختبار والعودة لاحقاً من حيث توقفت',
                color: '#22c55e'
              },
              {
                icon: FileText,
                title: 'تقرير شامل',
                description: 'ستحصل على تقرير مفصل وتحليل شخصيتك',
                color: '#a855f7'
              }
            ].map((feature, index) => {
              const Icon = feature.icon;
              return (
                <li 
                  key={index}
                  className="feature"
                  style={{
                    animation: `fadeInUp 0.6s ease-out ${index * 0.1}s both`
                  }}
                >
                  <div style={{ 
                    display: 'inline-flex',
                    padding: '10px',
                    borderRadius: '12px',
                    background: `${feature.color}20`,
                    marginBottom: '12px'
                  }}>
                    <Icon size={24} style={{ color: feature.color }} aria-hidden="true" />
                  </div>
                  <strong>{feature.title}</strong>
                  <p>{feature.description}</p>
                </li>
              );
            })}
          </ul>
        </section>
      </main>

      {/* Keyframe Animations */}
      <style>{`
        @keyframes shine {
          0%, 100% { transform: translateX(-100%); }
          50% { transform: translateX(100%); }
        }
        
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
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