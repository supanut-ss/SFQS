import { createContext } from 'react'

export interface RouterContextValue {
  path: string
  subPath: string
  params: Record<string, string>
  searchParams: URLSearchParams
  navigate: (to: string) => void
  goBack: () => void
}

export const RouterContext = createContext<RouterContextValue | null>(null)
