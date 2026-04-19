/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'Consolas', 'monospace'],
      },
      colors: {
        azure: {
          50: "#f5faff",
          100: "#e6f2fd",
          200: "#cce6fb",
          300: "#99ccf7",
          400: "#66b3f0",
          500: "#0078d4",
          600: "#005a9e",
          700: "#00437d",
          800: "#002f5c",
          900: "#001b3a",
          950: "#000d1e",
        },
        azureGreen: {
          50: "#f9fdf9",
          100: "#f0fbf0",
          200: "#e0f7e0",
          300: "#bff0bf",
          400: "#8fd98f",
          500: "#107c10",
          600: "#005700",
          700: "#003d00",
          800: "#002200",
          900: "#000000",
        },
        surface: {
          DEFAULT: 'var(--eg-surface)',
          dim: 'var(--eg-surface-dim)',
          container: 'var(--eg-surface-container)',
          high: 'var(--eg-surface-container-high)',
        },
      },
      borderColor: {
        subtle: 'var(--eg-border-subtle)',
        DEFAULT: 'var(--eg-border)',
      },
      boxShadow: {
        'card': '0 1px 3px 0 rgb(0 0 0 / 0.06), 0 1px 2px -1px rgb(0 0 0 / 0.06)',
        'card-hover': '0 4px 12px -2px rgb(0 0 0 / 0.08), 0 2px 4px -2px rgb(0 0 0 / 0.04)',
        'elevated': '0 8px 24px -4px rgb(0 0 0 / 0.12), 0 2px 8px -2px rgb(0 0 0 / 0.06)',
        'panel': '0 12px 40px -8px rgb(0 0 0 / 0.15)',
      },
      borderRadius: {
        'card': '12px',
        'button': '8px',
      },
      animation: {
        'fade-in': 'fade-in 250ms cubic-bezier(0.4, 0, 0.2, 1) both',
        'slide-up': 'slide-up 300ms cubic-bezier(0.4, 0, 0.2, 1) both',
        'slide-in-left': 'slide-in-left 250ms cubic-bezier(0.4, 0, 0.2, 1) both',
        'pulse-subtle': 'pulse-subtle 2s ease-in-out infinite',
      },
      keyframes: {
        'fade-in': {
          from: { opacity: '0', transform: 'translateY(4px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        'slide-up': {
          from: { opacity: '0', transform: 'translateY(12px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        'slide-in-left': {
          from: { opacity: '0', transform: 'translateX(-8px)' },
          to: { opacity: '1', transform: 'translateX(0)' },
        },
        'pulse-subtle': {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.7' },
        },
      },
    },
  },
  plugins: [],
}
