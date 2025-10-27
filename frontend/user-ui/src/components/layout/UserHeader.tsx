import { DESIGN_TOKENS } from "@/lib/design-tokens";

interface UserHeaderProps {
  showStep?: boolean;
  stepLabel?: string;
  className?: string;
}

export default function UserHeader({ showStep, stepLabel, className = "" }: UserHeaderProps) {
  return (
    <header
      className={`z-sticky w-full bg-white/95 backdrop-blur-sm supports-[backdrop-filter]:bg-white/90 border-b border-gray-200 ${className}`}
      style={{ 
        position: 'sticky',
        top: DESIGN_TOKENS.safeArea.top,
        zIndex: DESIGN_TOKENS.zIndex.sticky
      }}
      dir="rtl"
      role="banner"
      aria-label="رأس الصفحة"
    >
      <div className="container-responsive flex items-center justify-between h-14 sm:h-16">
        {/* Brand Section - Right side in RTL */}
        <div className="flex items-center space-sm">
          <img
            src="/FB-ICON.png"
            alt="أيقونة المنصة"
            className="w-10 h-10 sm:w-12 sm:h-12 object-contain flex-shrink-0"
            style={{ minWidth: '40px', minHeight: '40px' }}
          />
          <img
            src="/STEST.png"
            alt="منصة الاختبارات القياسية"
            className="w-10 h-10 sm:w-12 sm:h-12 object-contain flex-shrink-0"
            style={{ minWidth: '40px', minHeight: '40px' }}
          />
          <div className="flex flex-col">
            <h1 className="font-semibold text-base sm:text-lg lg:text-xl text-slate-900 leading-tight">
              <span className="hidden sm:inline">منصة الاختبارات القياسية</span>
              <span className="sm:hidden">الاختبارات القياسية</span>
            </h1>
          </div>
        </div>
        
        {/* Step Indicator - Left side in RTL */}
        <div className="flex items-center gap-2">
          {showStep && stepLabel && (
            <div 
              className="flex items-center space-sm bg-blue-50 text-blue-700 px-3 py-1.5 rounded-full border border-blue-200"
              role="status"
              aria-live="polite"
            >
              <span className="text-xs sm:text-sm font-medium">
                {stepLabel}
              </span>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}