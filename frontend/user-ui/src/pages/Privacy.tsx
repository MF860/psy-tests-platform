import { useState, useRef, useCallback, useEffect } from "react";
import { CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Button } from "../components/ui/button";
import { Checkbox } from "../components/ui/checkbox";
import { Label } from "../components/ui/label";
import { Shield, Database, Users, UserCheck } from "lucide-react";
import { GuardedRoute } from "../components/GuardedRoute";
import { useFlowState } from "../lib/useFlowState";
import { strings } from "../lib/strings";
import UserHeader from "../components/layout/UserHeader";
import ScreenContainer from "../components/layout/ScreenContainer";
import SectionCard from "../components/layout/SectionCard";

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
    <div className="min-h-screen bg-gradient-to-b from-slate-50 to-white">
      <UserHeader showStep stepLabel="خصوصية البيانات" />
      <ScreenContainer maxWidth="2xl" className="py-4 sm:py-6 lg:py-8">
        <SectionCard>
          <CardHeader className="pb-4 sm:pb-6">
            <CardTitle className="text-[clamp(20px,2.4vw,28px)] font-bold text-right">
              {strings.privacy.title}
            </CardTitle>
            <p className="text-muted-foreground mt-2 text-right leading-relaxed text-sm sm:text-base">
              {strings.privacy.content.introduction}
            </p>
          </CardHeader>
          
          <CardContent className="pb-0 flex flex-col">
            {/* Scrollable Content Area */}
            <div 
              ref={scrollRef}
              onScroll={handleScroll}
              className="overflow-y-auto overflow-x-hidden pe-2 space-y-4 sm:space-y-6 flex-1"
              style={{
                maxHeight: 'calc(70vh - 200px)',
                minHeight: '300px',
                scrollbarWidth: 'thin',
                scrollbarColor: 'rgb(203 213 225) transparent'
              }}
            >
              {/* Privacy content in responsive grid */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-1 gap-4 sm:gap-6">
                {privacyContent.map((section, index) => {
                  const IconComponent = section.icon;
                  return (
                    <div key={index} className="flex gap-3 sm:gap-4 p-3 sm:p-4 bg-muted/30 rounded-xl">
                      <div className="flex-shrink-0 mt-0.5 sm:mt-1">
                        <IconComponent className="h-5 w-5 sm:h-6 sm:w-6 text-primary" />
                      </div>
                      <div className="flex-1 min-w-0">
                        <h3 className="font-semibold text-base sm:text-lg mb-2 sm:mb-3 text-slate-900">
                          {section.title}
                        </h3>
                        {section.items ? (
                          <ul className="space-y-1.5 sm:space-y-2 text-sm sm:text-base text-muted-foreground">
                            {section.items.map((item, itemIndex) => (
                              <li key={itemIndex} className="flex items-start gap-2 leading-relaxed">
                                <span className="text-primary mt-1 flex-shrink-0 text-xs sm:text-sm">•</span>
                                <span className="break-words">{item}</span>
                              </li>
                            ))}
                          </ul>
                        ) : (
                          <p className="text-sm sm:text-base text-muted-foreground leading-relaxed break-words">
                            {section.description}
                          </p>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
              
              {/* Extra padding to ensure scroll to end detection works */}
              <div className="h-4" />
            </div>

            {/* Sticky Consent Section - Fixed positioning */}
            <div className="bg-white border-t border-slate-200 pt-4 sm:pt-6 pb-4 sm:pb-6 mt-4 sm:mt-6 -mx-6 px-6">
              <div className="space-y-3 sm:space-y-4">
                <div className="flex items-start gap-3">
                  <Checkbox
                    id="consent"
                    checked={isConsented}
                    onCheckedChange={(checked) => setIsConsented(checked === true)}
                    className="mt-0.5 sm:mt-1 min-w-[20px] min-h-[20px] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary transition-all duration-200"
                    aria-describedby={!hasScrolledToEnd ? "consent-helper" : "consent-label"}
                  />
                  <div className="flex-1 min-w-0">
                    <Label 
                      id="consent-label"
                      htmlFor="consent" 
                      className="text-sm sm:text-base leading-relaxed cursor-pointer text-slate-700 hover:text-slate-900 transition-colors duration-200 block"
                    >
                      {strings.privacy.consentText}
                    </Label>
                    {!hasScrolledToEnd && (
                      <p 
                        id="consent-helper" 
                        className="text-xs sm:text-sm text-amber-600 mt-1 font-medium"
                        aria-live="polite"
                        role="status"
                      >
                        يرجى قراءة جميع المحتوى للمتابعة
                      </p>
                    )}
                    {hasScrolledToEnd && !isConsented && (
                      <p 
                        className="text-xs sm:text-sm text-green-600 mt-1 font-medium"
                        aria-live="polite"
                        role="status"
                      >
                        تم قراءة جميع المحتوى. يمكنك الآن الموافقة للمتابعة
                      </p>
                    )}
                  </div>
                </div>

                <Button 
                  onClick={handleSubmit}
                  disabled={!canProceed}
                  className="w-full h-11 sm:h-12 min-h-[44px] text-base sm:text-lg font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                  data-submit="true"
                  aria-describedby={!canProceed ? "button-helper" : undefined}
                >
                  {strings.privacy.continueButton}
                </Button>
                
                {!canProceed && (
                  <p 
                    id="button-helper" 
                    className="text-xs sm:text-sm text-center text-muted-foreground"
                    aria-live="polite"
                  >
                    {!hasScrolledToEnd 
                      ? "اقرأ جميع المحتوى أولاً" 
                      : "يجب الموافقة على سياسة الخصوصية"}
                  </p>
                )}
              </div>
            </div>
          </CardContent>
        </SectionCard>
      </ScreenContainer>
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