import { DESIGN_TOKENS } from "@/lib/design-tokens";

interface UserHeaderProps {
  showStep?: boolean;
  stepLabel?: string;
  className?: string;
}

export default function UserHeader({ showStep, stepLabel, className = "" }: UserHeaderProps) {
  return (
    <header
      className={`z-sticky w-full backdrop-blur-md supports-[backdrop-filter]:bg-slate-950/5 border-b border-white/10 ${className}`}
      style={{ 
        position: 'sticky',
        top: DESIGN_TOKENS.safeArea.top,
        zIndex: DESIGN_TOKENS.zIndex.sticky,
        boxShadow: '0 1px 3px 0 rgba(0, 0, 0, 0.05)'
      }}
      dir="rtl"
      role="banner"
      aria-label="رأس الصفحة"
    >
      <div className="container-responsive">
        <div className="grid grid-cols-3 items-center h-14 sm:h-16 gap-4">
          {/* FB Icon - Left side in RTL */}
          <div className="justify-self-start">
            <div className="relative group">
              <div className="absolute inset-0 bg-gradient-to-r from-violet-500/20 to-cyan-500/20 rounded-xl blur-sm opacity-0 group-hover:opacity-100 transition-opacity duration-300" />
              <img
                src="/FB-ICON.png"
                alt="أيقونة المنصة"
                className="relative w-9 h-9 sm:w-10 sm:h-10 object-contain rounded-xl ring-1 ring-white/20 hover:ring-white/40 transition-all duration-300 hover:scale-105"
                style={{ minWidth: '36px', minHeight: '36px' }}
              />
            </div>
          </div>
          
          {/* STEST Logo - Center */}
          <div className="justify-self-center flex items-center">
            <img
              src="/STEST.png"
              alt="منصة الاختبارات القياسية"
              className="h-10 sm:h-12 object-contain"
              style={{ minHeight: '40px' }}
            />
          </div>
          
          {/* Step Indicator - Right side in RTL */}
          <div className="justify-self-end">
            {showStep && stepLabel && (
              <div 
                className="flex items-center px-3 py-1.5 rounded-full bg-gradient-to-r from-violet-500/10 to-cyan-500/10 border border-violet-500/20 backdrop-blur-sm"
                role="status"
                aria-live="polite"
              >
                <span className="text-xs sm:text-sm font-medium bg-gradient-to-r from-violet-400 to-cyan-400 bg-clip-text text-transparent">
                  {stepLabel}
                </span>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}