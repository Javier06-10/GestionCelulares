/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        // Paleta "Stealth Tech"
        tech: {
          'bg-light': '#F3F3F1',
          'card-light': '#FFFFFF',
          'bg-dark': '#121214',
          'card-dark': '#1A1A1E',
          charcoal: '#676567',
          steel: '#8D9097',
          pastel: '#C1C2C6',
          sand: '#CBC0AE',
          accent: '#00E676', // Esmeralda digital: caja abierta, disponible, CTA
          purple: '#6C5CE7'  // Violeta eléctrico: taller y créditos
        }
      },
      fontFamily: {
        sans: ['Segoe UI', 'system-ui', '-apple-system', 'sans-serif']
      }
    }
  },
  plugins: []
};
