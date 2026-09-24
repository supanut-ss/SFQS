import { useEffect, useState } from 'react'
import { ComponentGallery } from './ComponentGallery'
import { InstantQuotePage } from './pages/quote/InstantQuotePage'

type HealthStatus = 'checking' | 'ok' | 'error'
type View = 'quote' | 'gallery'

/**
 * T13: the public Instant Quote page is now the real app entry point (ui-plan.md IA page 1-2).
 * No router yet — the component gallery (T11's dev reference) is reachable via a small link
 * at the bottom instead of a route, since it's not a real product page.
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

      <div className="w-full flex justify-center mt-4">{view === 'quote' ? <InstantQuotePage /> : <ComponentGallery />}</div>

      <button
        type="button"
        className="text-xs text-muted-foreground underline mt-8"
        onClick={() => setView(view === 'quote' ? 'gallery' : 'quote')}
      >
        {view === 'quote' ? 'View component gallery (dev reference)' : 'Back to Instant Quote'}
      </button>
    </main>
  )
}

export default App
