import { useRouter } from '../../lib/useRouter'
import { QuotationDetailPage } from './QuotationDetailPage'
import { QuotationInboxPage } from './QuotationInboxPage'

/** Orchestrates ui-plan.md IA pages 3-4 with URL routing and deep links. */
export function QuotationsPage() {
  const { params, navigate } = useRouter()
  const selectedId = params.id ? Number(params.id) : null

  return selectedId === null ? (
    <QuotationInboxPage onSelect={(id) => navigate(`/ops/quotations/${id}`)} />
  ) : (
    <QuotationDetailPage id={selectedId} onBack={() => navigate('/ops/quotations')} />
  )
}
