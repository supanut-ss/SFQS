import { useEffect, useId, useRef, type KeyboardEvent, type ReactNode } from 'react'

export interface DialogProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
}

const FOCUSABLE_SELECTOR =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'

/** Modal dialog (design-system-spec.md tokens: dialog-*). Traps focus, closes on Escape or
 * overlay click, restores focus to the trigger on close — the baseline WAI-ARIA APG dialog
 * pattern, kept dependency-free since the primitive set has no modal library yet. */
export function Dialog({ open, onClose, title, children }: DialogProps) {
  const titleId = useId()
  const panelRef = useRef<HTMLDivElement>(null)
  const triggerRef = useRef<Element | null>(null)

  useEffect(() => {
    if (!open) return
    triggerRef.current = document.activeElement
    const panel = panelRef.current
    const firstFocusable = panel?.querySelector<HTMLElement>(FOCUSABLE_SELECTOR)
    firstFocusable?.focus()

    return () => {
      if (triggerRef.current instanceof HTMLElement) triggerRef.current.focus()
    }
  }, [open])

  if (!open) return null

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.key === 'Escape') {
      event.stopPropagation()
      onClose()
      return
    }

    if (event.key !== 'Tab' || !panelRef.current) return
    const focusable = Array.from(panelRef.current.querySelectorAll<HTMLElement>(FOCUSABLE_SELECTOR))
    if (focusable.length === 0) return
    const first = focusable[0]
    const last = focusable[focusable.length - 1]

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first.focus()
    }
  }

  return (
    <div className="dialog-overlay" onClick={onClose} onKeyDown={handleKeyDown}>
      <div
        ref={panelRef}
        className="dialog-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id={titleId} className="dialog-title">
          {title}
        </h2>
        {children}
      </div>
    </div>
  )
}
