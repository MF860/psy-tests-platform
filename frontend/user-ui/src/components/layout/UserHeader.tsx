interface UserHeaderProps {
  showStep?: boolean;
  stepLabel?: string;
  className?: string;
}

export default function UserHeader({ showStep, stepLabel, className = "" }: UserHeaderProps) {
  return (
    <header
      className={`header ${className}`}
      dir="rtl"
      role="banner"
      aria-label="رأس الصفحة"
    >
      <div className="container">
        <div className="header-grid">
          {/* FB Icon - Left side in RTL */}
          <div className="header-logo-corner">
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
          <div className="header-logo-main">
            <img
              src="/STEST.png"
              alt="منصة الاختبارات القياسية"
              className="h-10 sm:h-12 object-contain"
              style={{ minHeight: '40px' }}
            />
          </div>
          
          {/* Step Indicator - Right side in RTL */}
          <div className="header-actions">
            {showStep && stepLabel && (
              <div 
                className="flex items-center px-3 py-1.5 rounded-full bg-gradient-to-r from-violet-500/10 to-cyan-500/10 border border-violet-500/20 backdrop-blur-sm"
                role="status"
                aria-live="polite"
              >
                <span className="text-xs sm:text-sm font-medium bg-gradient-to-r from-violet-400 to-cyan-400 bg-clip-text text-transparent whitespace-nowrap">
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