import type { HTMLAttributes } from 'react'

export interface SkeletonProps extends HTMLAttributes<HTMLSpanElement> {
  width?: string | number
  height?: string | number
}

/** Loading placeholder — design-system-spec.md tokens: skeleton-*. Shimmer respects
 * prefers-reduced-motion (see components.css). aria-hidden since it conveys no content. */
export function Skeleton({ width = '100%', height = '1rem', className = '', style, ...props }: SkeletonProps) {
  return (
    <span
      className={`skeleton ${className}`.trim()}
      style={{ width, height, ...style }}
      aria-hidden="true"
      {...props}
    />
  )
}
