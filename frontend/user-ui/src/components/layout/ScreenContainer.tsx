import { ReactNode } from 'react';

interface ScreenContainerProps {
  children: ReactNode;
  className?: string;
  maxWidth?: 'sm' | 'md' | 'lg' | 'xl' | '2xl' | '4xl' | 'full';
}

export function ScreenContainer({ 
  children, 
  className = '', 
  maxWidth = '4xl' 
}: ScreenContainerProps) {
  const maxWidthClasses = {
    sm: 'max-w-sm',
    md: 'max-w-md',
    lg: 'max-w-lg', 
    xl: 'max-w-xl',
    '2xl': 'max-w-2xl',
    '4xl': 'max-w-4xl',
    full: 'max-w-none'
  };

  return (
    <div dir="rtl">
      <div className={`container ${maxWidthClasses[maxWidth]} mx-auto px-3 sm:px-4 lg:px-6 ${className}`}>
        {children}
      </div>
      <style>{`
        :root {
          --safe-bottom: env(safe-area-inset-bottom);
          --safe-top: env(safe-area-inset-top);
          --safe-left: env(safe-area-inset-left);
          --safe-right: env(safe-area-inset-right);
        }
      `}</style>
    </div>
  );
}

export default ScreenContainer;