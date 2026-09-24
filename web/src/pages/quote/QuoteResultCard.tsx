import { Badge } from '../../components/ui'
import { formatMoney } from '../../lib/format'
import type { QuoteCalculationResult } from './types'

export interface QuoteResultCardProps {
  result: QuoteCalculationResult
}

/** Pricing breakdown — design-system-spec.md "ตัวเลขคือพระเอก": subtotal in large
 * font-numeric, NotQuotable charges shown as a disclaimer under the total, never added in
 * (technical-plan.md §3 — DDP-style charges can't be pre-priced). */
export function QuoteResultCard({ result }: QuoteResultCardProps) {
  if (!result.rateFound) {
    return (
      <div className="card-sample" style={{ width: 'auto' }}>
        <Badge variant="warning">Manual pricing required</Badge>
        <p className="text-sm text-muted-foreground mt-3">
          {result.noRateFoundReason ?? 'No matching rate was found for this route — Sale will price it manually.'}
        </p>
      </div>
    )
  }

  return (
    <div className="card-sample" style={{ width: 'auto' }}>
      <table className="freight-table">
        <tbody>
          <tr>
            <td>Freight cost</td>
            <td className="num">{formatMoney(result.freightCost, result.quoteCurrency)}</td>
          </tr>
          {result.localChargeLines
            .filter((line) => !line.isNotQuotable)
            .map((line) => (
              <tr key={line.chargeType}>
                <td>{line.chargeType}</td>
                <td className="num">{formatMoney(line.amount, line.currency)}</td>
              </tr>
            ))}
        </tbody>
      </table>

      <p className="font-numeric font-bold text-2xl mt-4 mb-1">{formatMoney(result.subtotal, result.quoteCurrency)}</p>
      <p className="text-sm text-muted-foreground mb-3">
        Total ({result.rateSource === 'system' ? 'current system rate' : result.rateSource})
      </p>

      {result.notQuotableNotes.length > 0 && (
        <ul className="text-sm text-muted-foreground list-disc pl-5 space-y-1">
          {result.notQuotableNotes.map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
