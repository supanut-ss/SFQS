import { useEffect, useState } from 'react'
import Box from '@mui/material/Box'
import { Button, EmptyState, Input, Select, Skeleton, Table, useToast } from '../../components/ui'
import { ApiError, api } from '../../lib/api'
import { formatMoney } from '../../lib/format'
import type { ShipmentDirection, TransportMode } from '../ops/types'
import { useMasterData } from '../quote/useMasterData'
import { ModularQuotationSections, type ModularSectionsState } from './ModularQuotationSections'
import { StatusBadge } from './statusBadge'
import type { QuotationDetail, QuotationDimensionItem, QuotationLine, RefreshRateResponse } from './types'

export interface QuotationDetailPageProps {
  id: number
  onBack: () => void
}

interface EditableLine {
  id?: number
  description: string
  basis: string
  amount: string | number
  currency: string
}

/** ui-plan.md IA page 4 + design-system-spec.md "Sale Approval" section: line items, a
 * "refresh rate" delta the user must click for (never auto-refreshed, technical-plan.md §3),
 * editable line items (free customization per real operations worksheet), an editable final
 * price, an approval note, and the Reject/Approve pair. */
export function QuotationDetailPage({ id, onBack }: QuotationDetailPageProps) {
  const masterData = useMasterData()
  const [detail, setDetail] = useState<QuotationDetail | null>(null)
  const [editableLines, setEditableLines] = useState<EditableLine[]>([])
  const [loading, setLoading] = useState(true)
  const [savingLines, setSavingLines] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [delta, setDelta] = useState<RefreshRateResponse | null>(null)
  const [refreshing, setRefreshing] = useState(false)
  const [finalPrice, setFinalPrice] = useState('')
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)

  // Customer & Shipment parameters editable by Sale
  const [isEditingParameters, setIsEditingParameters] = useState(false)
  const [customerName, setCustomerName] = useState('')
  const [customerCompany, setCustomerCompany] = useState('')
  const [customerEmail, setCustomerEmail] = useState('')
  const [customerPhone, setCustomerPhone] = useState('')

  const [originPortId, setOriginPortId] = useState<number>(0)
  const [destinationPortId, setDestinationPortId] = useState<number>(0)
  const [direction, setDirection] = useState<ShipmentDirection>('import')
  const [mode, setMode] = useState<TransportMode>('lcl')
  const [incotermCode, setIncotermCode] = useState('')
  const [readyDate, setReadyDate] = useState('')
  const [containerSize, setContainerSize] = useState('20')
  const [qty, setQty] = useState(1)
  const [cbm, setCbm] = useState('')
  const [weightKg, setWeightKg] = useState('')

  const [modularState, setModularState] = useState<ModularSectionsState>({
    showSchedule: false,
    transitTime: '',
    frequency: '',
    closingSchedule: '',
    carrierInfo: '',
    showDimensions: false,
    dimensionItems: [],
    showPayment: false,
    paymentTerms: '',
    showInsurance: false,
    insuranceStatus: '',
    showTerms: false,
    termsAndConditions: '',
  })
  const toast = useToast()

  const load = () => {
    setLoading(true)
    setError(null)
    api
      .get<QuotationDetail>(`/api/quotes/${id}`)
      .then((res) => {
        setDetail(res)
        setEditableLines(
          res.lines.map((l) => ({
            id: l.id,
            description: l.description,
            basis: l.basis,
            amount: l.amount,
            currency: l.currency,
          }))
        )
        setFinalPrice(res.quotation.finalPrice.toString())

        // Populate customer & shipment fields
        setCustomerName(res.quotation.customerName ?? '')
        setCustomerCompany(res.quotation.customerCompany ?? '')
        setCustomerEmail(res.quotation.customerEmail ?? '')
        setCustomerPhone(res.quotation.customerPhone ?? '')
        setOriginPortId(res.quotation.originPortId)
        setDestinationPortId(res.quotation.destinationPortId)
        setDirection(res.quotation.direction)
        setMode(res.quotation.mode)
        setIncotermCode(res.quotation.incotermCode)
        setReadyDate(res.quotation.readyDate ? res.quotation.readyDate.slice(0, 10) : '')
        setContainerSize(res.quotation.containerSize ?? '20')
        setQty(res.quotation.qty || 1)
        setCbm(res.quotation.cbm != null ? res.quotation.cbm.toString() : '')
        setWeightKg(res.quotation.weightKg != null ? res.quotation.weightKg.toString() : '')

        let parsedDimensions: QuotationDimensionItem[] = []
        if (res.quotation.dimensionsJson) {
          try {
            parsedDimensions = JSON.parse(res.quotation.dimensionsJson)
          } catch {
            parsedDimensions = []
          }
        }
        setModularState({
          showSchedule: !!(
            res.quotation.transitTime ||
            res.quotation.frequency ||
            res.quotation.closingSchedule ||
            res.quotation.carrierInfo
          ),
          transitTime: res.quotation.transitTime ?? '',
          frequency: res.quotation.frequency ?? '',
          closingSchedule: res.quotation.closingSchedule ?? '',
          carrierInfo: res.quotation.carrierInfo ?? '',
          showDimensions: parsedDimensions.length > 0,
          dimensionItems: parsedDimensions,
          showPayment: !!res.quotation.paymentTerms,
          paymentTerms: res.quotation.paymentTerms ?? '',
          showInsurance: !!res.quotation.insuranceStatus,
          insuranceStatus: res.quotation.insuranceStatus ?? '',
          showTerms: !!res.quotation.termsAndConditions,
          termsAndConditions: res.quotation.termsAndConditions ?? '',
        })
      })
      .catch((err) => setError(err instanceof ApiError ? err.message : 'Failed to load quotation'))
      .finally(() => setLoading(false))
  }

  useEffect(load, [id])

  const handleRefreshRate = async () => {
    setRefreshing(true)
    try {
      setDelta(await api.post<RefreshRateResponse>(`/api/quotes/${id}/refresh-rate`))
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to refresh rate', 'destructive')
    } finally {
      setRefreshing(false)
    }
  }

  const updateLine = (index: number, patch: Partial<EditableLine>) => {
    setEditableLines((prev) => {
      const next = [...prev]
      next[index] = { ...next[index], ...patch }
      return next
    })
  }

  const addLine = () => {
    if (!detail) return
    setEditableLines((prev) => [
      ...prev,
      {
        description: '',
        basis: 'Per shipment',
        amount: 0,
        currency: detail.quotation.quoteCurrency,
      },
    ])
  }

  const removeLine = (index: number) => {
    setEditableLines((prev) => prev.filter((_, i) => i !== index))
  }

  const formatLinesForPayload = () =>
    editableLines.map((l) => ({
      id: l.id,
      description: l.description.trim() || 'Charge',
      basis: l.basis.trim() || 'Per shipment',
      amount: Number(l.amount) || 0,
      currency: (l.currency.trim() || detail?.quotation.quoteCurrency || 'USD').toUpperCase(),
    }))

  const computedSubtotal = editableLines.reduce((acc, l) => acc + (Number(l.amount) || 0), 0)

  const handleSaveLines = async () => {
    if (!detail) return
    setSavingLines(true)
    try {
      const formatted = formatLinesForPayload()
      const customerShipmentPayload = {
        customerName: customerName.trim() || undefined,
        customerCompany: customerCompany.trim() || undefined,
        customerEmail: customerEmail.trim() || undefined,
        customerPhone: customerPhone.trim() || undefined,
        originPortId: Number(originPortId) || undefined,
        destinationPortId: Number(destinationPortId) || undefined,
        direction,
        mode,
        incotermCode: incotermCode.toUpperCase(),
        readyDate: readyDate || undefined,
        containerSize: mode === 'fcl' ? containerSize : undefined,
        qty: Number(qty) || 1,
        cbm: cbm ? Number(cbm) : undefined,
        weightKg: weightKg ? Number(weightKg) : undefined,
      }
      const modularPayload = {
        transitTime: modularState.showSchedule ? modularState.transitTime.trim() || null : null,
        frequency: modularState.showSchedule ? modularState.frequency.trim() || null : null,
        closingSchedule: modularState.showSchedule ? modularState.closingSchedule.trim() || null : null,
        carrierInfo: modularState.showSchedule ? modularState.carrierInfo.trim() || null : null,
        paymentTerms: modularState.showPayment ? modularState.paymentTerms.trim() || null : null,
        insuranceStatus: modularState.showInsurance ? modularState.insuranceStatus.trim() || null : null,
        termsAndConditions: modularState.showTerms ? modularState.termsAndConditions.trim() || null : null,
        dimensionsJson:
          modularState.showDimensions && modularState.dimensionItems.length > 0
            ? JSON.stringify(modularState.dimensionItems)
            : null,
      }
      const res = await api.put<QuotationDetail>(`/api/quotes/${id}/lines`, {
        lines: formatted,
        finalPrice: finalPrice ? Number(finalPrice) : computedSubtotal,
        note: note.trim() || undefined,
        ...customerShipmentPayload,
        ...modularPayload,
      })
      setDetail(res)
      setEditableLines(
        res.lines.map((l) => ({
          id: l.id,
          description: l.description,
          basis: l.basis,
          amount: l.amount,
          currency: l.currency,
        }))
      )
      setFinalPrice(res.quotation.finalPrice.toString())
      toast.show('Quotation updated', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to save lines', 'destructive')
    } finally {
      setSavingLines(false)
    }
  }

  const triggerMailto = () => {
    if (!detail) return
    const q = detail.quotation
    const subject = encodeURIComponent(`Quotation ${q.quoteNo} - Freito Logistics`)
    const body = encodeURIComponent(
      `Dear ${q.customerName},\n\nPlease find attached quotation ${q.quoteNo} for your shipment.\n\nTotal: ${formatMoney(q.finalPrice, q.quoteCurrency)}\n\nBest regards,\nFreito Team`
    )
    const mailtoUrl = `mailto:${q.customerEmail}?subject=${subject}&body=${body}`
    const link = document.createElement('a')
    link.href = mailtoUrl
    link.click()
  }

  const handleApprove = async () => {
    setBusy(true)
    try {
      const formatted = canDecide ? formatLinesForPayload() : undefined
      const customerShipmentPayload = {
        customerName: customerName.trim() || undefined,
        customerCompany: customerCompany.trim() || undefined,
        customerEmail: customerEmail.trim() || undefined,
        customerPhone: customerPhone.trim() || undefined,
        originPortId: Number(originPortId) || undefined,
        destinationPortId: Number(destinationPortId) || undefined,
        direction,
        mode,
        incotermCode: incotermCode.toUpperCase(),
        readyDate: readyDate || undefined,
        containerSize: mode === 'fcl' ? containerSize : undefined,
        qty: Number(qty) || 1,
        cbm: cbm ? Number(cbm) : undefined,
        weightKg: weightKg ? Number(weightKg) : undefined,
      }
      const modularPayload = {
        transitTime: modularState.showSchedule ? modularState.transitTime.trim() || null : null,
        frequency: modularState.showSchedule ? modularState.frequency.trim() || null : null,
        closingSchedule: modularState.showSchedule ? modularState.closingSchedule.trim() || null : null,
        carrierInfo: modularState.showSchedule ? modularState.carrierInfo.trim() || null : null,
        paymentTerms: modularState.showPayment ? modularState.paymentTerms.trim() || null : null,
        insuranceStatus: modularState.showInsurance ? modularState.insuranceStatus.trim() || null : null,
        termsAndConditions: modularState.showTerms ? modularState.termsAndConditions.trim() || null : null,
        dimensionsJson:
          modularState.showDimensions && modularState.dimensionItems.length > 0
            ? JSON.stringify(modularState.dimensionItems)
            : null,
      }
      await api.post(`/api/quotes/${id}/approve`, {
        finalPrice: finalPrice ? Number(finalPrice) : computedSubtotal,
        note: note || undefined,
        lines: formatted,
        ...customerShipmentPayload,
        ...modularPayload,
      })
      toast.show('Quotation approved', 'success')
      load()
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to approve quotation', 'destructive')
    } finally {
      setBusy(false)
    }
  }

  const handleApplyRefreshedRate = () => {
    if (!delta || !detail) return
    const newLines: EditableLine[] = [
      {
        description: 'Freight',
        basis: 'Refreshed rate',
        amount: delta.currentFreightCost,
        currency: delta.currency,
      },
      ...delta.localChargeDeltas.map((l) => ({
        description: l.chargeType,
        basis: 'Local charge',
        amount: l.currentAmount,
        currency: delta.currency,
      })),
    ]
    setEditableLines(newLines)
    setFinalPrice(delta.currentSubtotal.toString())
    toast.show('Applied refreshed rates to quotation lines', 'success')
  }

  const handleSend = async () => {
    setBusy(true)
    try {
      await api.post(`/api/quotes/${id}/send`, { note: 'Sent to customer manually' })
      toast.show('Quotation marked as sent', 'success')
      triggerMailto()
      load()
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to send quotation', 'destructive')
    } finally {
      setBusy(false)
    }
  }

  const handleReject = async () => {
    if (!note.trim()) {
      toast.show('A note is required to reject a quotation', 'destructive')
      return
    }
    setBusy(true)
    try {
      await api.post(`/api/quotes/${id}/reject`, { note })
      toast.show('Quotation rejected', 'success')
      load()
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to reject quotation', 'destructive')
    } finally {
      setBusy(false)
    }
  }

  const handleDownloadPdf = () => {
    window.open(`/api/quotes/${id}/pdf`, '_blank')
  }

  if (loading) {
    return (
      <div className="quote-form flex flex-col gap-3">
        <Skeleton height="2rem" width="40%" />
        <Skeleton height="2.5rem" />
      </div>
    )
  }

  if (error || !detail) {
    return <EmptyState title="Could not load quotation" description={error ?? undefined} />
  }

  const { quotation, lines } = detail
  const canDecide = quotation.status === 'pendingSaleApproval'
  const isApproved = quotation.status === 'approved'
  const isSent = quotation.status === 'approvedAndSent'

  const allPorts = [...masterData.seaPorts, ...masterData.airPorts]
  const originPortObj = allPorts.find((p) => p.id === originPortId)
  const destPortObj = allPorts.find((p) => p.id === destinationPortId)
  const originPortLabel = originPortObj ? `${originPortObj.name} (${originPortObj.code})` : `Port #${originPortId}`
  const destPortLabel = destPortObj ? `${destPortObj.name} (${destPortObj.code})` : `Port #${destinationPortId}`

  return (
    <div className="flex flex-col gap-4 w-full max-w-3xl">
      <Button type="button" variant="link" className="text-sm text-muted-foreground self-start" onClick={onBack}>
        ← Back to inbox
      </Button>

      <div className="flex items-center justify-between">
        <h2 className="font-heading text-lg font-semibold text-balance">{quotation.quoteNo}</h2>
        <StatusBadge status={quotation.status} />
      </div>

      <div className="quote-form flex flex-col gap-4">
        {/* Customer & Shipment Parameters (Editable by Sale) */}
        <div className="border border-border/80 rounded-md p-3.5 bg-muted/15 flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold uppercase tracking-wider text-muted-foreground flex items-center gap-1.5">
              <span>📋</span>
              <span>Customer & Shipment Parameters</span>
            </span>
            {canDecide && (
              <Button
                type="button"
                variant="outline"
                className="text-xs py-0.5 px-2.5"
                onClick={() => setIsEditingParameters(!isEditingParameters)}
              >
                {isEditingParameters ? 'Close Edit Form' : '✎ Edit Parameters'}
              </Button>
            )}
          </div>

          {!isEditingParameters ? (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2, fontSize: '0.75rem' }}>
              <div>
                <p className="text-muted-foreground text-[10px] uppercase font-semibold">Customer</p>
                <p className="text-sm font-medium mt-0.5">
                  {customerName} {customerCompany ? `(${customerCompany})` : ''}
                </p>
                <p className="text-muted-foreground">{customerEmail}</p>
                <p className="text-muted-foreground">{customerPhone}</p>
              </div>
              <div>
                <p className="text-muted-foreground text-[10px] uppercase font-semibold">Shipment</p>
                <p className="text-sm font-medium mt-0.5">
                  {originPortLabel} → {destPortLabel}
                </p>
                <p className="text-muted-foreground">
                  {mode.toUpperCase()} • {direction} • {incotermCode} • Ready: {readyDate || '-'}
                </p>
                <p className="text-muted-foreground font-medium mt-0.5">
                  {mode === 'fcl' && `Container: ${qty} × ${containerSize}'`}
                  {mode === 'lcl' && `Volume: ${cbm || '-'} CBM, Weight: ${weightKg || '-'} kg`}
                  {mode === 'air' && `Weight: ${weightKg || '-'} kg, Volume: ${cbm || '-'} CBM`}
                </p>
              </div>
            </Box>
          ) : (
            <div className="flex flex-col gap-3 border-t border-border/60 pt-3">
              <p className="text-xs text-muted-foreground">
                Edit the parameters submitted by the customer. Saving will update the quotation record and PDF.
              </p>
              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 1.5, fontSize: '0.75rem' }}>
                <Input
                  label="Customer Name"
                  value={customerName}
                  onChange={(e) => setCustomerName(e.target.value)}
                />
                <Input
                  label="Company Name"
                  placeholder="Optional"
                  value={customerCompany}
                  onChange={(e) => setCustomerCompany(e.target.value)}
                />
                <Input
                  label="Customer Email"
                  type="email"
                  value={customerEmail}
                  onChange={(e) => setCustomerEmail(e.target.value)}
                />
                <Input
                  label="Customer Phone"
                  value={customerPhone}
                  onChange={(e) => setCustomerPhone(e.target.value)}
                />
              </Box>

              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(4, 1fr)' }, gap: 1.5, fontSize: '0.75rem' }}>
                <Select
                  label="Mode"
                  value={mode}
                  onChange={(e) => setMode(e.target.value as TransportMode)}
                  options={[
                    { value: 'fcl', label: 'FCL (Sea)' },
                    { value: 'lcl', label: 'LCL (Sea)' },
                    { value: 'air', label: 'Air Freight' },
                  ]}
                />
                <Select
                  label="Direction"
                  value={direction}
                  onChange={(e) => setDirection(e.target.value as ShipmentDirection)}
                  options={[
                    { value: 'import', label: 'Import' },
                    { value: 'export', label: 'Export' },
                  ]}
                />
                <Select
                  label="Incoterm"
                  value={incotermCode}
                  onChange={(e) => setIncotermCode(e.target.value)}
                  options={masterData.incoterms.map((i) => ({ value: i.code, label: `${i.code} - ${i.name}` }))}
                />
                <Input
                  label="Ready Date"
                  type="date"
                  value={readyDate}
                  onChange={(e) => setReadyDate(e.target.value)}
                />
              </Box>

              <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 1.5, fontSize: '0.75rem' }}>
                <Select
                  label="Origin Port / Airport"
                  value={originPortId.toString()}
                  onChange={(e) => setOriginPortId(Number(e.target.value))}
                  options={(mode === 'air' ? masterData.airPorts : masterData.seaPorts).map((p) => ({
                    value: p.id.toString(),
                    label: `${p.name} (${p.code}) - ${p.country}`,
                  }))}
                />
                <Select
                  label="Destination Port / Airport"
                  value={destinationPortId.toString()}
                  onChange={(e) => setDestinationPortId(Number(e.target.value))}
                  options={(mode === 'air' ? masterData.airPorts : masterData.seaPorts).map((p) => ({
                    value: p.id.toString(),
                    label: `${p.name} (${p.code}) - ${p.country}`,
                  }))}
                />
              </Box>

              {mode === 'fcl' && (
                <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1.5, fontSize: '0.75rem' }}>
                  <Select
                    label="Container Size"
                    value={containerSize}
                    onChange={(e) => setContainerSize(e.target.value)}
                    options={[
                      { value: '20', label: "20' Container" },
                      { value: '40', label: "40' Container" },
                      { value: '40HQ', label: "40' High Cube" },
                    ]}
                  />
                  <Input
                    label="Quantity"
                    type="number"
                    min="1"
                    value={qty.toString()}
                    onChange={(e) => setQty(Number(e.target.value) || 1)}
                  />
                </Box>
              )}

              {(mode === 'lcl' || mode === 'air') && (
                <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1.5, fontSize: '0.75rem' }}>
                  <Input
                    label="Volume (CBM)"
                    type="number"
                    step="0.001"
                    value={cbm}
                    onChange={(e) => setCbm(e.target.value)}
                  />
                  <Input
                    label="Weight (kg)"
                    type="number"
                    step="0.1"
                    value={weightKg}
                    onChange={(e) => setWeightKg(e.target.value)}
                  />
                </Box>
              )}
            </div>
          )}
        </div>

        {canDecide ? (
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide text-balance">Quotation Lines</h3>
              <span className="text-xs text-muted-foreground">Sale can customize or add line items before approving</span>
            </div>

            <div className="overflow-x-auto border border-border rounded-md">
              <table className="table w-full text-sm">
                <thead>
                  <tr>
                    <th className="text-left py-2 px-3">Description</th>
                    <th className="text-left py-2 px-3 w-36">Basis</th>
                    <th className="text-right py-2 px-3 w-32">Amount</th>
                    <th className="text-left py-2 px-3 w-20">Currency</th>
                    <th className="w-10 text-center py-2 px-1">
                      <span className="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {editableLines.map((line, idx) => (
                    <tr key={idx} className="border-t border-border/50">
                      <td className="py-2 px-2">
                        <input
                          className="input py-1 px-2 text-sm w-full"
                          value={line.description}
                          onChange={(e) => updateLine(idx, { description: e.target.value })}
                          placeholder="Description (e.g. THC, Pick up)"
                        />
                      </td>
                      <td className="py-2 px-2">
                        <input
                          className="input py-1 px-2 text-sm w-full"
                          maxLength={80}
                          value={line.basis}
                          onChange={(e) => updateLine(idx, { basis: e.target.value })}
                          placeholder="Basis (e.g. Per shipment)"
                        />
                      </td>
                      <td className="py-2 px-2">
                        <input
                          type="number"
                          step="0.01"
                          className="input py-1 px-2 text-sm text-right w-full"
                          value={line.amount}
                          onChange={(e) => updateLine(idx, { amount: e.target.value })}
                        />
                      </td>
                      <td className="py-2 px-2">
                        <input
                          className="input py-1 px-2 text-sm w-full uppercase"
                          maxLength={3}
                          value={line.currency}
                          onChange={(e) => updateLine(idx, { currency: e.target.value.toUpperCase() })}
                        />
                      </td>
                      <td className="py-2 px-1 text-center">
                        <Button
                          type="button"
                          variant="ghost"
                          className="text-destructive hover:opacity-80 p-1 text-base font-bold leading-none"
                          onClick={() => removeLine(idx)}
                          aria-label="Remove line"
                        >
                          ✕
                        </Button>
                      </td>
                    </tr>
                  ))}
                  {editableLines.length === 0 && (
                    <tr>
                      <td colSpan={5} className="py-4 text-center text-muted-foreground text-sm">
                        No line items. Click below to add one.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>

            <div className="flex items-center justify-between mt-1">
              <Button type="button" variant="outline" className="text-xs py-1 px-3" onClick={addLine}>
                + Add line item
              </Button>
              <Button
                type="button"
                variant="secondary"
                className="text-xs py-1 px-3"
                onClick={handleSaveLines}
                disabled={savingLines || busy}
              >
                {savingLines ? 'Saving…' : 'Save line changes'}
              </Button>
            </div>
          </div>
        ) : (
          <Table
            columns={[
              { key: 'description', header: 'Description', render: (l: QuotationLine) => l.description },
              { key: 'basis', header: 'Basis', render: (l: QuotationLine) => l.basis },
              { key: 'amount', header: 'Amount', align: 'right', render: (l: QuotationLine) => formatMoney(l.amount, l.currency) },
            ]}
            rows={lines}
            rowKey={(l) => l.id}
            emptyTitle="No priced lines"
          />
        )}

        <ModularQuotationSections
          editable={canDecide}
          data={modularState}
          onChange={(patch) => setModularState((prev) => ({ ...prev, ...patch }))}
        />

        <div className="flex items-baseline justify-between">
          <span className="text-sm text-muted-foreground font-medium">Subtotal</span>
          <p className="font-numeric font-bold text-2xl">{formatMoney(canDecide ? computedSubtotal : quotation.subtotal, quotation.quoteCurrency)}</p>
        </div>

        <hr className="quote-divider" />

        <div className="rate-compare">
          <span>Compare against the current system rate</span>
          <Button variant="outline" onClick={handleRefreshRate} disabled={refreshing}>
            {refreshing ? 'Checking…' : 'Refresh rate'}
          </Button>
        </div>

        {delta && (
          <div className="text-sm flex flex-col gap-1">
            <p>
              Freight:{' '}
              <span className={delta.freightDelta > 0 ? 'delta-up' : delta.freightDelta < 0 ? 'delta-down' : ''}>
                {delta.freightDelta > 0 ? '▲' : delta.freightDelta < 0 ? '▼' : '–'} {formatMoney(Math.abs(delta.freightDelta), delta.currency)}
              </span>
            </p>
            <p>
              Subtotal:{' '}
              <span className={delta.subtotalDelta > 0 ? 'delta-up' : delta.subtotalDelta < 0 ? 'delta-down' : ''}>
                {delta.subtotalDelta > 0 ? '▲' : delta.subtotalDelta < 0 ? '▼' : '–'} {formatMoney(Math.abs(delta.subtotalDelta), delta.currency)}
              </span>{' '}
              (now {formatMoney(delta.currentSubtotal, delta.currency)})
            </p>
            {!delta.currentRateFound && <p className="text-destructive">No system rate found anymore — price manually.</p>}
            {canDecide && delta.currentRateFound && (
              <Button
                type="button"
                variant="outline"
                className="text-xs py-1 px-3 self-start mt-1.5"
                onClick={handleApplyRefreshedRate}
              >
                Apply refreshed rates to quotation lines
              </Button>
            )}
          </div>
        )}

        {canDecide && (
          <>
            <hr className="quote-divider" />
            <div className="flex items-end gap-2">
              <div className="flex-1">
                <Input
                  label="Final price to customer"
                  type="number"
                  step="0.01"
                  value={finalPrice}
                  onChange={(e) => setFinalPrice(e.target.value)}
                />
              </div>
              <Button
                type="button"
                variant="outline"
                className="text-xs py-2 px-3 mb-1"
                onClick={() => setFinalPrice(computedSubtotal.toString())}
              >
                Use Subtotal
              </Button>
            </div>
            <div className="field-block">
              <label htmlFor="approval-note">Approval note</label>
              <textarea
                id="approval-note"
                className="input"
                rows={3}
                value={note}
                onChange={(e) => setNote(e.target.value)}
                placeholder="Reason for a price change, or why this quotation is rejected"
              />
            </div>
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 1.5, mt: 1 }}>
              <Button variant="outline-destructive" onClick={handleReject} disabled={busy}>
                Reject
              </Button>
              <Button variant="success" onClick={handleApprove} disabled={busy}>
                Approve quotation
              </Button>
            </Box>
          </>
        )}

        {isApproved && (
          <div className="flex flex-col gap-3 p-4 bg-muted/40 border border-border rounded-lg">
            <div className="flex items-center justify-between flex-wrap gap-2">
              <div>
                <p className="text-sm font-semibold text-foreground">Quotation approved</p>
                <p className="text-xs text-muted-foreground">
                  Review or download the PDF quotation, then send it manually to the customer.
                </p>
              </div>
              <div className="flex items-center gap-2">
                <Button type="button" variant="outline" onClick={handleDownloadPdf}>
                  Download PDF
                </Button>
                <Button type="button" variant="success" onClick={handleSend} disabled={busy}>
                  {busy ? 'Sending…' : 'Send to customer'}
                </Button>
              </div>
            </div>
          </div>
        )}

        {(isSent || quotation.status === 'confirmed') && (
          <div className="flex items-center justify-between flex-wrap gap-2 p-3 bg-muted/20 border border-border rounded-lg">
            <div className="text-xs text-muted-foreground">
              {quotation.sentAt ? `Dispatched to customer on ${quotation.sentAt.slice(0, 10)}` : 'Dispatched to customer'}
            </div>
            <div className="flex items-center gap-2">
              <Button type="button" variant="outline" onClick={handleDownloadPdf}>
                Download PDF
              </Button>
              <Button type="button" variant="secondary" onClick={triggerMailto}>
                Compose email (Outlook)
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
