import React, { useEffect, useState, useTransition } from 'react'
import { RouterContext, type RouterContextValue } from './RouterContext'

function parseHash(hash: string): { path: string; searchParams: URLSearchParams } {
  const clean = hash.replace(/^#\/?/, '')
  const [pathname = '', search = ''] = clean.split('?')
  const normalizedPath = '/' + pathname.replace(/\/+$/, '')
  return {
    path: normalizedPath === '/' ? '/quote' : normalizedPath,
    searchParams: new URLSearchParams(search),
  }
}

export function RouterProvider({ children }: { children: React.ReactNode }) {
  const [currentHash, setCurrentHash] = useState(() => window.location.hash || '#/quote')
  const [, startTransition] = useTransition()

  useEffect(() => {
    if (!window.location.hash) {
      window.location.hash = '#/quote'
    }

    const onHashChange = () => {
      startTransition(() => {
        setCurrentHash(window.location.hash || '#/quote')
      })
    }

    window.addEventListener('hashchange', onHashChange)
    return () => window.removeEventListener('hashchange', onHashChange)
  }, [])

  const { path, searchParams } = parseHash(currentHash)

  const navigate = (to: string) => {
    const formatted = to.startsWith('#') ? to : `#${to.startsWith('/') ? '' : '/'}${to}`
    if (window.location.hash === formatted) return
    window.location.hash = formatted
  }

  const goBack = () => {
    if (window.history.length > 1) {
      window.history.back()
    } else {
      navigate('/quote')
    }
  }

  // Parse route parameters, e.g. /ops/quotations/:id
  const quoteMatch = path.match(/^\/ops\/quotations\/(\d+)$/)
  const params: Record<string, string> = {}
  if (quoteMatch) {
    params.id = quoteMatch[1]
  }

  const value: RouterContextValue = {
    path,
    subPath: path.replace(/^\/ops\/?/, ''),
    params,
    searchParams,
    navigate,
    goBack,
  }

  return <RouterContext.Provider value={value}>{children}</RouterContext.Provider>
}
