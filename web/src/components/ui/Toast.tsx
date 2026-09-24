import { useCallback, useMemo, useRef, useState, type ReactNode } from 'react'
import { ToastContext, type ToastVariant } from './ToastContext'

interface ToastItem {
  id: number
  message: string
  variant: ToastVariant
}

const AUTO_DISMISS_MS = 5000

/** Toast provider — mount once near the app root. design-system-spec.md tokens: toast-*.
 * aria-live="polite" region so screen readers announce new toasts without stealing focus. */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const nextId = useRef(0)

  const dismiss = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  const show = useCallback(
    (message: string, variant: ToastVariant = 'default') => {
      const id = nextId.current++
      setToasts((current) => [...current, { id, message, variant }])
      setTimeout(() => dismiss(id), AUTO_DISMISS_MS)
    },
    [dismiss],
  )

  const value = useMemo(() => ({ show }), [show])

  return (
    <ToastContext.Provider value={value}>
      {children}
      <div className="toast-viewport" role="region" aria-live="polite" aria-label="Notifications">
        {toasts.map((toast) => (
          <div key={toast.id} className={`toast ${toast.variant !== 'default' ? `toast-${toast.variant}` : ''}`.trim()}>
            <span>{toast.message}</span>
            <button type="button" className="toast-dismiss" aria-label="Dismiss" onClick={() => dismiss(toast.id)}>
              ×
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
