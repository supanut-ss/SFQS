import type { HTMLAttributes } from 'react'

export type BadgeVariant =
  | 'import'
  | 'export'
  | 'fcl'
  | 'lcl'
  | 'air'
  | 'success'
  | 'warning'
  | 'destructive'
  | 'info'
  | 'status-in-transit'
  | 'status-delayed'
  | 'status-arrived'
  | 'status-pending-do'

export interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant: BadgeVariant
}

/** Pill badge for direction/mode/status — design-system-spec.md "Badge". */
export function Badge({ variant, className = '', ...props }: BadgeProps) {
  return <span className={`badge badge-${variant} ${className}`.trim()} {...props} />
}
