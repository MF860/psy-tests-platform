// Design System Tokens & Configuration
// Spacing scale follows 4/8/12/16 system with semantic naming

export const DESIGN_TOKENS = {
  // Spacing Scale (4px base unit)
  spacing: {
    xs: '0.25rem',  // 4px
    sm: '0.5rem',   // 8px  
    md: '0.75rem',  // 12px
    lg: '1rem',     // 16px
    xl: '1.25rem',  // 20px
    '2xl': '1.5rem', // 24px
    '3xl': '2rem',  // 32px
    '4xl': '2.5rem', // 40px
    '5xl': '3rem',  // 48px
  } as const,
  
  // Border Radius
  radius: {
    none: '0',
    sm: '0.125rem',  // 2px
    md: '0.25rem',   // 4px
    lg: '0.375rem',  // 6px
    xl: '0.5rem',    // 8px
    '2xl': '0.75rem', // 12px
    full: '9999px'
  } as const,
  
  // Typography Scale
  typography: {
    xs: '0.75rem',    // 12px
    sm: '0.875rem',   // 14px  
    base: '1rem',     // 16px
    lg: '1.125rem',   // 18px
    xl: '1.25rem',    // 20px
    '2xl': '1.5rem',  // 24px
    '3xl': '1.875rem', // 30px
    '4xl': '2.25rem', // 36px
  } as const,
  
  // Z-Index Layering  
  zIndex: {
    base: 0,
    dropdown: 10,
    sticky: 20,
    modal: 30,
    popover: 40,
    toast: 50,
    tooltip: 60,
  } as const,
  
  // Breakpoints (matches Tailwind defaults)
  breakpoints: {
    sm: '640px',
    md: '768px', 
    lg: '1024px',
    xl: '1280px',
    '2xl': '1536px',
  } as const,
  
  // Safe Area Insets for iOS
  safeArea: {
    top: 'env(safe-area-inset-top, 0px)',
    right: 'env(safe-area-inset-right, 0px)',
    bottom: 'env(safe-area-inset-bottom, 0px)', 
    left: 'env(safe-area-inset-left, 0px)',
  } as const,
  
  // Color Semantic Roles (WCAG AA compliant)
  colors: {
    primary: {
      50: '#f0f9ff',
      100: '#e0f2fe', 
      500: '#0ea5e9',
      600: '#0284c7',
      700: '#0369a1',
      900: '#0c4a6e',
    },
    success: {
      50: '#f0fdf4',
      100: '#dcfce7',
      500: '#22c55e', 
      600: '#16a34a',
      700: '#15803d',
      900: '#14532d',
    },
    warning: {
      50: '#fffbeb',
      100: '#fef3c7',
      500: '#f59e0b',
      600: '#d97706', 
      700: '#b45309',
      900: '#78350f',
    },
    danger: {
      50: '#fef2f2',
      100: '#fee2e2',
      500: '#ef4444',
      600: '#dc2626',
      700: '#b91c1c', 
      900: '#7f1d1d',
    },
  } as const,
  
  // Animation & Transitions
  animations: {
    fast: '150ms cubic-bezier(0.4, 0, 0.2, 1)',
    normal: '200ms cubic-bezier(0.4, 0, 0.2, 1)', 
    slow: '300ms cubic-bezier(0.4, 0, 0.2, 1)',
  } as const,
} as const;

// Utility functions for consuming tokens
export const spacing = (size: keyof typeof DESIGN_TOKENS.spacing) => DESIGN_TOKENS.spacing[size];
export const radius = (size: keyof typeof DESIGN_TOKENS.radius) => DESIGN_TOKENS.radius[size];
export const zIndex = (layer: keyof typeof DESIGN_TOKENS.zIndex) => DESIGN_TOKENS.zIndex[layer];

// Responsive utilities
export const mediaQuery = (breakpoint: keyof typeof DESIGN_TOKENS.breakpoints) => 
  `@media (min-width: ${DESIGN_TOKENS.breakpoints[breakpoint]})`;

// Color contrast validation (WCAG AA = 4.5:1, AAA = 7:1)
export const CONTRAST_RATIOS = {
  AA_NORMAL: 4.5,
  AA_LARGE: 3.0, 
  AAA_NORMAL: 7.0,
  AAA_LARGE: 4.5,
} as const;

// Common component sizing standards
export const COMPONENT_SIZES = {
  // Minimum touch target (iOS/Android accessibility)
  minTouchTarget: '44px',
  
  // Input heights
  input: {
    sm: '2.25rem', // 36px
    md: '2.5rem',  // 40px  
    lg: '2.75rem', // 44px
  },
  
  // Button heights
  button: {
    sm: '2rem',    // 32px
    md: '2.5rem',  // 40px
    lg: '2.75rem', // 44px
  },
  
  // Header heights
  header: {
    mobile: '3.5rem',  // 56px
    desktop: '4rem',   // 64px
  },
} as const;

// RTL-specific utilities
export const RTL_HELPERS = {
  // Direction-aware margin/padding
  marginStart: (size: string) => ({ marginInlineStart: size }),
  marginEnd: (size: string) => ({ marginInlineEnd: size }),
  paddingStart: (size: string) => ({ paddingInlineStart: size }),
  paddingEnd: (size: string) => ({ paddingInlineEnd: size }),
  
  // Border radius for RTL
  roundedStart: (size: string) => ({
    borderStartStartRadius: size,
    borderEndStartRadius: size,
  }),
  roundedEnd: (size: string) => ({ 
    borderStartEndRadius: size,
    borderEndEndRadius: size,
  }),
} as const;

export type DesignTokens = typeof DESIGN_TOKENS;
export type SpacingSize = keyof typeof DESIGN_TOKENS.spacing;
export type RadiusSize = keyof typeof DESIGN_TOKENS.radius;
export type ZIndexLayer = keyof typeof DESIGN_TOKENS.zIndex;