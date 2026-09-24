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

/** ui-plan.md IA page 4 + design-system-spec.md "Sale Approval" section: line items, a
 * "refresh rate" delta the user must click for (never auto-refreshed, technical-plan.md §3),
 * an editable final price, an approval note, and the Reject/Approve pair — 1:2 weight, Approve
 * in success green so it doesn't compete visually with the app's ordinary primary buttons. */
export function QuotationDetailPage({ id, onBack }: QuotationDetailPageProps) {
  const [detail, setDetail] = useState<QuotationDetail | null>(null)
  const [loading, setLoading] = useState(true)
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

  const handleApprove = async () => {
    setBusy(true)
    try {
      await api.post(`/api/quotes/${id}/approve`, { finalPrice: Number(finalPrice), note: note || undefined })
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

        <p className="font-numeric font-bold text-2xl">{formatMoney(quotation.subtotal, quotation.quoteCurrency)}</p>

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
            <Input
              label="Final price to customer"
              type="number"
              step="0.01"
              value={finalPrice}
              onChange={(e) => setFinalPrice(e.target.value)}
            />
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
