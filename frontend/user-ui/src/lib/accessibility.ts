// RTL & Accessibility Utilities
// Comprehensive utilities for RTL layout and accessibility improvements

import { useEffect, useRef } from 'react';

/**
 * RTL-aware positioning and spacing utilities
 */
export const RTL_UTILS = {
  // Logical properties for RTL compatibility
  marginInlineStart: (value: string) => ({ marginInlineStart: value }),
  marginInlineEnd: (value: string) => ({ marginInlineEnd: value }),
  paddingInlineStart: (value: string) => ({ paddingInlineStart: value }),
  paddingInlineEnd: (value: string) => ({ paddingInlineEnd: value }),
  
  // Border radius for RTL
  borderStartStartRadius: (value: string) => ({ borderStartStartRadius: value }),
  borderStartEndRadius: (value: string) => ({ borderStartEndRadius: value }),
  borderEndStartRadius: (value: string) => ({ borderEndStartRadius: value }),
  borderEndEndRadius: (value: string) => ({ borderEndEndRadius: value }),
  
  // Icon mirroring for RTL
  mirrorIcon: 'transform: scaleX(-1)',
  
  // Text alignment classes
  textStart: 'text-start', // Maps to text-right in RTL
  textEnd: 'text-end',     // Maps to text-left in RTL
} as const;

/**
 * Accessibility focus management hook
 * Manages focus trapping, restoration, and keyboard navigation
 */
export function useFocusManagement(options: {
  trapFocus?: boolean;
  restoreFocus?: boolean;
  autoFocus?: boolean;
} = {}) {
  const { trapFocus = false, restoreFocus = false, autoFocus = false } = options;
  const containerRef = useRef<HTMLDivElement>(null);
  const previousActiveElement = useRef<Element | null>(null);

  useEffect(() => {
    if (autoFocus && containerRef.current) {
      // Find first focusable element and focus it
      const firstFocusable = containerRef.current.querySelector(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      ) as HTMLElement;
      
      if (firstFocusable) {
        setTimeout(() => firstFocusable.focus(), 50);
      }
    }

    if (restoreFocus) {
      previousActiveElement.current = document.activeElement;
    }

    return () => {
      if (restoreFocus && previousActiveElement.current) {
        (previousActiveElement.current as HTMLElement).focus?.();
      }
    };
  }, [autoFocus, restoreFocus]);

  useEffect(() => {
    if (!trapFocus || !containerRef.current) return;

    const container = containerRef.current;
    const focusableElements = container.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    
    const firstFocusable = focusableElements[0] as HTMLElement;
    const lastFocusable = focusableElements[focusableElements.length - 1] as HTMLElement;

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Tab') return;

      if (event.shiftKey) {
        if (document.activeElement === firstFocusable) {
          event.preventDefault();
          lastFocusable?.focus();
        }
      } else {
        if (document.activeElement === lastFocusable) {
          event.preventDefault();
          firstFocusable?.focus();
        }
      }
    };

    container.addEventListener('keydown', handleKeyDown);
    return () => container.removeEventListener('keydown', handleKeyDown);
  }, [trapFocus]);

  return containerRef;
}

/**
 * Enhanced keyboard navigation hook
 * Provides arrow key navigation and shortcuts
 */
export function useKeyboardNavigation(options: {
  onNext?: () => void;
  onPrevious?: () => void;
  onSubmit?: () => void;
  onEscape?: () => void;
  disabled?: boolean;
} = {}) {
  const { onNext, onPrevious, onSubmit, onEscape, disabled = false } = options;

  useEffect(() => {
    if (disabled) return;

    const handleKeyDown = (event: KeyboardEvent) => {
      // Ignore if user is typing in an input
      const target = event.target as HTMLElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') {
        return;
      }

      switch (event.key) {
        case 'ArrowRight': // Previous in RTL
        case 'ArrowUp':
          event.preventDefault();
          onPrevious?.();
          break;
        
        case 'ArrowLeft': // Next in RTL
        case 'ArrowDown':
          event.preventDefault();
          onNext?.();
          break;
        
        case 'Enter':
          if (!event.shiftKey) {
            event.preventDefault();
            onSubmit?.();
          } else {
            event.preventDefault();
            onPrevious?.();
          }
          break;
        
        case 'Escape':
          event.preventDefault();
          onEscape?.();
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onNext, onPrevious, onSubmit, onEscape, disabled]);
}

/**
 * WCAG Color Contrast Utilities
 */
export const WCAG_CONTRAST = {
  // Helper to calculate luminance
  getLuminance: (hex: string): number => {
    const rgb = parseInt(hex.slice(1), 16);
    const r = (rgb >> 16) & 0xff;
    const g = (rgb >> 8) & 0xff;
    const b = (rgb >> 0) & 0xff;
    
    const [rs, gs, bs] = [r, g, b].map(c => {
      c /= 255;
      return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
    });
    
    return 0.2126 * rs + 0.7152 * gs + 0.0722 * bs;
  },
  
  // Calculate contrast ratio between two colors
  getContrastRatio: (color1: string, color2: string): number => {
    const l1 = WCAG_CONTRAST.getLuminance(color1);
    const l2 = WCAG_CONTRAST.getLuminance(color2);
    const lighter = Math.max(l1, l2);
    const darker = Math.min(l1, l2);
    return (lighter + 0.05) / (darker + 0.05);
  },
  
  // Check if contrast meets WCAG standards
  meetsWCAG: (color1: string, color2: string, level: 'AA' | 'AAA' = 'AA'): boolean => {
    const ratio = WCAG_CONTRAST.getContrastRatio(color1, color2);
    return level === 'AA' ? ratio >= 4.5 : ratio >= 7;
  }
} as const;

/**
 * Screen reader utilities
 */
export const SCREEN_READER = {
  // Announce messages to screen readers
  announce: (message: string, priority: 'polite' | 'assertive' = 'polite'): void => {
    const announcer = document.createElement('div');
    announcer.setAttribute('aria-live', priority);
    announcer.setAttribute('aria-atomic', 'true');
    announcer.className = 'sr-only';
    announcer.textContent = message;
    
    document.body.appendChild(announcer);
    setTimeout(() => document.body.removeChild(announcer), 1000);
  },
  
  // Create skip links for navigation
  createSkipLink: (targetId: string, text: string): HTMLAnchorElement => {
    const skipLink = document.createElement('a');
    skipLink.href = `#${targetId}`;
    skipLink.textContent = text;
    skipLink.className = 'skip-link sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 z-50 bg-primary text-primary-foreground px-3 py-2 rounded';
    return skipLink;
  }
} as const;

/**
 * RTL-aware animation utilities
 */
export const RTL_ANIMATIONS = {
  slideInFromEnd: 'animate-[slide-in-from-right_0.3s_ease-out] rtl:animate-[slide-in-from-left_0.3s_ease-out]',
  slideInFromStart: 'animate-[slide-in-from-left_0.3s_ease-out] rtl:animate-[slide-in-from-right_0.3s_ease-out]',
  slideOutToEnd: 'animate-[slide-out-to-right_0.3s_ease-in] rtl:animate-[slide-out-to-left_0.3s_ease-in]',
  slideOutToStart: 'animate-[slide-out-to-left_0.3s_ease-in] rtl:animate-[slide-out-to-right_0.3s_ease-in]',
} as const;

/**
 * Touch-friendly interaction utilities
 */
export const TOUCH_INTERACTIONS = {
  // Minimum touch target size (44px)
  minTouchTarget: 'min-h-[44px] min-w-[44px]',
  
  // Touch-friendly spacing
  touchSpacing: 'gap-2 sm:gap-3',
  
  // Enhanced tap targets for mobile
  enhancedTapTarget: 'touch-manipulation select-none',
  
  // Prevent zoom on double-tap for iOS
  preventZoom: 'touch-manipulation user-select-none',
} as const;

/**
 * Form accessibility helpers
 */
export const FORM_A11Y = {
  // Generate unique IDs for form controls
  generateId: (prefix: string): string => `${prefix}-${Math.random().toString(36).substr(2, 9)}`,
  
  // Required field marker classes
  requiredMarkerClasses: 'text-destructive ml-1',
  
  // Error message classes
  errorMessageClasses: 'text-sm text-destructive mt-1',
  
  // Success message classes  
  successMessageClasses: 'text-sm text-green-600 mt-1',
  
  // Helper to create error message attributes
  errorMessageAttrs: (id: string) => ({
    id,
    className: 'text-sm text-destructive mt-1',
    role: 'alert' as const,
    'aria-live': 'polite' as const,
  }),
  
  // Helper to create success message attributes
  successMessageAttrs: (id: string) => ({
    id,
    className: 'text-sm text-green-600 mt-1',
    role: 'status' as const,
    'aria-live': 'polite' as const,
  }),
} as const;

/**
 * Mobile-specific accessibility enhancements
 */
export const MOBILE_A11Y = {
  // Prevent zoom on focus (iOS Safari)
  preventZoomOnFocus: {
    fontSize: '16px', // Prevents zoom on iOS
  },
  
  // Enhanced contrast for mobile viewing
  mobileContrast: 'contrast-125 sm:contrast-100',
  
  // Larger tap targets for mobile
  mobileTapTarget: 'h-12 sm:h-10 px-4 sm:px-3',
  
  // Mobile-friendly focus indicators
  mobileFocus: 'focus:ring-4 focus:ring-primary/30 sm:focus:ring-2',
} as const;

// Export comprehensive accessibility class combinations
export const A11Y_CLASSES = {
  button: 'touch-target focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 transition-colors duration-200',
  input: 'touch-target focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary border-input transition-colors duration-200',
  card: 'focus-within:ring-2 focus-within:ring-primary/20 transition-all duration-200',
  interactive: 'hover:bg-muted/50 focus-visible:bg-muted active:bg-muted/80 transition-colors duration-150',
  skipLink: 'skip-link sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 z-50 bg-primary text-primary-foreground px-3 py-2 rounded',
} as const;