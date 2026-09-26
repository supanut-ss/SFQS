import type { ButtonHTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'outline-destructive' | 'destructive' | 'success'

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
}

const variantClass: Record<ButtonVariant, string> = {
  primary: '',
  secondary: 'btn-secondary',
  outline: 'btn-outline',
  'outline-destructive': 'btn-outline-destructive',
  destructive: 'btn-destructive',
  success: 'btn-success',
}

/** design-system-spec.md "Button" + "Sale Approval" actions (success/outline-destructive). */
export function Button({ variant = 'primary', className = '', ...props }: ButtonProps) {
  return <button className={cn('btn', variantClass[variant], className)} {...props} />
}
