import { useState } from 'react'
import { Button, Dialog, Input, Select } from '../../components/ui'
import type { ChargeCalcBasis, ChargeSide, Currency, LocalCharge, Port, ShipmentDirection, TransportMode } from './types'

export interface LocalChargeFormValues {
  portId: number
  direction: ShipmentDirection
  mode: TransportMode
  chargeType: string
  calcBasis: ChargeCalcBasis
  amountMin: string
  amountMax: string
  minimumCharge: string
  currencyCode: string
  chargeSide: ChargeSide
}

const empty: LocalChargeFormValues = {
  portId: 0,
  direction: 'export',
  mode: 'fcl',
  chargeType: '',
  calcBasis: 'perShipment',
  amountMin: '',
  amountMax: '',
  minimumCharge: '',
  currencyCode: '',
  chargeSide: 'origin',
}

const CALC_BASIS_OPTIONS: { value: ChargeCalcBasis; label: string }[] = [
  { value: 'perShipment', label: 'Per shipment' },
  { value: 'perContainer', label: 'Per container' },
  { value: 'perRevenueTon', label: 'Per revenue ton (LCL)' },
  { value: 'perCBM', label: 'Per CBM (LCL)' },
  { value: 'perKG', label: 'Per KG' },
  { value: 'notQuotable', label: 'Not quotable (shown as a note, excluded from total)' },
]

export interface LocalChargeFormDialogProps {
  open: boolean
  onClose: () => void
  onSubmit: (values: LocalChargeFormValues) => Promise<void>
  ports: Port[]
  currencies: Currency[]
  editing?: LocalCharge | null
}

/** design-system-spec.md / technical-plan.md §2: NotQuotable charges (DDP-style customs duty,
 * at-cost, time-based) are excluded from the quoted total and shown as a disclaimer instead —
 * the calc basis dropdown spells that out so Operation doesn't need to remember it. */
export function LocalChargeFormDialog({ open, onClose, onSubmit, ports, currencies, editing }: LocalChargeFormDialogProps) {
  const [values, setValues] = useState<LocalChargeFormValues>(
    editing
      ? {
          portId: editing.portId,
          direction: editing.direction,
          mode: editing.mode,
          chargeType: editing.chargeType,
          calcBasis: editing.calcBasis,
          amountMin: editing.amountMin.toString(),
          amountMax: editing.amountMax.toString(),
          minimumCharge: editing.minimumCharge?.toString() ?? '',
          currencyCode: editing.currencyCode,
          chargeSide: editing.chargeSide,
        }
      : empty,
  )
  const [submitting, setSubmitting] = useState(false)

  const modePorts = ports.filter((p) => (values.mode === 'air' ? p.type === 'air' : p.type === 'sea'))

  const handleSubmit = async () => {
    setSubmitting(true)
    try {
      await onSubmit(values)
      onClose()
    } catch {
      // onSubmit already surfaced a toast — swallow here so a failed save doesn't become an
      // unhandled promise rejection; the dialog just stays open so the user can retry.
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onClose={onClose} title={editing ? 'Edit local charge' : 'New local charge'}>
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
            onChange={(e) => setValues({ ...values, mode: e.target.value as TransportMode })}
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
            label="Port"
            placeholder="Select port"
            options={modePorts.map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
            value={values.portId ? String(values.portId) : ''}
            onChange={(e) => setValues({ ...values, portId: Number(e.target.value) })}
          />
          <Select
            label="Charge side"
            options={[
              { value: 'origin', label: 'Origin' },
              { value: 'destination', label: 'Destination' },
            ]}
            value={values.chargeSide}
            onChange={(e) => setValues({ ...values, chargeSide: e.target.value as ChargeSide })}
          />
        </div>
        <Input label="Charge type" placeholder="THC, D/O, CFS, ..." value={values.chargeType} onChange={(e) => setValues({ ...values, chargeType: e.target.value })} />
        <Select
          label="Calculation basis"
          options={CALC_BASIS_OPTIONS}
          value={values.calcBasis}
          onChange={(e) => setValues({ ...values, calcBasis: e.target.value as ChargeCalcBasis })}
        />

        {values.calcBasis !== 'notQuotable' && (
          <>
            <div className="form-row">
              <Input label="Amount min" type="number" step="0.0001" value={values.amountMin} onChange={(e) => setValues({ ...values, amountMin: e.target.value })} />
              <Input label="Amount max" type="number" step="0.0001" value={values.amountMax} onChange={(e) => setValues({ ...values, amountMax: e.target.value })} />
            </div>
            <Input
              label="Minimum charge (optional floor)"
              type="number"
              step="0.0001"
              value={values.minimumCharge}
              onChange={(e) => setValues({ ...values, minimumCharge: e.target.value })}
            />
          </>
        )}
        <Select
          label="Currency"
          placeholder="Select currency"
          options={currencies.map((c) => ({ value: c.code, label: `${c.code} — ${c.name}` }))}
          value={values.currencyCode}
          onChange={(e) => setValues({ ...values, currencyCode: e.target.value })}
        />

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
