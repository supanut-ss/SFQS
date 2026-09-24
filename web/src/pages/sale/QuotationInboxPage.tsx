import { useEffect, useState } from 'react'
import { EmptyState, Select, Skeleton, Table } from '../../components/ui'
import { ApiError, api } from '../../lib/api'
import { formatMoney } from '../../lib/format'
import { StatusBadge } from './statusBadge'
import type { Quotation, QuotationStatus } from './types'

interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

const STATUS_OPTIONS: { value: QuotationStatus | ''; label: string }[] = [
  { value: '', label: 'All statuses' },
  { value: 'pendingSaleApproval', label: 'Pending Sale Approval' },
  { value: 'approvedAndSent', label: 'Approved & Sent' },
  { value: 'confirmed', label: 'Confirmed' },
  { value: 'rejected', label: 'Rejected' },
  { value: 'expired', label: 'Expired' },
  { value: 'draft', label: 'Draft' },
]

export interface QuotationInboxPageProps {
  onSelect: (id: number) => void
}

/** ui-plan.md IA page 3 — GET /api/quotes?status=&page= (T7), Sale/Admin only (T8). */
export function QuotationInboxPage({ onSelect }: QuotationInboxPageProps) {
  const [status, setStatus] = useState<QuotationStatus | ''>('pendingSaleApproval')
  const [quotations, setQuotations] = useState<Quotation[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    setLoading(true)
    setError(null)
    const query = status ? `?status=${status}` : ''
    api
      .get<Paged<Quotation>>(`/api/quotes${query}`)
      .then((res) => {
        if (!cancelled) setQuotations(res.items)
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof ApiError ? err.message : 'Failed to load quotations')
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [status])

  return (
    <div className="flex flex-col gap-4 w-full max-w-4xl">
      <div className="flex items-center justify-between">
        <h2 className="font-heading text-lg font-semibold">Quotation inbox</h2>
        <Select
          label="Status"
          options={STATUS_OPTIONS.map((o) => ({ value: o.value, label: o.label }))}
          value={status}
          onChange={(e) => setStatus(e.target.value as QuotationStatus | '')}
        />
      </div>

      {loading ? (
        <div className="flex flex-col gap-2">
          <Skeleton height="2rem" />
          <Skeleton height="2rem" />
        </div>
      ) : error ? (
        <EmptyState title="Could not load quotations" description={error} />
      ) : (
        <Table
          columns={[
            { key: 'quoteNo', header: 'Quote #', render: (q: Quotation) => q.quoteNo },
            { key: 'customer', header: 'Customer', render: (q: Quotation) => q.customerName },
            { key: 'mode', header: 'Mode', render: (q: Quotation) => q.mode.toUpperCase() },
            { key: 'direction', header: 'Direction', render: (q: Quotation) => q.direction },
            { key: 'total', header: 'Total', align: 'right', render: (q: Quotation) => formatMoney(q.finalPrice, q.quoteCurrency) },
            { key: 'status', header: 'Status', render: (q: Quotation) => <StatusBadge status={q.status} /> },
            { key: 'expires', header: 'Expires', render: (q: Quotation) => q.expiresAt.slice(0, 10) },
            {
              key: 'actions',
              header: '',
              render: (q: Quotation) => (
                <button type="button" className="text-sm underline text-primary" onClick={() => onSelect(q.id)}>
                  View
                </button>
              ),
            },
          ]}
          rows={quotations}
          rowKey={(q) => q.id}
          emptyTitle="No quotations"
          emptyDescription="Nothing matches this status filter right now."
        />
      )}
    </div>
  )
}
