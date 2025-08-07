/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './Pages/**/*.{razor,html,cshtml}',
    './Shared/**/*.{razor,html,cshtml}',
    './Components/**/*.{razor,html,cshtml}',
    './wwwroot/index.html',
    '!./node_modules/**/*'
  ],
  theme: {
    extend: {
      colors: {
        // Primary colors - Drata inspired
        primary: {
          50: '#E0E7FF',
          100: '#C7D2FE',
          200: '#A5B4FC',
          300: '#818CF8',
          400: '#6366F1',
          500: '#4F46E5',
          600: '#0B4F71', // Main primary
          700: '#073D58',
          800: '#0D5A81',
          900: '#312E81',
        },
        // Accent colors
        accent: {
          blue: '#2196F3',
          green: '#4CAF50',
          orange: '#FF9800',
          red: '#F44336',
        },
        // Gray scale
        gray: {
          50: '#F5F7FA',
          100: '#FFFFFF',
          200: '#E0E6ED',
          300: '#D1D5DB',
          400: '#8792A2',
          500: '#647788',
          600: '#6B7280',
          700: '#1A1F36',
          800: '#111827',
          900: '#000000',
        },
        // Status colors
        success: '#26b050',
        error: '#dc3545',
        warning: '#ffc107',
        info: '#17a2b8',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      fontSize: {
        'xs': '0.75rem',
        'sm': '0.875rem',
        'base': '1rem',
        'lg': '1.125rem',
        'xl': '1.25rem',
        '2xl': '1.5rem',
        '3xl': '1.875rem',
        '4xl': '2.25rem',
        '5xl': '3rem',
      },
      spacing: {
        '18': '4.5rem',
        '88': '22rem',
        '120': '30rem',
      },
      animation: {
        'fade-in': 'fadeIn 0.3s ease-out',
        'slide-in': 'slideIn 0.3s ease-out',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0', transform: 'translateY(10px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' },
        },
        slideIn: {
          '0%': { transform: 'translateX(-100%)' },
          '100%': { transform: 'translateX(0)' },
        },
      },
    },
  },
  plugins: [
    require('@tailwindcss/forms'),
    require('@tailwindcss/typography'),
  ],
}