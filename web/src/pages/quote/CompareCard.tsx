import Box from '@mui/material/Box'
import { formatMoney } from '../../lib/format'
import type { QuoteCalculationResult } from './types'

export interface CompareCardProps {
  label: string
  result: QuoteCalculationResult
  recommended: boolean
}

function CompareCard({ label, result, recommended }: CompareCardProps) {
  return (
    <div className={`compare-card${recommended ? ' highlight' : ''}`}>
      <h4>{label}</h4>
      {recommended && <div className="recommended-tag">Recommended — lower total</div>}
      {result.rateFound ? (
        <>
          <p className="total">{formatMoney(result.subtotal, result.quoteCurrency)}</p>
          <p className="breakdown">
            Freight {formatMoney(result.freightCost, result.quoteCurrency)}
            <br />
            Local charges {formatMoney(result.localChargeTotal, result.quoteCurrency)}
          </p>
        </>
      ) : (
        <p className="breakdown">No system rate found for this option.</p>
      )}
    </div>
  )
}

export interface CompareSectionProps {
  fcl: QuoteCalculationResult
  lcl: QuoteCalculationResult
}

/** design-system-spec.md "FCL vs LCL Comparison Card": cards side by side, the cheaper one
 * highlighted with a thick primary border so it's obvious at a glance. Matches
 * BreakevenCalculator's logic (Freito.Domain.Quoting) — comparing each mode's already-computed
 * subtotal is equivalent to that formula, just evaluated against real API results instead of
 * raw inputs. */
export function CompareSection({ fcl, lcl }: CompareSectionProps) {
  const fclWins = fcl.rateFound && (!lcl.rateFound || fcl.subtotal <= lcl.subtotal)
  const lclWins = lcl.rateFound && (!fcl.rateFound || lcl.subtotal < fcl.subtotal)

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2, width: '100%', maxWidth: 640 }}>
      <CompareCard label="FCL" result={fcl} recommended={fclWins} />
      <CompareCard label="LCL" result={lcl} recommended={lclWins} />
    </Box>
  )
}
