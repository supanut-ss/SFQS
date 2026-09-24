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
  const [submitting, setSubmitting] = useState(false)

  const modePorts = ports.filter((p) => (values.mode === 'air' ? p.type === 'air' : p.type === 'sea'))
  const modeCarriers = carriers.filter((c) => (values.mode === 'air' ? c.type === 'airline' : c.type === 'shippingLine'))

  const handleSubmit = async () => {
    setSubmitting(true)
    try {
      await onSubmit(values)
      onClose()
    } catch {
      // onSubmit already surfaced a toast — swallow here so a failed save (validation
      // conflict, network error) doesn't become an unhandled promise rejection; the dialog
      // just stays open so the user can fix the input and retry.
    } finally {
      setSubmitting(false)
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
            onChange={(e) => setValues({ ...values, mode: e.target.value as TransportMode, containerSize: '', weightBreakMin: '', weightBreakMax: '' })}
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
            options={modePorts.map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
            value={values.originPortId ? String(values.originPortId) : ''}
            onChange={(e) => setValues({ ...values, originPortId: Number(e.target.value) })}
          />
          <Select
            label="Destination"
            placeholder="Select destination"
            options={modePorts.filter((p) => p.id !== values.originPortId).map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
            value={values.destinationPortId ? String(values.destinationPortId) : ''}
            onChange={(e) => setValues({ ...values, destinationPortId: Number(e.target.value) })}
          />
        </div>
        <Select
          label="Carrier"
          placeholder="Select carrier"
          options={modeCarriers.map((c) => ({ value: String(c.id), label: c.name }))}
          value={values.carrierId ? String(values.carrierId) : ''}
          onChange={(e) => setValues({ ...values, carrierId: Number(e.target.value) })}
        />

        {values.mode === 'fcl' && (
          <Select
            label="Container size"
            placeholder="Select size"
            options={['20', '40', '40HQ'].map((s) => ({ value: s, label: s }))}
            value={values.containerSize}
            onChange={(e) => setValues({ ...values, containerSize: e.target.value })}
          />
        )}
        {values.mode === 'air' && (
          <div className="form-row">
            <Input
              label="Weight break min (kg)"
              type="number"
              value={values.weightBreakMin}
              onChange={(e) => setValues({ ...values, weightBreakMin: e.target.value })}
            />
            <Input
              label="Weight break max (kg)"
              type="number"
              value={values.weightBreakMax}
              onChange={(e) => setValues({ ...values, weightBreakMax: e.target.value })}
            />
          </div>
        )}

        <div className="form-row">
          <Input label="Price min" type="number" step="0.0001" value={values.priceMin} onChange={(e) => setValues({ ...values, priceMin: e.target.value })} />
          <Input label="Price max" type="number" step="0.0001" value={values.priceMax} onChange={(e) => setValues({ ...values, priceMax: e.target.value })} />
        </div>
        <Select
          label="Currency"
          placeholder="Select currency"
          options={currencies.map((c) => ({ value: c.code, label: `${c.code} — ${c.name}` }))}
          value={values.currencyCode}
          onChange={(e) => setValues({ ...values, currencyCode: e.target.value })}
        />
        <div className="form-row">
          <Input label="Valid from" type="date" value={values.validFrom} onChange={(e) => setValues({ ...values, validFrom: e.target.value })} />
          <Input label="Valid to" type="date" value={values.validTo} onChange={(e) => setValues({ ...values, validTo: e.target.value })} />
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
