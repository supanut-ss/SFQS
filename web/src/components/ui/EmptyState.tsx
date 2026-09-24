import type { ReactNode } from 'react'

export interface EmptyStateProps {
  title: string
  description?: string
  action?: ReactNode
  icon?: ReactNode
}

const defaultIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.5} aria-hidden="true">
    <path strokeLinecap="round" strokeLinejoin="round" d="M3 7.5 12 3l9 4.5M3 7.5v9L12 21m-9-4.5L12 21m0-13.5 9 4.5M12 21l9-4.5v-9" />
  </svg>
)

/** Empty state for tables/lists with no rows — design-system-spec.md tokens: empty-state-*. */
export function EmptyState({ title, description, action, icon = defaultIcon }: EmptyStateProps) {
  return (
    <div className="empty-state">
      {icon}
      <h3>{title}</h3>
      {description && <p>{description}</p>}
      {action}
    </div>
  )
}
