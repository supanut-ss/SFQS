import { useEffect, useState } from 'react'
import { InstantQuotePage } from './pages/quote/InstantQuotePage'
import { OpsApp } from './pages/ops/OpsApp'

type HealthStatus = 'checking' | 'ok' | 'error'
type View = 'quote' | 'ops'

/**
 * T13/T12: the public Instant Quote page and the internal ops area (Rate/Local charge/Master
 * data management, gated by login) are the two real app surfaces so far (ui-plan.md IA). No
 * router yet — switched via a small link at the bottom.
 */
function App() {
  const [health, setHealth] = useState<HealthStatus>('checking')
  const [view, setView] = useState<View>('quote')

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
    <main className="min-h-dvh flex flex-col items-center gap-4 bg-background text-foreground py-10 px-4">
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
        {view === 'quote' && <InstantQuotePage />}
        {view === 'ops' && <OpsApp />}
      </div>

      <div className="flex gap-4 mt-8">
        <button type="button" className="text-xs text-muted-foreground underline" onClick={() => setView('quote')}>
          Instant Quote
        </button>
        <button type="button" className="text-xs text-muted-foreground underline" onClick={() => setView('ops')}>
          Operation / Admin sign in
        </button>
      </div>
    </main>
  )
}

export default App
