import { useState } from 'react'
import Box from '@mui/material/Box'
import { Button, Skeleton } from '../../components/ui'
import { useToast } from '../../components/ui'
import { ApiError, api } from '../../lib/api'
import { CompareSection } from './CompareCard'
import { CustomerDetailsForm, type CustomerDetails } from './CustomerDetailsForm'
import { QuoteResultCard } from './QuoteResultCard'
import { ShipmentForm } from './ShipmentForm'
import type { QuoteCalculationResult, QuoteShipmentInput, QuoteSubmitInput } from './types'
import { useMasterData } from './useMasterData'

type Step = 'form' | 'result' | 'contact' | 'confirmed'

const emptyShipment: QuoteShipmentInput = {
  mode: 'fcl',
  direction: 'export',
  originPortId: 0,
  destinationPortId: 0,
  incotermCode: '',
  readyDate: '',
  containerQty: 1,
}

/** ui-plan.md IA pages 1-2: Instant Quote (public) + Quote result/submit, as one linear flow
 * since there's no router yet (T13 scope) — form -> priced result (+ FCL/LCL compare) -> guest
 * contact details -> confirmation. Calculating never sends anything to a customer; only
 * Sale's approval (T8) does that (requirements.md §6). */
export function InstantQuotePage() {
  const { seaPorts, airPorts, incoterms, cargoTypes, loading, error: masterDataError } = useMasterData()
  const toast = useToast()

  const [step, setStep] = useState<Step>('form')
  const [shipment, setShipment] = useState<QuoteShipmentInput>(emptyShipment)
  const [result, setResult] = useState<QuoteCalculationResult | null>(null)
  const [compareResult, setCompareResult] = useState<QuoteCalculationResult | null>(null)
  const [calculating, setCalculating] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [confirmation, setConfirmation] = useState<{ quoteNo: string; expiresAt: string } | null>(null)

  const handleCalculate = async () => {
    setCalculating(true)
    setCompareResult(null)
    try {
      const primary = await api.post<QuoteCalculationResult>('/api/public/quotes/calculate', shipment)
      setResult(primary)
      setStep('result')

      if (shipment.mode === 'lcl') {
        // Comparable FCL quote for the same route — a single 20' container is the smallest
        // real comparison point; labelled clearly in CompareCard so it isn't mistaken for an
        // exact match to the LCL cargo's actual volume.
        const fclCompare = await api.post<QuoteCalculationResult>('/api/public/quotes/calculate', {
          ...shipment,
          mode: 'fcl',
          containerSize: '20',
          containerQty: 1,
        } satisfies QuoteShipmentInput)
        setCompareResult(fclCompare)
      }
    } catch (err) {
      toast.show(err instanceof ApiError ? err.errors.join(' ') || err.message : 'Failed to calculate quote', 'destructive')
    } finally {
      setCalculating(false)
    }
  }

  const handleSubmit = async (details: CustomerDetails) => {
    if (!details.cargoTypeId) {
      toast.show('Select a cargo type', 'destructive')
      return
    }

    setSubmitting(true)
    try {
      const payload: QuoteSubmitInput = {
        ...shipment,
        customerName: details.customerName,
        customerCompany: details.customerCompany || undefined,
        customerEmail: details.customerEmail,
        customerPhone: details.customerPhone,
        cargoTypeId: details.cargoTypeId,
      }
      const submitted = await api.post<QuoteCalculationResult>('/api/public/quotes', payload)
      setConfirmation({ quoteNo: submitted.quoteNo!, expiresAt: submitted.expiresAt! })
      setStep('confirmed')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.errors.join(' ') || err.message : 'Failed to submit quote', 'destructive')
    } finally {
      setSubmitting(false)
    }
  }

  const startOver = () => {
    setShipment(emptyShipment)
    setResult(null)
    setCompareResult(null)
    setConfirmation(null)
    setStep('form')
  }

  if (loading) {
    return (
      <div className="quote-form flex flex-col gap-3">
        <Skeleton height="2rem" width="40%" />
        <Skeleton height="2.5rem" />
        <Skeleton height="2.5rem" />
      </div>
    )
  }

  if (masterDataError) {
    return (
      <div className="quote-form">
        <p className="text-sm text-destructive">Could not load the quote form: {masterDataError}</p>
      </div>
    )
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, alignItems: 'center', width: '100%' }}>
      {(step === 'form' || step === 'result' || step === 'contact') && (
        <ShipmentForm
          value={shipment}
          onChange={setShipment}
          seaPorts={seaPorts}
          airPorts={airPorts}
          incoterms={incoterms}
          onSubmit={handleCalculate}
          submitting={calculating}
        />
      )}

      {result && (step === 'result' || step === 'contact') && (
        <Box className="quote-form" sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          <QuoteResultCard result={result} />
          {compareResult && <CompareSection fcl={compareResult} lcl={result} />}
          {step === 'result' && (
            <Button onClick={() => setStep('contact')} style={{ width: '100%' }}>
              Request this quote
            </Button>
          )}
        </Box>
      )}

      {step === 'contact' && (
        <CustomerDetailsForm cargoTypes={cargoTypes} onSubmit={handleSubmit} submitting={submitting} />
      )}

      {step === 'confirmed' && confirmation && (
        <Box className="quote-form" sx={{ textAlign: 'center', display: 'flex', flexDirection: 'column', gap: 2, alignItems: 'center' }}>
          <h2 className="font-heading text-xl font-semibold text-balance">Quote request received</h2>
          <p className="text-sm text-muted-foreground">
            Reference <span className="font-numeric font-semibold text-foreground">{confirmation.quoteNo}</span> — valid until{' '}
            {new Date(confirmation.expiresAt).toLocaleDateString()}.
          </p>
          <p className="text-sm text-muted-foreground">
            This is a draft price. A member of our Sale team will review and confirm it with you shortly.
          </p>
          <Button variant="outline" onClick={startOver}>
            Start a new quote
          </Button>
        </Box>
      )}
    </Box>
  )
}
