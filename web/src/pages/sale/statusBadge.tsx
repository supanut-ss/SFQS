import { Badge, type BadgeVariant } from '../../components/ui'
import type { QuotationStatus } from './types'

const STATUS_VARIANT: Record<QuotationStatus, BadgeVariant> = {
  draft: 'info',
  pendingSaleApproval: 'warning',
  approvedAndSent: 'success',
  confirmed: 'success',
  rejected: 'destructive',
  expired: 'destructive',
}

const STATUS_LABEL: Record<QuotationStatus, string> = {
  draft: 'Draft',
  pendingSaleApproval: 'Pending Sale Approval',
  approvedAndSent: 'Approved & Sent',
  confirmed: 'Confirmed',
  rejected: 'Rejected',
  expired: 'Expired',
}

/** requirements.md §6: badge color per status — warning/amber for the mandatory approval
 * gate's waiting state, success once it's past that gate. */
export function StatusBadge({ status }: { status: QuotationStatus }) {
  return <Badge variant={STATUS_VARIANT[status]}>{STATUS_LABEL[status]}</Badge>
}
