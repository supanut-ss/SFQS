import type { ReactNode } from 'react'
import MuiDialog from '@mui/material/Dialog'
import DialogTitle from '@mui/material/DialogTitle'
import DialogContent from '@mui/material/DialogContent'

export interface DialogProps {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
}

/** Modal dialog (design-system-spec.md tokens: dialog-*). Wraps MUI Dialog, which already
 * implements the WAI-ARIA APG dialog pattern (focus trap, Escape/backdrop close, focus
 * restoration on close) instead of hand-rolling it. */
export function Dialog({ open, onClose, title, children }: DialogProps) {
  return (
    <MuiDialog
      open={open}
      onClose={onClose}
      slotProps={{ paper: { className: 'dialog-panel' } }}
    >
      <DialogTitle className="dialog-title">{title}</DialogTitle>
      <DialogContent>{children}</DialogContent>
    </MuiDialog>
  )
}
