import { useState } from 'react'
import { QuotationDetailPage } from './QuotationDetailPage'
import { QuotationInboxPage } from './QuotationInboxPage'

/** Orchestrates ui-plan.md IA pages 3-4 as one linear flow (no router yet), same pattern as
 * InstantQuotePage (T13) and OpsApp (T12). */
export function QuotationsPage() {
  const [selectedId, setSelectedId] = useState<number | null>(null)

  return selectedId === null ? (
    <QuotationInboxPage onSelect={setSelectedId} />
  ) : (
    <QuotationDetailPage id={selectedId} onBack={() => setSelectedId(null)} />
  )
}
