import { createTheme, type PaletteMode } from '@mui/material/styles'

export function getTheme(mode: PaletteMode) {
  const isDark = mode === 'dark'

  return createTheme({
    palette: {
      mode,
      primary: {
        main: isDark ? '#5B85C4' : '#1D3D6B',
        dark: isDark ? '#3E639C' : '#16304F',
        contrastText: isDark ? '#0B1220' : '#FFFFFF',
      },
      secondary: {
        main: isDark ? '#1E293B' : '#F1F5F9',
        contrastText: isDark ? '#F1F5F9' : '#0F172A',
      },
      background: {
        default: isDark ? '#0B1220' : '#F8FAFC',
        paper: isDark ? '#111827' : '#FFFFFF',
      },
      text: {
        primary: isDark ? '#F1F5F9' : '#0F172A',
        secondary: isDark ? '#94A3B8' : '#64748B',
      },
      divider: isDark ? '#1E293B' : '#E2E8F0',
      success: {
        main: isDark ? '#4ADE80' : '#15803D',
      },
      warning: {
        main: isDark ? '#FBBF24' : '#B45309',
      },
      error: {
        main: isDark ? '#F87171' : '#B91C1C',
      },
      info: {
        main: isDark ? '#7DA3E3' : '#285089',
      },
    },
    shape: {
      borderRadius: 6,
    },
    typography: {
      fontFamily: "'Inter', -apple-system, 'Segoe UI', Roboto, sans-serif",
      button: {
        textTransform: 'none',
        fontWeight: 600,
      },
    },
  })
}

export const theme = getTheme('light')
