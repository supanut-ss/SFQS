import { useState, type ReactNode } from 'react'
import { Button } from './Button'
import { Dialog } from './Dialog'

export interface ConfirmDialogProps {
  open: boolean
  onClose: () => void
  onConfirm: () => Promise<void> | void
  title: string
  description?: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  variant?: 'destructive' | 'primary'
  busy?: boolean
}

/**
 * Accessible confirmation dialog replacing window.confirm() per ui-plan.md §5 #69.
 * Traps focus, handles Escape/backdrop clicks, and supports async confirm operations.
 */
export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'destructive',
  busy: propBusy = false,
}: ConfirmDialogProps) {
  const [internalBusy, setInternalBusy] = useState(false)
  const busy = propBusy || internalBusy

  const handleConfirm = async () => {
    try {
      setInternalBusy(true)
      await onConfirm()
      onClose()
    } finally {
      setInternalBusy(false)
    }
  }

  return (
    <Dialog open={open} onClose={busy ? () => {} : onClose} title={title}>
      <div className="flex flex-col gap-4">
        {description && <div className="text-sm text-muted-foreground">{description}</div>}
        <div className="flex justify-end gap-3 mt-2">
          <Button variant="outline" onClick={onClose} disabled={busy}>
            {cancelLabel}
          </Button>
          <Button
            variant={variant === 'destructive' ? 'destructive' : 'primary'}
            onClick={handleConfirm}
            disabled={busy}
          >
            {busy ? 'Processing…' : confirmLabel}
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
