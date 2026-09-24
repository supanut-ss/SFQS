import { useState } from 'react'
import { Button, Dialog, Input, Select } from '../../components/ui'
import type { Carrier, Currency, FreightRate, Port, ShipmentDirection, TransportMode } from './types'

export interface RateFormValues {
  originPortId: number
  destinationPortId: number
  mode: TransportMode
  direction: ShipmentDirection
  carrierId: number
  containerSize: string
  weightBreakMin: string
  weightBreakMax: string
  priceMin: string
  priceMax: string
  currencyCode: string
  validFrom: string
  validTo: string
}

const empty: RateFormValues = {
  originPortId: 0,
  destinationPortId: 0,
  mode: 'fcl',
  direction: 'export',
  carrierId: 0,
  containerSize: '',
  weightBreakMin: '',
  weightBreakMax: '',
  priceMin: '',
  priceMax: '',
  currencyCode: '',
  validFrom: '',
  validTo: '',
}

export interface RateFormDialogProps {
  open: boolean
  onClose: () => void
  onSubmit: (values: RateFormValues) => Promise<void>
  ports: Port[]
  carriers: Carrier[]
  currencies: Currency[]
  editing?: FreightRate | null
}

/** Create (and revise — technical-plan.md: a rate update always inserts a new validity-dated
 * row, never overwrites in place, so past quotes' snapshots stay correct) form for FreightRate.
 * Mode gates which fields apply, mirroring the same rule QuotationService/RateResolver
 * enforce server-side (FCL needs a container size, Air needs a weight bracket, LCL needs
 * neither). */
export function RateFormDialog({ open, onClose, onSubmit, ports, carriers, currencies, editing }: RateFormDialogProps) {
  const [values, setValues] = useState<RateFormValues>(
    editing
      ? {
          originPortId: editing.originPortId,
          destinationPortId: editing.destinationPortId,
          mode: editing.mode,
          direction: editing.direction,
          carrierId: editing.carrierId,
          containerSize: editing.containerSize ?? '',
          weightBreakMin: editing.weightBreakMin?.toString() ?? '',
          weightBreakMax: editing.weightBreakMax?.toString() ?? '',
          priceMin: editing.priceMin.toString(),
          priceMax: editing.priceMax.toString(),
          currencyCode: editing.currencyCode,
          validFrom: editing.validFrom.slice(0, 10),
          validTo: editing.validTo.slice(0, 10),
        }
      : empty,
  )
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [submitting, setSubmitting] = useState(false)

  const modePorts = ports.filter((p) => (values.mode === 'air' ? p.type === 'air' : p.type === 'sea'))
  const modeCarriers = carriers.filter((c) => (values.mode === 'air' ? c.type === 'airline' : c.type === 'shippingLine'))

  const validate = (): boolean => {
    const errs: Record<string, string> = {}
    if (!values.originPortId) {
      errs.originPortId = 'Origin port is required'
    }
    if (!values.destinationPortId) {
      errs.destinationPortId = 'Destination port is required'
    } else if (values.originPortId && values.destinationPortId === values.originPortId) {
      errs.destinationPortId = 'Destination must be different from origin'
    }
    if (!values.carrierId) {
      errs.carrierId = 'Carrier is required'
    }
    if (values.mode === 'fcl' && !values.containerSize) {
      errs.containerSize = 'Container size is required'
    }
    if (values.mode === 'air') {
      if (values.weightBreakMin !== '' && Number(values.weightBreakMin) < 0) {
        errs.weightBreakMin = 'Weight break min must be ≥ 0'
      }
      if (values.weightBreakMax === '') {
        errs.weightBreakMax = 'Weight break max is required'
      } else if (Number(values.weightBreakMax) <= 0) {
        errs.weightBreakMax = 'Weight break max must be > 0'
      } else if (values.weightBreakMin !== '' && Number(values.weightBreakMin) > Number(values.weightBreakMax)) {
        errs.weightBreakMax = 'Max weight must be ≥ min weight'
      }
    }
    if (values.priceMin === '') {
      errs.priceMin = 'Price min is required'
    } else if (Number(values.priceMin) < 0) {
      errs.priceMin = 'Price must be non-negative'
    }
    if (values.priceMax === '') {
      errs.priceMax = 'Price max is required'
    } else if (Number(values.priceMax) < 0) {
      errs.priceMax = 'Price must be non-negative'
    } else if (values.priceMin !== '' && Number(values.priceMax) < Number(values.priceMin)) {
      errs.priceMax = 'Price max must be ≥ price min'
    }
    if (!values.currencyCode) {
      errs.currencyCode = 'Currency is required'
    }
    if (!values.validFrom) {
      errs.validFrom = 'Valid from date is required'
    }
    if (!values.validTo) {
      errs.validTo = 'Valid to date is required'
    } else if (values.validFrom && values.validTo < values.validFrom) {
      errs.validTo = 'Valid to date cannot be before valid from'
    }

    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  const handleSubmit = async () => {
    if (!validate()) return
    setSubmitting(true)
    try {
      await onSubmit(values)
      onClose()
    } catch {
      // onSubmit already surfaced a toast
    } finally {
      setSubmitting(false)
    }
  }

  const clearError = (field: string) => {
    if (errors[field]) {
      setErrors((prev) => {
        const next = { ...prev }
        delete next[field]
        return next
      })
    }
  }

  return (
    <Dialog open={open} onClose={onClose} title={editing ? 'Revise rate' : 'New rate'}>
      <div className="flex flex-col gap-4">
        <div className="form-row">
          <Select
            label="Mode"
            options={[
              { value: 'fcl', label: 'FCL' },
              { value: 'lcl', label: 'LCL' },
              { value: 'air', label: 'Air' },
            ]}
            value={values.mode}
            onChange={(e) => {
              setValues({ ...values, mode: e.target.value as TransportMode, containerSize: '', weightBreakMin: '', weightBreakMax: '' })
              setErrors({})
            }}
          />
          <Select
            label="Direction"
            options={[
              { value: 'export', label: 'Export' },
              { value: 'import', label: 'Import' },
            ]}
            value={values.direction}
            onChange={(e) => setValues({ ...values, direction: e.target.value as ShipmentDirection })}
          />
        </div>
        <div className="form-row">
          <Select
            label="Origin"
            placeholder="Select origin"
            error={errors.originPortId}
            options={modePorts.map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
            value={values.originPortId ? String(values.originPortId) : ''}
            onChange={(e) => {
              setValues({ ...values, originPortId: Number(e.target.value) })
              clearError('originPortId')
            }}
          />
          <Select
            label="Destination"
            placeholder="Select destination"
            error={errors.destinationPortId}
            options={modePorts.filter((p) => p.id !== values.originPortId).map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
            value={values.destinationPortId ? String(values.destinationPortId) : ''}
            onChange={(e) => {
              setValues({ ...values, destinationPortId: Number(e.target.value) })
              clearError('destinationPortId')
            }}
          />
        </div>
        <Select
          label="Carrier"
          placeholder="Select carrier"
          error={errors.carrierId}
          options={modeCarriers.map((c) => ({ value: String(c.id), label: c.name }))}
          value={values.carrierId ? String(values.carrierId) : ''}
          onChange={(e) => {
            setValues({ ...values, carrierId: Number(e.target.value) })
            clearError('carrierId')
          }}
        />

        {values.mode === 'fcl' && (
          <Select
            label="Container size"
            placeholder="Select size"
            error={errors.containerSize}
            options={['20', '40', '40HQ'].map((s) => ({ value: s, label: s }))}
            value={values.containerSize}
            onChange={(e) => {
              setValues({ ...values, containerSize: e.target.value })
              clearError('containerSize')
            }}
          />
        )}
        {values.mode === 'air' && (
          <div className="form-row">
            <Input
              label="Weight break min (kg)"
              type="number"
              error={errors.weightBreakMin}
              value={values.weightBreakMin}
              onChange={(e) => {
                setValues({ ...values, weightBreakMin: e.target.value })
                clearError('weightBreakMin')
              }}
            />
            <Input
              label="Weight break max (kg)"
              type="number"
              error={errors.weightBreakMax}
              value={values.weightBreakMax}
              onChange={(e) => {
                setValues({ ...values, weightBreakMax: e.target.value })
                clearError('weightBreakMax')
              }}
            />
          </div>
        )}

        <div className="form-row">
          <Input
            label="Price min"
            type="number"
            step="0.0001"
            error={errors.priceMin}
            value={values.priceMin}
            onChange={(e) => {
              setValues({ ...values, priceMin: e.target.value })
              clearError('priceMin')
            }}
          />
          <Input
            label="Price max"
            type="number"
            step="0.0001"
            error={errors.priceMax}
            value={values.priceMax}
            onChange={(e) => {
              setValues({ ...values, priceMax: e.target.value })
              clearError('priceMax')
            }}
          />
        </div>
        <Select
          label="Currency"
          placeholder="Select currency"
          error={errors.currencyCode}
          options={currencies.map((c) => ({ value: c.code, label: `${c.code} — ${c.name}` }))}
          value={values.currencyCode}
          onChange={(e) => {
            setValues({ ...values, currencyCode: e.target.value })
            clearError('currencyCode')
          }}
        />
        <div className="form-row">
          <Input
            label="Valid from"
            type="date"
            error={errors.validFrom}
            value={values.validFrom}
            onChange={(e) => {
              setValues({ ...values, validFrom: e.target.value })
              clearError('validFrom')
            }}
          />
          <Input
            label="Valid to"
            type="date"
            error={errors.validTo}
            value={values.validTo}
            onChange={(e) => {
              setValues({ ...values, validTo: e.target.value })
              clearError('validTo')
            }}
          />
        </div>

        <div className="flex justify-end gap-3 mt-2">
          <Button variant="outline" onClick={onClose}>
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={submitting}>
            {submitting ? 'Saving…' : 'Save'}
          </Button>
        </div>
      </div>
    </Dialog>
  )
}
