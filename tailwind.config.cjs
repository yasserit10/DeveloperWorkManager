/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Components/**/*.{razor,cshtml,html}',
    './Pages/**/*.{razor,cshtml,html}'
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#f0f7ff',
          100: '#dcecff',
          500: '#2f7df4',
          600: '#1768e8',
          700: '#1255bd',
          900: '#102a56'
        }
      },
      boxShadow: {
        soft: '0 16px 45px rgba(30, 64, 175, .08)',
        sidebar: '0 0 35px rgba(15, 23, 42, .09)'
      }
    }
  },
  plugins: []
};
