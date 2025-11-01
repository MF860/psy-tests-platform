interface UserHeaderProps {
  showStep?: boolean;
  stepLabel?: string;
  className?: string;
}

export default function UserHeader({ showStep, stepLabel, className = "" }: UserHeaderProps) {
  return (
    <header
      className={`luxury-header ${className}`}
      dir="rtl"
      role="banner"
      aria-label="رأس الصفحة"
    >
      <div className="luxury-header-inner">
        {/* Left Logo - FB Icon (Primary) */}
        <div className="luxury-header-logo-left">
          <img
            src="/FB-ICON.png"
            alt="الشعار الرئيسي"
            loading="eager"
            decoding="async"
            srcSet="/FB-ICON.png 1x, /FB-ICON.png 2x, /FB-ICON.png 3x"
          />
        </div>
        
        {/* Center Spacer (for visual balance) */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
          {showStep && stepLabel && (
            <div 
              style={{
                padding: '6px 16px',
                borderRadius: '24px',
                background: 'linear-gradient(135deg, rgba(16, 185, 129, 0.1) 0%, rgba(30, 64, 175, 0.1) 100%)',
                border: '1px solid rgba(16, 185, 129, 0.2)',
                fontSize: '0.875rem',
                fontWeight: 600,
                color: 'var(--luxury-green)',
                whiteSpace: 'nowrap'
              }}
              role="status"
              aria-live="polite"
            >
              {stepLabel}
            </div>
          )}
        </div>
        
        {/* Right Logo - STEST (Partner) */}
        <div className="luxury-header-logo-right">
          <img
            src="/STEST.png"
            alt="شعار الشريك"
            loading="eager"
            decoding="async"
            srcSet="/STEST.png 1x, /STEST.png 2x, /STEST.png 3x"
          />
        </div>
      </div>
    </header>
  );
}