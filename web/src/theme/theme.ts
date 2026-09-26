import { createTheme, type PaletteMode } from '@mui/material/styles'

/**
 * Reads a resolved value from the design tokens in tokens.css (the single
 * source of truth, generated from tokens.json) instead of duplicating hex
 * values here. Must run after the .dark class for `mode` has been applied
 * to <html>, so callers set that class before calling getTheme().
 */
function cssVar(name: string, fallback: string): string {
  if (typeof window === 'undefined') return fallback
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  return value || fallback
}

export function getTheme(mode: PaletteMode) {
  const isDark = mode === 'dark'

  return createTheme({
    palette: {
      mode,
      primary: {
        main: cssVar('--color-primary', isDark ? '#5B85C4' : '#1D3D6B'),
        dark: cssVar('--color-primary-hover', isDark ? '#3E639C' : '#16304F'),
        contrastText: cssVar('--color-primary-foreground', isDark ? '#0B1220' : '#FFFFFF'),
      },
      secondary: {
        main: cssVar('--color-secondary', isDark ? '#1E293B' : '#F1F5F9'),
        contrastText: cssVar('--color-secondary-foreground', isDark ? '#F1F5F9' : '#0F172A'),
      },
      background: {
        default: cssVar('--color-background', isDark ? '#0B1220' : '#F8FAFC'),
        paper: cssVar('--color-surface', isDark ? '#111827' : '#FFFFFF'),
      },
      text: {
        primary: cssVar('--color-foreground', isDark ? '#F1F5F9' : '#0F172A'),
        secondary: cssVar('--color-muted-foreground', isDark ? '#94A3B8' : '#64748B'),
      },
      divider: cssVar('--color-border', isDark ? '#1E293B' : '#E2E8F0'),
      success: {
        main: cssVar('--color-success', isDark ? '#4ADE80' : '#15803D'),
      },
      warning: {
        main: cssVar('--color-warning', isDark ? '#FBBF24' : '#B45309'),
      },
      error: {
        main: cssVar('--color-destructive', isDark ? '#F87171' : '#B91C1C'),
      },
      info: {
        main: cssVar('--color-info', isDark ? '#7DA3E3' : '#285089'),
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
