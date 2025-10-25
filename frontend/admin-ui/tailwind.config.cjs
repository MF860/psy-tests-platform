/** @type {import('tailwindcss').Config} */
const sharedConfig = require('../shared-tailwind.config.js');

module.exports = {
  darkMode: 'media',
  content: ['./index.html','./src/**/*.{ts,tsx}'],
  theme: {
    ...sharedConfig.theme,
    container: {
      center: true,
      padding: '1rem',
      screens: {
        sm: '640px',
        md: '768px',
        lg: '1024px',
        xl: '1280px',
        '2xl': '1536px'
      },
    },
    extend: {
      ...sharedConfig.theme.extend,
      // Admin UI specific overrides
      keyframes: {
        ...sharedConfig.theme.extend.keyframes,
        'accordion-down': { 
          from: { height: '0' }, 
          to: { height: 'var(--radix-accordion-content-height)' } 
        },
        'accordion-up': { 
          from: { height: 'var(--radix-accordion-content-height)' }, 
          to: { height: '0' } 
        },
      },
      animation: { 
        ...sharedConfig.theme.extend.animation,
        'accordion-down': 'accordion-down 0.2s ease-out', 
        'accordion-up': 'accordion-up 0.2s ease-out',
      },
      boxShadow: {
        ...sharedConfig.theme.extend.boxShadow,
        soft: '0 8px 30px rgba(0,0,0,.06)',
        glow: '0 0 20px rgba(59, 130, 246, 0.3)'
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('tailwindcss-animate')
  ],
};
