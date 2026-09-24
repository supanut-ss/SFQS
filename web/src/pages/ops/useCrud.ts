import { useCallback, useEffect, useState } from 'react'
import { ApiError, api } from '../../lib/api'
import type { Paged } from './types'

/** Generic list+CRUD state for the ops pages — every resource here (rates, local charges,
 * ports, carriers, ...) follows the same GET-list/POST/PUT/DELETE shape from T4's controllers,
 * so one hook covers all of them instead of duplicating fetch/loading/error handling per page.
 * `paged` matches whether the list endpoint returns {items,page,pageSize,total} (rates, local
 * charges) or a bare array (ports, carriers, currencies, incoterms, cargo types). */
export function useCrud<T>(basePath: string, paged = true) {
  const [items, setItems] = useState<T[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const reload = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      if (paged) {
        const result = await api.get<Paged<T>>(basePath)
        setItems(result.items)
      } else {
        setItems(await api.get<T[]>(basePath))
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to load data')
    } finally {
      setLoading(false)
    }
  }, [basePath, paged])

  useEffect(() => {
    reload()
  }, [reload])

  const create = async (payload: unknown) => {
    await api.post(basePath, payload)
    await reload()
  }

  const update = async (id: string | number, payload: unknown) => {
    await api.put(`${basePath}/${id}`, payload)
    await reload()
  }

  const remove = async (id: string | number) => {
    await api.del(`${basePath}/${id}`)
    await reload()
  }

  return { items, loading, error, reload, create, update, remove, setItems }
}
