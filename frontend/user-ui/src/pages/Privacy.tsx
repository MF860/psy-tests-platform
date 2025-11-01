import { useState, useRef, useCallback, useEffect } from "react";
import { Checkbox } from "../components/ui/checkbox";
import { Label } from "../components/ui/label";
import { Shield, Database, Users, UserCheck } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { strings } from "../lib/strings";

function PrivacyContent() {
  const { setConsented } = useFlowState();
  const [isConsented, setIsConsented] = useState(false);
  const [hasScrolledToEnd, setHasScrolledToEnd] = useState(false);
  const scrollRef = useRef<HTMLDivElement>(null);

  const handleScroll = useCallback(() => {
    const element = scrollRef.current;
    if (!element) return;
    
    const { scrollTop, scrollHeight, clientHeight } = element;
    const isAtBottom = Math.abs(scrollHeight - clientHeight - scrollTop) < 5;
    
    if (isAtBottom && !hasScrolledToEnd) {
      setHasScrolledToEnd(true);
    }
  }, [hasScrolledToEnd]);

  const handleSubmit = () => {
    if (!isConsented || !hasScrolledToEnd) return;
    
    // Update flow state - GuardedRoute will handle navigation  
    setConsented(true);
  };

  const privacyContent = [
    {
      icon: Database,
      title: strings.privacy.content.dataCollection,
      items: strings.privacy.content.dataList
    },
    {
      icon: Shield,
      title: "حماية وأمان البيانات",
      description: strings.privacy.content.security
    },
    {
      icon: Users,
      title: "كيف نستخدم بياناتك", 
      description: strings.privacy.content.usage
    },
    {
      icon: UserCheck,
      title: "حقوقك في الخصوصية",
      description: strings.privacy.content.rights
    }
  ];

  const canProceed = isConsented && hasScrolledToEnd;

  // Keyboard handling for better accessibility
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Allow Escape to clear consent (if desired)
      if (event.key === 'Escape' && isConsented) {
        setIsConsented(false);
      }
      
      // Allow Enter to submit when conditions are met and button is focused
      if (event.key === 'Enter' && canProceed && document.activeElement?.getAttribute('data-submit') === 'true') {
        handleSubmit();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isConsented, canProceed, handleSubmit]);

  return (
    <div className="relative h-screen w-screen overflow-hidden bg-gradient-to-br from-gray-900 via-purple-900 to-gray-900">
      {/* Animated background particles */}
      <div className="absolute inset-0 opacity-20">
        <div className="absolute top-20 left-20 w-72 h-72 bg-blue-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse"></div>
        <div className="absolute top-40 right-20 w-72 h-72 bg-purple-500 rounded-full mix-blend-multiply filter blur-xl animate-pulse animation-delay-2000"></div>
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
          <div className="px-2.5 sm:px-3 md:px-4 py-1.5 sm:py-2 bg-white/10 backdrop-blur-sm border border-white/20 rounded-full text-blue-300 text-xs sm:text-sm font-medium">
            خصوصية البيانات
          </div>
          <img 
            src="/FB-ICON.png" 
            alt="أيقونة" 
            className="h-8 sm:h-10 md:h-12 object-contain drop-shadow-lg"
          />
        </div>
      </header>

      {/* Main content - centered with scroll - Responsive */}
      <main className="relative z-10 h-full flex items-center justify-center px-3 sm:px-4 pt-24 sm:pt-28 md:pt-32 pb-6 sm:pb-8">
        <div className="w-full max-w-3xl">
          {/* Glassmorphism privacy card */}
          <div className="bg-white/10 backdrop-blur-lg rounded-xl sm:rounded-2xl shadow-xl border border-white/20 overflow-hidden">
            {/* Header */}
            <div className="p-4 sm:p-6 md:p-8 pb-3 sm:pb-4">
              <h1 className="text-white font-bold text-2xl sm:text-3xl mb-2 text-center" dir="rtl">
                {strings.privacy.title}
              </h1>
              <p className="text-gray-300 text-sm sm:text-base text-center leading-relaxed" dir="rtl">
                {strings.privacy.content.introduction}
              </p>
            </div>

            {/* Scrollable content */}
            <div 
              ref={scrollRef}
              onScroll={handleScroll}
              className="px-3 sm:px-6 md:px-8 overflow-y-auto overflow-x-hidden"
              style={{
                maxHeight: 'calc(100vh - 400px)',
                minHeight: '250px',
                scrollbarWidth: 'thin',
                scrollbarColor: 'rgba(255, 255, 255, 0.3) transparent'
              }}
            >
              <div className="space-y-3 sm:space-y-4 pb-4">
                {privacyContent.map((section, index) => {
                  const IconComponent = section.icon;
                  return (
                    <div 
                      key={index} 
                      className="bg-white/5 backdrop-blur-sm rounded-lg p-3 sm:p-4 border border-white/10 hover:bg-white/10 transition-all duration-300"
                    >
                      <div className="flex gap-3 sm:gap-4" dir="rtl">
                        <div className="flex-shrink-0">
                          <div className="w-8 h-8 sm:w-10 sm:h-10 bg-blue-500/20 rounded-lg flex items-center justify-center">
                            <IconComponent className="h-4 w-4 sm:h-5 sm:w-5 text-blue-400" />
                          </div>
                        </div>
                        <div className="flex-1">
                          <h3 className="font-semibold text-base sm:text-lg mb-1.5 sm:mb-2 text-white">
                            {section.title}
                          </h3>
                          {section.items ? (
                            <ul className="space-y-1.5 sm:space-y-2 text-xs sm:text-sm text-gray-300">
                              {section.items.map((item, itemIndex) => (
                                <li key={itemIndex} className="flex items-start gap-2 leading-relaxed">
                                  <span className="text-blue-400 mt-1 flex-shrink-0">•</span>
                                  <span>{item}</span>
                                </li>
                              ))}
                            </ul>
                          ) : (
                            <p className="text-xs sm:text-sm text-gray-300 leading-relaxed">
                              {section.description}
                            </p>
                          )}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Consent and button section */}
            <div className="border-t border-white/20 bg-white/5 backdrop-blur-sm p-4 sm:p-5 md:p-6">
              <div className="space-y-3 sm:space-y-4">
                <div className="flex items-start gap-2.5 sm:gap-3" dir="rtl">
                  <Checkbox
                    id="consent"
                    checked={isConsented}
                    onCheckedChange={(checked) => setIsConsented(checked === true)}
                    className="mt-1 border-white/30 data-[state=checked]:bg-blue-500 data-[state=checked]:border-blue-500"
                    aria-describedby={!hasScrolledToEnd ? "consent-helper" : "consent-label"}
                  />
                  <div className="flex-1">
                    <Label 
                      id="consent-label"
                      htmlFor="consent" 
                      className="text-xs sm:text-sm leading-relaxed cursor-pointer text-white hover:text-gray-200 transition-colors duration-200 block"
                    >
                      {strings.privacy.consentText}
                    </Label>
                    {!hasScrolledToEnd && (
                      <p 
                        id="consent-helper" 
                        className="text-[10px] sm:text-xs text-yellow-300 mt-1 font-medium"
                        aria-live="polite"
                        role="status"
                      >
                        يرجى قراءة جميع المحتوى للمتابعة
                      </p>
                    )}
                    {hasScrolledToEnd && !isConsented && (
                      <p 
                        className="text-[10px] sm:text-xs text-teal-300 mt-1 font-medium"
                        aria-live="polite"
                        role="status"
                      >
                        تم قراءة جميع المحتوى. يمكنك الآن الموافقة للمتابعة
                      </p>
                    )}
                  </div>
                </div>

                <button
                  onClick={handleSubmit}
                  disabled={!canProceed}
                  className="w-full bg-gradient-to-r from-blue-500 to-teal-400 text-white font-bold py-2.5 sm:py-3 px-4 sm:px-6 rounded-lg shadow-lg hover:scale-105 hover:shadow-2xl transition-all duration-300 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:scale-100 text-sm sm:text-base"
                  data-submit="true"
                  aria-describedby={!canProceed ? "button-helper" : undefined}
                >
                  {strings.privacy.continueButton}
                </button>
                
                {!canProceed && (
                  <p 
                    id="button-helper" 
                    className="text-[10px] sm:text-xs text-center text-gray-400"
                    aria-live="polite"
                    dir="rtl"
                  >
                    {!hasScrolledToEnd 
                      ? "اقرأ جميع المحتوى أولاً" 
                      : "يجب الموافقة على سياسة الخصوصية"}
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}

export default function Privacy() {
  return (
    <GuardedRoute page="privacy">
      <PrivacyContent />
    </GuardedRoute>
  );
}