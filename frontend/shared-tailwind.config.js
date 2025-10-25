/** 
 * @type {import('tailwindcss').Config} 
 * 
 * UNIFIED DESIGN SYSTEM CONFIGURATION
 * For Arabic Psychometric Testing Platform
 * Shared across User UI and Admin UI
 * 
 * Design Principles:
 * - RTL-first Arabic layout
 * - WCAG AA accessibility compliance
 * - Modern, professional aesthetics
 * - Touch-friendly spacing (48px buttons)
 * - Consistent color semantics
 */

module.exports = {
  // Core theme extensions
  theme: {
    extend: {
      // ═══════════════════════════════════════════════════════════════
      // COLOR SYSTEM
      // ═══════════════════════════════════════════════════════════════
      colors: {
        // Primary palette (Blue) - Main brand color
        primary: {
          DEFAULT: '#3B82F6',
          50: '#EFF6FF',
          100: '#DBEAFE',
          200: '#BFDBFE',
          300: '#93C5FD',
          400: '#60A5FA',
          500: '#3B82F6',
          600: '#2563EB', // Hover state
          700: '#1D4ED8', // Active state
          800: '#1E40AF',
          900: '#1E3A8A',
          foreground: '#FFFFFF',
        },
        
        // Secondary palette (Indigo) - Accent color
        secondary: {
          DEFAULT: '#6366F1',
          50: '#EEF2FF',
          100: '#E0E7FF',
          200: '#C7D2FE',
          300: '#A5B4FC',
          400: '#818CF8',
          500: '#6366F1',
          600: '#4F46E5',
          700: '#4338CA',
          800: '#3730A3',
          900: '#312E81',
          foreground: '#FFFFFF',
        },
        
        // Tertiary palette (Green) - Success/High performance
        tertiary: {
          DEFAULT: '#10B981',
          50: '#ECFDF5',
          100: '#D1FAE5',
          200: '#A7F3D0',
          300: '#6EE7B7',
          400: '#34D399',
          500: '#10B981',
          600: '#059669',
          700: '#047857',
          800: '#065F46',
          900: '#064E3B',
        },
        
        // Semantic colors
        success: {
          DEFAULT: '#10B981',
          50: '#ECFDF5',
          100: '#D1FAE5',
          500: '#10B981',
          600: '#059669',
          700: '#047857',
          foreground: '#FFFFFF',
        },
        
        warning: {
          DEFAULT: '#F59E0B',
          50: '#FFFBEB',
          100: '#FEF3C7',
          500: '#F59E0B',
          600: '#D97706',
          700: '#B45309',
          foreground: '#FFFFFF',
        },
        
        danger: {
          DEFAULT: '#EF4444',
          50: '#FEF2F2',
          100: '#FEE2E2',
          500: '#EF4444',
          600: '#DC2626',
          700: '#B91C1C',
          foreground: '#FFFFFF',
        },
        
        info: {
          DEFAULT: '#06B6D4',
          50: '#ECFEFF',
          100: '#CFFAFE',
          500: '#06B6D4',
          600: '#0891B2',
          700: '#0E7490',
          foreground: '#FFFFFF',
        },
        
        // Neutral grays - Modern gray scale
        gray: {
          50: '#F9FAFB',
          100: '#F3F4F6',
          200: '#E5E7EB',
          300: '#D1D5DB',
          400: '#9CA3AF',
          500: '#6B7280',
          600: '#4B5563',
          700: '#374151',
          800: '#1F2937',
          900: '#111827',
          950: '#030712',
        },
        
        // UI element colors (shadcn/ui compatible)
        background: 'hsl(0 0% 100%)',
        foreground: 'hsl(222.2 84% 4.9%)',
        
        card: {
          DEFAULT: 'hsl(0 0% 100%)',
          foreground: 'hsl(222.2 84% 4.9%)',
        },
        
        popover: {
          DEFAULT: 'hsl(0 0% 100%)',
          foreground: 'hsl(222.2 84% 4.9%)',
        },
        
        muted: {
          DEFAULT: 'hsl(210 40% 96.1%)',
          foreground: 'hsl(215.4 16.3% 46.9%)',
        },
        
        accent: {
          DEFAULT: 'hsl(210 40% 96.1%)',
          foreground: 'hsl(222.2 47.4% 11.2%)',
        },
        
        destructive: {
          DEFAULT: 'hsl(0 84.2% 60.2%)',
          foreground: 'hsl(0 0% 98%)',
        },
        
        border: 'hsl(214.3 31.8% 91.4%)',
        input: 'hsl(214.3 31.8% 91.4%)',
        ring: 'hsl(221.2 83.2% 53.3%)',
      },
      
      // ═══════════════════════════════════════════════════════════════
      // TYPOGRAPHY SYSTEM
      // ═══════════════════════════════════════════════════════════════
      fontFamily: {
        sans: ['Inter', 'Tajawal', 'Cairo', 'system-ui', '-apple-system', 'sans-serif'],
        arabic: ['Tajawal', 'Cairo', 'Noto Naskh Arabic', 'sans-serif'],
        english: ['Inter', 'Roboto', 'system-ui', '-apple-system', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'Consolas', 'Monaco', 'monospace'],
      },
      
      fontSize: {
        // Display - Hero headings
        'display': ['48px', { lineHeight: '56px', fontWeight: '700', letterSpacing: '-0.02em' }],
        
        // Title - Page titles
        'title': ['36px', { lineHeight: '44px', fontWeight: '700', letterSpacing: '-0.01em' }],
        
        // Headings
        'h1': ['30px', { lineHeight: '36px', fontWeight: '600' }],
        'h2': ['24px', { lineHeight: '32px', fontWeight: '600' }],
        'h3': ['20px', { lineHeight: '28px', fontWeight: '600' }],
        
        // Body text
        'body': ['16px', { lineHeight: '24px', fontWeight: '400' }],
        'body-lg': ['18px', { lineHeight: '28px', fontWeight: '400' }],
        'body-sm': ['14px', { lineHeight: '20px', fontWeight: '400' }],
        
        // Utility
        'small': ['14px', { lineHeight: '20px', fontWeight: '400' }],
        'tiny': ['12px', { lineHeight: '16px', fontWeight: '400' }],
        'label': ['14px', { lineHeight: '20px', fontWeight: '500' }],
      },
      
      // ═══════════════════════════════════════════════════════════════
      // SPACING SYSTEM
      // ═══════════════════════════════════════════════════════════════
      spacing: {
        'xs': '4px',    // 0.25rem
        'sm': '8px',    // 0.5rem
        'md': '16px',   // 1rem - Base unit
        'lg': '24px',   // 1.5rem
        'xl': '32px',   // 2rem
        '2xl': '48px',  // 3rem
        '3xl': '64px',  // 4rem
        '4xl': '80px',  // 5rem
        '5xl': '96px',  // 6rem
      },
      
      // ═══════════════════════════════════════════════════════════════
      // BORDER RADIUS
      // ═══════════════════════════════════════════════════════════════
      borderRadius: {
        'none': '0',
        'sm': '4px',
        'DEFAULT': '8px',
        'md': '8px',
        'lg': '12px',
        'xl': '16px',
        '2xl': '20px',
        '3xl': '24px',
        'full': '9999px',
      },
      
      // ═══════════════════════════════════════════════════════════════
      // SHADOWS
      // ═══════════════════════════════════════════════════════════════
      boxShadow: {
        // Card shadows
        'card': '0 4px 12px rgba(0, 0, 0, 0.08)',
        'card-hover': '0 8px 24px rgba(0, 0, 0, 0.12)',
        'elevated': '0 8px 24px rgba(0, 0, 0, 0.15)',
        
        // UI element shadows
        'sm': '0 2px 4px rgba(0, 0, 0, 0.06)',
        'DEFAULT': '0 4px 6px rgba(0, 0, 0, 0.1)',
        'md': '0 6px 12px rgba(0, 0, 0, 0.1)',
        'lg': '0 10px 20px rgba(0, 0, 0, 0.12)',
        'xl': '0 20px 40px rgba(0, 0, 0, 0.15)',
        '2xl': '0 25px 50px rgba(0, 0, 0, 0.18)',
        
        // Functional shadows
        'inner': 'inset 0 2px 4px rgba(0, 0, 0, 0.06)',
        'none': 'none',
      },
      
      // ═══════════════════════════════════════════════════════════════
      // ANIMATIONS
      // ═══════════════════════════════════════════════════════════════
      keyframes: {
        // Fade animations
        'fade-in': {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        'fade-out': {
          '0%': { opacity: '1' },
          '100%': { opacity: '0' },
        },
        
        // Slide animations
        'slide-in-from-top': {
          '0%': { transform: 'translateY(-100%)' },
          '100%': { transform: 'translateY(0)' },
        },
        'slide-in-from-bottom': {
          '0%': { transform: 'translateY(100%)' },
          '100%': { transform: 'translateY(0)' },
        },
        'slide-in-from-left': {
          '0%': { transform: 'translateX(-100%)' },
          '100%': { transform: 'translateX(0)' },
        },
        'slide-in-from-right': {
          '0%': { transform: 'translateX(100%)' },
          '100%': { transform: 'translateX(0)' },
        },
        
        // Scale animations
        'scale-in': {
          '0%': { transform: 'scale(0.95)', opacity: '0' },
          '100%': { transform: 'scale(1)', opacity: '1' },
        },
        'scale-out': {
          '0%': { transform: 'scale(1)', opacity: '1' },
          '100%': { transform: 'scale(0.95)', opacity: '0' },
        },
        
        // Pulse
        'pulse': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.5' },
        },
        
        // Spin
        'spin': {
          '0%': { transform: 'rotate(0deg)' },
          '100%': { transform: 'rotate(360deg)' },
        },
      },
      
      animation: {
        'fade-in': 'fade-in 0.3s ease-out',
        'fade-out': 'fade-out 0.3s ease-out',
        'slide-in-from-top': 'slide-in-from-top 0.3s ease-out',
        'slide-in-from-bottom': 'slide-in-from-bottom 0.3s ease-out',
        'slide-in-from-left': 'slide-in-from-left 0.3s ease-out',
        'slide-in-from-right': 'slide-in-from-right 0.3s ease-out',
        'scale-in': 'scale-in 0.2s ease-out',
        'scale-out': 'scale-out 0.2s ease-out',
        'pulse': 'pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'spin': 'spin 1s linear infinite',
      },
      
      // ═══════════════════════════════════════════════════════════════
      // TRANSITIONS
      // ═══════════════════════════════════════════════════════════════
      transitionDuration: {
        '0': '0ms',
        '75': '75ms',
        '100': '100ms',
        '150': '150ms',
        '200': '200ms',
        '300': '300ms',
        '500': '500ms',
        '700': '700ms',
        '1000': '1000ms',
      },
      
      transitionTimingFunction: {
        'bounce': 'cubic-bezier(0.68, -0.55, 0.265, 1.55)',
        'smooth': 'cubic-bezier(0.4, 0, 0.2, 1)',
      },
    },
  },
  
  // ═══════════════════════════════════════════════════════════════
  // PLUGINS
  // ═══════════════════════════════════════════════════════════════
  // Note: Plugins should be installed in each UI project individually
  // Add these to your tailwind.config.cjs in user-ui and admin-ui:
  // plugins: [
  //   require('@tailwindcss/forms')({ strategy: 'class' }),
  //   require('tailwindcss-animate'),
  // ],
};
