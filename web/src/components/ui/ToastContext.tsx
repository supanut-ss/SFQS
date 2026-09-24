import { createContext } from 'react'

export type ToastVariant = 'default' | 'success' | 'destructive'

export interface ToastContextValue {
  show: (message: string, variant?: ToastVariant) => void
}

export const ToastContext = createContext<ToastContextValue | null>(null)
