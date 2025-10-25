/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      direction: {
        rtl: 'rtl',
      },
    },
  },
  plugins: [],
}

