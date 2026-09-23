/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ['class'], // toggled by adding .dark to <html>, matching style-guide.html
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    // Every color here is a var() pointing at tokens.css — no new colors are
    // introduced. See ui-plan.md §5: "map ตัวแปรจาก tokens.css เป็น theme
    // ไม่สร้างสีใหม่".
    extend: {
      colors: {
        background: 'var(--color-background)',
        surface: 'var(--color-surface)',
        foreground: 'var(--color-foreground)',
        primary: {
          DEFAULT: 'var(--color-primary)',
          hover: 'var(--color-primary-hover)',
          foreground: 'var(--color-primary-foreground)',
        },
        secondary: {
          DEFAULT: 'var(--color-secondary)',
          foreground: 'var(--color-secondary-foreground)',
        },
        accent: {
          DEFAULT: 'var(--color-accent)',
          foreground: 'var(--color-accent-foreground)',
        },
        muted: {
          DEFAULT: 'var(--color-muted)',
          foreground: 'var(--color-muted-foreground)',
        },
        success: { DEFAULT: 'var(--color-success)', bg: 'var(--color-success-bg)' },
        warning: { DEFAULT: 'var(--color-warning)', bg: 'var(--color-warning-bg)' },
        destructive: {
          DEFAULT: 'var(--color-destructive)',
          bg: 'var(--color-destructive-bg)',
          foreground: 'var(--color-destructive-foreground)',
        },
        info: { DEFAULT: 'var(--color-info)', bg: 'var(--color-info-bg)' },
        highlight: { DEFAULT: 'var(--color-highlight)', bg: 'var(--color-highlight-bg)' },
        border: 'var(--color-border)',
        ring: 'var(--color-ring)',
        freight: {
          import: 'var(--freight-import)',
          'import-bg': 'var(--freight-import-bg)',
          export: 'var(--freight-export)',
          'export-bg': 'var(--freight-export-bg)',
          fcl: 'var(--freight-mode-fcl)',
          'fcl-bg': 'var(--freight-mode-fcl-bg)',
          lcl: 'var(--freight-mode-lcl)',
          'lcl-bg': 'var(--freight-mode-lcl-bg)',
          air: 'var(--freight-mode-air)',
          'air-bg': 'var(--freight-mode-air-bg)',
        },
      },
      fontFamily: {
        heading: 'var(--font-heading)',
        body: 'var(--font-body)',
        numeric: 'var(--font-numeric)',
      },
      borderRadius: {
        DEFAULT: 'var(--primitive-radius-default)',
        sm: 'var(--primitive-radius-sm)',
        md: 'var(--primitive-radius-md)',
        lg: 'var(--primitive-radius-lg)',
        full: 'var(--primitive-radius-full)',
      },
    },
  },
  plugins: [],
}
