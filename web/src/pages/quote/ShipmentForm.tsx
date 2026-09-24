import { Button, SegmentedControl, Select } from '../../components/ui'
import { Input } from '../../components/ui'
import { ModeSpecificFields, type ModeFieldValues } from './ModeSpecificFields'
import type { Incoterm, Port, QuoteShipmentInput, ShipmentDirection, TransportMode } from './types'

export interface ShipmentFormProps {
  value: QuoteShipmentInput
  onChange: (value: QuoteShipmentInput) => void
  seaPorts: Port[]
  airPorts: Port[]
  incoterms: Incoterm[]
  onSubmit: () => void
  submitting: boolean
  submitLabel?: string
}

/** design-system-spec.md "Instant Quote Form": segmented mode control (not a dropdown, since
 * it drives which fields show), field pairs per row for density. */
export function ShipmentForm({
  value,
  onChange,
  seaPorts,
  airPorts,
  incoterms,
  onSubmit,
  submitting,
  submitLabel = 'Calculate quote',
}: ShipmentFormProps) {
  const ports = value.mode === 'air' ? airPorts : seaPorts

  const setMode = (mode: TransportMode) => {
    // Clear mode-specific fields on switch — a leftover CBM shouldn't silently apply to Air.
    onChange({
      ...value,
      mode,
      containerSize: undefined,
      containerQty: undefined,
      cbm: undefined,
      weightKg: undefined,
      actualWeightKg: undefined,
      volumeCm3: undefined,
    })
  }

  const handleModeFieldsChange = (fields: ModeFieldValues) => onChange({ ...value, ...fields })

  return (
    <form
      className="quote-form"
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit()
      }}
    >
      <SegmentedControl
        label="Transport mode"
        value={value.mode}
        onChange={setMode}
        options={[
          { value: 'fcl', label: 'FCL' },
          { value: 'lcl', label: 'LCL' },
          { value: 'air', label: 'Air' },
        ]}
      />

      <div className="form-row">
        <Select
          label="Origin"
          required
          placeholder="Select origin"
          options={ports.map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
          value={value.originPortId ? String(value.originPortId) : ''}
          onChange={(e) => onChange({ ...value, originPortId: Number(e.target.value) })}
        />
        <Select
          label="Destination"
          required
          placeholder="Select destination"
          options={ports.filter((p) => p.id !== value.originPortId).map((p) => ({ value: String(p.id), label: `${p.name} (${p.code})` }))}
          value={value.destinationPortId ? String(value.destinationPortId) : ''}
          onChange={(e) => onChange({ ...value, destinationPortId: Number(e.target.value) })}
        />
      </div>

      <div className="form-row">
        <Select
          label="Direction"
          required
          options={[
            { value: 'export', label: 'Export (I am the seller)' },
            { value: 'import', label: 'Import (I am the buyer)' },
          ]}
          value={value.direction}
          onChange={(e) => onChange({ ...value, direction: e.target.value as ShipmentDirection })}
        />
        <Select
          label="Incoterm"
          required
          placeholder="Select an Incoterm"
          options={incoterms.map((i) => ({ value: i.code, label: `${i.code} — ${i.name}` }))}
          value={value.incotermCode}
          onChange={(e) => onChange({ ...value, incotermCode: e.target.value })}
        />
      </div>

      <ModeSpecificFields mode={value.mode} values={value} onChange={handleModeFieldsChange} />

      <div className="form-row">
        <Input
          label="Ready date"
          type="date"
          required
          value={value.readyDate}
          onChange={(e) => onChange({ ...value, readyDate: e.target.value })}
        />
      </div>

      <Button type="submit" style={{ width: '100%' }} disabled={submitting}>
        {submitting ? 'Calculating…' : submitLabel}
      </Button>
    </form>
  )
}
