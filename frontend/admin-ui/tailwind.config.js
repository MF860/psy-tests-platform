/** @type {import('tailwindcss').Config} */
import { fontFamily } from 'tailwindcss/defaultTheme'

export default {
  darkMode: ["class"],
  content: ["./index.html","./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ['"Noto Naskh Arabic"', ...fontFamily.sans],
      },
      container: { center: true, padding: "2rem" }
    },
  },
  plugins: [require('@tailwindcss/forms')],
}

