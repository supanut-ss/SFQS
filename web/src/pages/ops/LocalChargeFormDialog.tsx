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
  const [errors, setErrors] = useState<Record<string, string>>({})
  const [submitting, setSubmitting] = useState(false)

  const modePorts = ports.filter((p) => (values.mode === 'air' ? p.type === 'air' : p.type === 'sea'))

  const validate = (): boolean => {
    const errs: Record<string, string> = {}
    if (!values.portId) {
      errs.portId = 'Port is required'
    }
    if (!values.chargeType.trim()) {
      errs.chargeType = 'Charge type is required'
    }
    if (values.calcBasis !== 'notQuotable') {
      if (values.amountMin === '') {
        errs.amountMin = 'Minimum amount is required'
      } else if (Number(values.amountMin) < 0) {
        errs.amountMin = 'Amount must be non-negative'
      }
      if (values.amountMax === '') {
        errs.amountMax = 'Maximum amount is required'
      } else if (Number(values.amountMax) < 0) {
        errs.amountMax = 'Amount must be non-negative'
      } else if (values.amountMin !== '' && Number(values.amountMax) < Number(values.amountMin)) {
        errs.amountMax = 'Max amount must be ≥ min amount'
      }
      if (values.minimumCharge !== '' && Number(values.minimumCharge) < 0) {
        errs.minimumCharge = 'Minimum charge floor must be ≥ 0'
      }
    }
    if (!values.currencyCode) {
      errs.currencyCode = 'Currency is required'
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
            onChange={(e) => {
              setValues({ ...values, mode: e.target.value as TransportMode })
              clearError('portId')
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
            label="Port"
            placeholder="Select port"
            error={errors.portId}
            options={modePorts.map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
            value={values.portId ? String(values.portId) : ''}
            onChange={(e) => {
              setValues({ ...values, portId: Number(e.target.value) })
              clearError('portId')
            }}
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
        <Input
          label="Charge type"
          placeholder="THC, D/O, CFS, ..."
          error={errors.chargeType}
          value={values.chargeType}
          onChange={(e) => {
            setValues({ ...values, chargeType: e.target.value })
            clearError('chargeType')
          }}
        />
        <Select
          label="Calculation basis"
          options={CALC_BASIS_OPTIONS}
          value={values.calcBasis}
          onChange={(e) => {
            setValues({ ...values, calcBasis: e.target.value as ChargeCalcBasis })
            clearError('amountMin')
            clearError('amountMax')
          }}
        />

        {values.calcBasis !== 'notQuotable' && (
          <>
            <div className="form-row">
              <Input
                label="Amount min"
                type="number"
                step="0.0001"
                error={errors.amountMin}
                value={values.amountMin}
                onChange={(e) => {
                  setValues({ ...values, amountMin: e.target.value })
                  clearError('amountMin')
                }}
              />
              <Input
                label="Amount max"
                type="number"
                step="0.0001"
                error={errors.amountMax}
                value={values.amountMax}
                onChange={(e) => {
                  setValues({ ...values, amountMax: e.target.value })
                  clearError('amountMax')
                }}
              />
            </div>
            <Input
              label="Minimum charge (optional floor)"
              type="number"
              step="0.0001"
              error={errors.minimumCharge}
              value={values.minimumCharge}
              onChange={(e) => {
                setValues({ ...values, minimumCharge: e.target.value })
                clearError('minimumCharge')
              }}
            />
          </>
        )}
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
