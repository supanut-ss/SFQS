import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ThemeProvider } from '@mui/material/styles'
import { theme } from './theme/theme'
import './index.css'
import App from './App.tsx'
import { ToastProvider } from './components/ui'
import { AuthProvider } from './lib/auth'
import { RouterProvider } from './lib/router'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={theme}>
      <ToastProvider>
        <AuthProvider>
          <RouterProvider>
            <App />
          </RouterProvider>
        </AuthProvider>
      </ToastProvider>
    </ThemeProvider>
  </StrictMode>,
)
