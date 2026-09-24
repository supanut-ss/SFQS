import { useEffect, useState } from 'react'
import { ComponentGallery } from './ComponentGallery'

type HealthStatus = 'checking' | 'ok' | 'error'

/**
 * M0 scaffold page: proves the React app is wired to the design tokens and
 * can reach the API. Real pages (Instant Quote, Sale inbox, Operation rate
 * management, ...) are M2/M3 work — see ui-plan.md §4 for the full IA.
 */
function App() {
  const [health, setHealth] = useState<HealthStatus>('checking')

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
    <main className="min-h-dvh flex flex-col items-center gap-4 bg-background text-foreground py-10">
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
      {/* T11 component library — dev-only reference, replaced by real pages in T12-T14. */}
      <ComponentGallery />
    </main>
  )
}

export default App
