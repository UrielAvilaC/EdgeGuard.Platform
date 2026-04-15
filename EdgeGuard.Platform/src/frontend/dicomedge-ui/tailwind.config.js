/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ["./src/**/*.{html,ts}"],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        azure: {
          50: "#f5faff",
          100: "#e6f2fd",
          200: "#cce6fb",
          300: "#99ccf7",
          400: "#66b3f0",
          500: "#0078d4", // principal
          600: "#005a9e",
          700: "#00437d",
          800: "#002f5c",
          900: "#001b3a",
        },
        azureGreen: {
          50: "#f9fdf9",
          100: "#f0fbf0",
          200: "#e0f7e0",
          300: "#bff0bf",
          400: "#8fd98f",
          500: "#107c10", // principal
          600: "#005700",
          700: "#003d00",
          800: "#002200",
          900: "#000000",
        }
      }
    },
  },
  plugins: [],
}
