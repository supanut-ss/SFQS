import { useState } from 'react'
import Box from '@mui/material/Box'
import { Button, Input, Select } from '../../components/ui'
import type { CargoType } from './types'

export interface CustomerDetails {
  customerName: string
  customerCompany: string
  customerEmail: string
  customerPhone: string
  cargoTypeId: number | undefined
}

export interface CustomerDetailsFormProps {
  cargoTypes: CargoType[]
  onSubmit: (details: CustomerDetails) => void
  submitting: boolean
}

const initial: CustomerDetails = {
  customerName: '',
  customerCompany: '',
  customerEmail: '',
  customerPhone: '',
  cargoTypeId: undefined,
}

/** Step 2 (ui-plan.md IA page 2, "Quote result / ส่งคำขอ"): contact details so Sale can reach
 * the guest — the quote itself is already priced, this only creates the Draft to queue it. */
export function CustomerDetailsForm({ cargoTypes, onSubmit, submitting }: CustomerDetailsFormProps) {
  const [details, setDetails] = useState<CustomerDetails>(initial)

  return (
    <Box
      component="form"
      className="quote-form"
      sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}
      onSubmit={(e) => {
        e.preventDefault()
        onSubmit(details)
      }}
    >
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }}>
        <Input
          label="Your name"
          required
          value={details.customerName}
          onChange={(e) => setDetails({ ...details, customerName: e.target.value })}
        />
        <Input
          label="Company"
          value={details.customerCompany}
          onChange={(e) => setDetails({ ...details, customerCompany: e.target.value })}
        />
      </Box>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }}>
        <Input
          label="Email"
          type="email"
          required
          value={details.customerEmail}
          onChange={(e) => setDetails({ ...details, customerEmail: e.target.value })}
        />
        <Input
          label="Phone"
          type="tel"
          required
          value={details.customerPhone}
          onChange={(e) => setDetails({ ...details, customerPhone: e.target.value })}
        />
      </Box>
      <Box sx={{ display: 'grid', gridTemplateColumns: '1fr', gap: 2 }}>
        <Select
          label="Cargo type"
          required
          placeholder="Select cargo type"
          options={cargoTypes.map((c) => ({ value: String(c.id), label: c.name }))}
          value={details.cargoTypeId ? String(details.cargoTypeId) : ''}
          onChange={(e) => setDetails({ ...details, cargoTypeId: Number(e.target.value) })}
        />
      </Box>
      <Button type="submit" style={{ width: '100%' }} disabled={submitting}>
        {submitting ? 'Submitting…' : 'Request this quote'}
      </Button>
    </Box>
  )
}
