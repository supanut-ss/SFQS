import { useEffect, useState } from 'react'
import { InstantQuotePage } from './pages/quote/InstantQuotePage'
import { OpsApp } from './pages/ops/OpsApp'
import { useRouter } from './lib/useRouter'
import { useColorMode } from './theme/ColorModeContext'
import { cn } from './lib/cn'

type HealthStatus = 'checking' | 'ok' | 'error'

/**
 * T13/T12: the public Instant Quote page and the internal ops area (Rate/Local charge/Master
 * data management, gated by login) are navigated via client hash router with deep link support.
 */
function App() {
  const [health, setHealth] = useState<HealthStatus>('checking')
  const { path, navigate } = useRouter()
  const isOps = path.startsWith('/ops')
  const { mode, toggleMode } = useColorMode()

  useEffect(() => {
    let cancelled = false
    fetch('/api/health')
      .then((res) => {
        if (!cancelled) setHealth(res.ok ? 'ok' : 'error')
      })
      .catch(() => {
        if (!cancelled) setHealth('error')
      })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <main className="min-h-dvh flex flex-col items-center gap-4 bg-background text-foreground py-10 px-4 relative">
      <button
        type="button"
        onClick={toggleMode}
        aria-label="Toggle dark mode"
        className="absolute top-4 right-4 text-xs px-3 py-1.5 rounded-full border border-border text-foreground hover:bg-muted"
      >
        {mode === 'dark' ? '☀️ Light' : '🌙 Dark'}
      </button>
      <img src="/logo-icon.png" alt="Freito logo" className="h-10 w-auto" />
      <h1 className="font-heading text-2xl font-semibold">Freito</h1>
      <p className="text-sm text-muted-foreground">Smart Freight Quotation System</p>
      <p className="text-sm">
        API:{' '}
        <span
          className={
            health === 'ok'
              ? 'text-success font-semibold'
              : health === 'error'
                ? 'text-destructive font-semibold'
                : 'text-muted-foreground'
          }
        >
          {health === 'checking' ? 'checking…' : health === 'ok' ? 'reachable' : 'unreachable'}
        </span>
      </p>

      <div className="w-full flex justify-center mt-4">
        {!isOps && <InstantQuotePage />}
        {isOps && <OpsApp />}
      </div>

      <div className="flex gap-4 mt-8">
        <button
          type="button"
          className={cn('text-xs underline', !isOps ? 'font-semibold text-foreground' : 'text-muted-foreground')}
          onClick={() => navigate('/quote')}
        >
          Instant Quote
        </button>
        <button
          type="button"
          className={cn('text-xs underline', isOps ? 'font-semibold text-foreground' : 'text-muted-foreground')}
          onClick={() => navigate('/ops')}
        >
          Operation / Admin sign in
        </button>
      </div>
    </main>
  )
}

export default App
