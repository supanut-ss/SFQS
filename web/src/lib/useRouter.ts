import { useContext } from 'react'
import { RouterContext, type RouterContextValue } from './RouterContext'

export function useRouter(): RouterContextValue {
  const ctx = useContext(RouterContext)
  if (!ctx) {
    throw new Error('useRouter must be used within a RouterProvider')
  }
  return ctx
}
