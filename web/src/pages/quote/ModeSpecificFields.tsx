import Box from '@mui/material/Box'
import { Input, Select } from '../../components/ui'
import type { TransportMode } from './types'

const CONTAINER_SIZES = ['20', '40', '40HQ']

export interface ModeFieldValues {
  containerSize?: string
  containerQty?: number
  cbm?: number
  weightKg?: number
  actualWeightKg?: number
  volumeCm3?: number
}

export interface ModeSpecificFieldsProps {
  mode: TransportMode
  values: ModeFieldValues
  onChange: (values: ModeFieldValues) => void
}

/** The fields that change per transport mode — technical-plan.md §3: FCL needs a container
 * size/qty, LCL needs CBM+weight (revenue ton), Air needs actual weight + volume
 * (chargeable weight = max(actual, volumetric), see ChargeableWeightCalculator). */
export function ModeSpecificFields({ mode, values, onChange }: ModeSpecificFieldsProps) {
  if (mode === 'fcl') {
    return (
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }}>
        <Select
          label="Container size"
          required
          options={CONTAINER_SIZES.map((size) => ({ value: size, label: size }))}
          placeholder="Select a size"
          value={values.containerSize ?? ''}
          onChange={(e) => onChange({ ...values, containerSize: e.target.value })}
        />
        <Input
          label="Container quantity"
          type="number"
          min={1}
          required
          value={values.containerQty ?? 1}
          onChange={(e) => onChange({ ...values, containerQty: Number(e.target.value) })}
        />
      </Box>
    )
  }

  if (mode === 'lcl') {
    return (
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }}>
        <Input
          label="CBM"
          type="number"
          step="0.001"
          min={0.001}
          required
          value={values.cbm ?? ''}
          onChange={(e) => onChange({ ...values, cbm: Number(e.target.value) })}
        />
        <Input
          label="Weight (kg)"
          type="number"
          step="0.001"
          min={0.001}
          required
          value={values.weightKg ?? ''}
          onChange={(e) => onChange({ ...values, weightKg: Number(e.target.value) })}
        />
      </Box>
    )
  }

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }}>
      <Input
        label="Actual weight (kg)"
        type="number"
        step="0.001"
        min={0.001}
        required
        value={values.actualWeightKg ?? ''}
        onChange={(e) => onChange({ ...values, actualWeightKg: Number(e.target.value) })}
      />
      <Input
        label="Volume (cm³)"
        type="number"
        step="1"
        min={0}
        required
        help="Total shipment volume: sum of L × W × H per piece"
        value={values.volumeCm3 ?? ''}
        onChange={(e) => onChange({ ...values, volumeCm3: Number(e.target.value) })}
      />
    </Box>
  )
}
