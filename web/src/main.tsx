import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ColorModeProvider } from './theme/ColorModeContext'
import './index.css'
import App from './App.tsx'
import { ToastProvider } from './components/ui'
import { AuthProvider } from './lib/auth'
import { RouterProvider } from './lib/router'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ColorModeProvider>
      <ToastProvider>
        <AuthProvider>
          <RouterProvider>
            <App />
          </RouterProvider>
        </AuthProvider>
      </ToastProvider>
    </ColorModeProvider>
  </StrictMode>,
)
