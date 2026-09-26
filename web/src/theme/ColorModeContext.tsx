import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { ThemeProvider } from '@mui/material/styles'
import { getTheme } from './theme'

type ColorMode = 'light' | 'dark'

const STORAGE_KEY = 'freito-color-mode'

const ColorModeContext = createContext<{
  mode: ColorMode
  toggleMode: () => void
}>({
  mode: 'light',
  toggleMode: () => {},
})

export function useColorMode() {
  return useContext(ColorModeContext)
}

function applyMode(mode: ColorMode) {
  document.documentElement.classList.toggle('dark', mode === 'dark')
  localStorage.setItem(STORAGE_KEY, mode)
}

function getInitialMode(): ColorMode {
  const stored = localStorage.getItem(STORAGE_KEY)
  const mode = stored === 'light' || stored === 'dark' ? stored : window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  // Apply eagerly so getTheme() reads the right tokens.css values on first render.
  applyMode(mode)
  return mode
}

export function ColorModeProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<ColorMode>(getInitialMode)

  const value = useMemo(
    () => ({
      mode,
      toggleMode: () =>
        setMode((prev) => {
          const next = prev === 'light' ? 'dark' : 'light'
          applyMode(next)
          return next
        }),
    }),
    [],
  )

  const theme = useMemo(() => getTheme(mode), [mode])

  return (
    <ColorModeContext.Provider value={value}>
      <ThemeProvider theme={theme}>{children}</ThemeProvider>
    </ColorModeContext.Provider>
  )
}
