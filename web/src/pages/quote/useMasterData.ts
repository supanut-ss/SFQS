import { useEffect, useState } from 'react'
import { api } from '../../lib/api'
import type { CargoType, Incoterm, Port } from './types'

interface MasterData {
  seaPorts: Port[]
  airPorts: Port[]
  incoterms: Incoterm[]
  cargoTypes: CargoType[]
  loading: boolean
  error: string | null
}

/** Loads the reference data the guest form needs — all public GET endpoints (T7's
 * PublicQuotesController and T4's MasterDataController leave these three unauthenticated). */
export function useMasterData(): MasterData {
  const [seaPorts, setSeaPorts] = useState<Port[]>([])
  const [airPorts, setAirPorts] = useState<Port[]>([])
  const [incoterms, setIncoterms] = useState<Incoterm[]>([])
  const [cargoTypes, setCargoTypes] = useState<CargoType[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    Promise.all([
      api.get<Port[]>('/api/master/ports?type=Sea'),
      api.get<Port[]>('/api/master/ports?type=Air'),
      api.get<Incoterm[]>('/api/master/incoterms'),
      api.get<CargoType[]>('/api/master/cargo-types'),
    ])
      .then(([sea, air, incotermList, cargoTypeList]) => {
        if (cancelled) return
        setSeaPorts(sea)
        setAirPorts(air)
        setIncoterms(incotermList)
        setCargoTypes(cargoTypeList)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Failed to load reference data')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  return { seaPorts, airPorts, incoterms, cargoTypes, loading, error }
}
