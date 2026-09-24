import { useEffect, useState } from 'react'
import { Button, EmptyState, Input, Skeleton, Table, useToast } from '../../components/ui'
import { ApiError, api } from '../../lib/api'
import { formatMoney } from '../../lib/format'
import { StatusBadge } from './statusBadge'
import type { QuotationDetail, QuotationLine, RefreshRateResponse } from './types'

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
      const res = await api.put<QuotationDetail>(`/api/quotes/${id}/lines`, {
        lines: formatted,
        finalPrice: finalPrice ? Number(finalPrice) : computedSubtotal,
        note: note.trim() || undefined,
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
      toast.show('Quotation lines updated', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to save lines', 'destructive')
    } finally {
      setSavingLines(false)
    }
  }

  const handleApprove = async () => {
    setBusy(true)
    try {
      const formatted = canDecide ? formatLinesForPayload() : undefined
      await api.post(`/api/quotes/${id}/approve`, {
        finalPrice: finalPrice ? Number(finalPrice) : computedSubtotal,
        note: note || undefined,
        lines: formatted,
      })
      toast.show('Quotation approved and marked as sent', 'success')
      load()
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to approve quotation', 'destructive')
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
  const canDownloadPdf = quotation.status === 'approvedAndSent' || quotation.status === 'confirmed'

  return (
    <div className="flex flex-col gap-4 w-full max-w-3xl">
      <button type="button" className="text-sm underline text-muted-foreground self-start" onClick={onBack}>
        ← Back to inbox
      </button>

      <div className="flex items-center justify-between">
        <h2 className="font-heading text-lg font-semibold">{quotation.quoteNo}</h2>
        <StatusBadge status={quotation.status} />
      </div>

      <div className="quote-form flex flex-col gap-4">
        <div className="form-row">
          <div>
            <p className="text-xs text-muted-foreground font-semibold">Customer</p>
            <p className="text-sm">{quotation.customerName}</p>
            <p className="text-sm text-muted-foreground">{quotation.customerEmail}</p>
            <p className="text-sm text-muted-foreground">{quotation.customerPhone}</p>
          </div>
          <div>
            <p className="text-xs text-muted-foreground font-semibold">Shipment</p>
            <p className="text-sm">
              {quotation.mode.toUpperCase()} • {quotation.direction} • {quotation.incotermCode}
            </p>
            <p className="text-sm text-muted-foreground">Ready date: {quotation.readyDate.slice(0, 10)}</p>
          </div>
        </div>

        {canDecide ? (
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Quotation Lines</h3>
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
                        <button
                          type="button"
                          className="text-destructive hover:opacity-80 p-1 text-base font-bold leading-none"
                          onClick={() => removeLine(idx)}
                          title="Remove line"
                        >
                          ✕
                        </button>
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
            <div className="approval-actions">
              <Button variant="outline-destructive" onClick={handleReject} disabled={busy}>
                Reject
              </Button>
              <Button variant="success" onClick={handleApprove} disabled={busy}>
                Approve &amp; send to customer
              </Button>
            </div>
          </>
        )}

        {canDownloadPdf && (
          <Button variant="outline" onClick={handleDownloadPdf}>
            Download PDF
          </Button>
        )}
      </div>
    </div>
  )
}
