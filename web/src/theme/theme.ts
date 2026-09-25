import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#1D3D6B',
      dark: '#16304F',
      contrastText: '#FFFFFF',
    },
    secondary: {
      main: '#F1F5F9',
      contrastText: '#0F172A',
    },
    background: {
      default: '#F8FAFC',
      paper: '#FFFFFF',
    },
    text: {
      primary: '#0F172A',
      secondary: '#64748B',
    },
    divider: '#E2E8F0',
    success: {
      main: '#15803D',
    },
    warning: {
      main: '#B45309',
    },
    error: {
      main: '#B91C1C',
    },
    info: {
      main: '#285089',
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
