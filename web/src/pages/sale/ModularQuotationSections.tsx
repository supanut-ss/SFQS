import { useState } from 'react'
import Box from '@mui/material/Box'
import { Button, Input } from '../../components/ui'
import type { QuotationDimensionItem } from './types'

export interface ModularSectionsState {
  showSchedule: boolean
  transitTime: string
  frequency: string
  closingSchedule: string
  carrierInfo: string

  showDimensions: boolean
  dimensionItems: QuotationDimensionItem[]

  showPayment: boolean
  paymentTerms: string

  showInsurance: boolean
  insuranceStatus: string

  showTerms: boolean
  termsAndConditions: string
}

export interface ModularQuotationSectionsProps {
  editable: boolean
  data: ModularSectionsState
  onChange: (patch: Partial<ModularSectionsState>) => void
}

const PRESET_TERMS = [
  'Rates based on standard service level and availability.',
  'Fuel surcharge subject to change without prior notice.',
  'If changes in weight or dimensions occur, quote will be revised accordingly.',
  'Pick up does not include residential, pallet jack, or special handling.',
  'Items not specified do not cover actual expenses billed per receipt, nor do they include VAT.',
]

const PAYMENT_PRESETS = [
  'Credit 30 Days',
  '15 days from invoice date',
  'Cash Before Delivery (CBD)',
  'Cash on Delivery (COD)',
]

const INSURANCE_PRESETS = [
  'Declined (Buyer/Shipper bears risk)',
  'Included in quoted rate',
  'Optional (Available upon request at 0.15% CIF value)',
]

export function ModularQuotationSections({ editable, data, onChange }: ModularQuotationSectionsProps) {
  const [dropdownOpen, setDropdownOpen] = useState(false)

  // Dimensions calculations
  const totalQty = data.dimensionItems.reduce((acc, it) => acc + (Number(it.quantity) || 0), 0)
  const totalCbm = data.dimensionItems.reduce((acc, it) => {
    const vol = ((Number(it.lengthCm) || 0) * (Number(it.widthCm) || 0) * (Number(it.heightCm) || 0)) / 1_000_000
    return acc + vol * (Number(it.quantity) || 0)
  }, 0)
  const totalGrossWeight = data.dimensionItems.reduce((acc, it) => acc + (Number(it.grossWeightKg) || 0), 0)
  const volumetricAirWeight = data.dimensionItems.reduce((acc, it) => {
    const volWeight = ((Number(it.lengthCm) || 0) * (Number(it.widthCm) || 0) * (Number(it.heightCm) || 0)) / 6000
    return acc + volWeight * (Number(it.quantity) || 0)
  }, 0)

  const addDimensionRow = () => {
    onChange({
      dimensionItems: [
        ...data.dimensionItems,
        { quantity: 1, lengthCm: 100, widthCm: 100, heightCm: 100, grossWeightKg: 100 },
      ],
    })
  }

  const updateDimensionRow = (idx: number, patch: Partial<QuotationDimensionItem>) => {
    const next = [...data.dimensionItems]
    next[idx] = { ...next[idx], ...patch }
    onChange({ dimensionItems: next })
  }

  const removeDimensionRow = (idx: number) => {
    onChange({ dimensionItems: data.dimensionItems.filter((_, i) => i !== idx) })
  }

  const appendTermPreset = (preset: string) => {
    const existing = data.termsAndConditions.trim()
    const next = existing ? `${existing}\n• ${preset}` : `• ${preset}`
    onChange({ termsAndConditions: next })
  }

  return (
    <div className="flex flex-col gap-3">
      {/* Header and Add Section Dropdown (in edit mode) */}
      {editable && (
        <div className="flex items-center justify-between flex-wrap gap-2 border-t border-border pt-3">
          <div>
            <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
              Modular Sections (Optional)
            </h3>
            <p className="text-xs text-muted-foreground">
              Selectively add specific details for this quotation before sending to the customer.
            </p>
          </div>

          <div className="relative">
            <Button
              type="button"
              variant="outline"
              className="text-xs py-1 px-3 flex items-center gap-1.5"
              onClick={() => setDropdownOpen(!dropdownOpen)}
            >
              <span>+ Add Section</span>
              <span className="text-[10px]">▼</span>
            </Button>

            {dropdownOpen && (
              <div
                className="absolute right-0 mt-1 w-64 bg-card border border-border rounded-md shadow-lg py-1 z-30 flex flex-col text-xs"
                onMouseLeave={() => setDropdownOpen(false)}
              >
                {!data.showSchedule && (
                  <button
                    type="button"
                    className="text-left px-3 py-2 hover:bg-muted transition-colors flex items-center gap-2"
                    onClick={() => {
                      onChange({ showSchedule: true })
                      setDropdownOpen(false)
                    }}
                  >
                    <span>🚢 / ✈️</span>
                    <span>Transit & Schedule</span>
                  </button>
                )}
                {!data.showDimensions && (
                  <button
                    type="button"
                    className="text-left px-3 py-2 hover:bg-muted transition-colors flex items-center gap-2"
                    onClick={() => {
                      onChange({
                        showDimensions: true,
                        dimensionItems:
                          data.dimensionItems.length > 0
                            ? data.dimensionItems
                            : [{ quantity: 1, lengthCm: 120, widthCm: 80, heightCm: 100, grossWeightKg: 250 }],
                      })
                      setDropdownOpen(false)
                    }}
                  >
                    <span>📦</span>
                    <span>Cargo Dimensions & Packing</span>
                  </button>
                )}
                {!data.showPayment && (
                  <button
                    type="button"
                    className="text-left px-3 py-2 hover:bg-muted transition-colors flex items-center gap-2"
                    onClick={() => {
                      onChange({ showPayment: true, paymentTerms: data.paymentTerms || 'Credit 30 Days' })
                      setDropdownOpen(false)
                    }}
                  >
                    <span>💳</span>
                    <span>Payment & Credit Terms</span>
                  </button>
                )}
                {!data.showInsurance && (
                  <button
                    type="button"
                    className="text-left px-3 py-2 hover:bg-muted transition-colors flex items-center gap-2"
                    onClick={() => {
                      onChange({ showInsurance: true, insuranceStatus: data.insuranceStatus || 'Declined' })
                      setDropdownOpen(false)
                    }}
                  >
                    <span>🛡️</span>
                    <span>Cargo Insurance</span>
                  </button>
                )}
                {!data.showTerms && (
                  <button
                    type="button"
                    className="text-left px-3 py-2 hover:bg-muted transition-colors flex items-center gap-2"
                    onClick={() => {
                      onChange({
                        showTerms: true,
                        termsAndConditions:
                          data.termsAndConditions ||
                          '• Rates based on standard service level and availability.\n• Items not specified do not include VAT.',
                      })
                      setDropdownOpen(false)
                    }}
                  >
                    <span>📝</span>
                    <span>Terms & Conditions / Disclaimer</span>
                  </button>
                )}
                {data.showSchedule && data.showDimensions && data.showPayment && data.showInsurance && data.showTerms && (
                  <div className="px-3 py-2 text-muted-foreground italic text-center">
                    All modular sections added
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}

      {/* SECTION 1: Transit & Schedule */}
      {(data.showSchedule || (!editable && (data.carrierInfo || data.transitTime || data.frequency || data.closingSchedule))) && (
        <div className="border border-border/80 rounded-md p-3 bg-muted/15 flex flex-col gap-2.5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-foreground flex items-center gap-1.5">
              <span>🚢 / ✈️</span>
              <span>Routing, Carrier & Schedule</span>
            </span>
            {editable && (
              <button
                type="button"
                className="text-xs text-destructive hover:underline"
                onClick={() =>
                  onChange({
                    showSchedule: false,
                    transitTime: '',
                    frequency: '',
                    closingSchedule: '',
                    carrierInfo: '',
                  })
                }
              >
                Remove
              </button>
            )}
          </div>

          {editable ? (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 1.5, fontSize: '0.75rem' }}>
              <Input
                label="Carrier / Airline"
                placeholder="e.g. MSC / Hapag, Cargolux, Oceanblu"
                value={data.carrierInfo}
                onChange={(e) => onChange({ carrierInfo: e.target.value })}
              />
              <Input
                label="Transit Time (T/T)"
                placeholder="e.g. 3-4 Days, 35-45 Days"
                value={data.transitTime}
                onChange={(e) => onChange({ transitTime: e.target.value })}
              />
              <Input
                label="Sailing / Flight Frequency"
                placeholder="e.g. Daily, Weekly (Wed/Fri)"
                value={data.frequency}
                onChange={(e) => onChange({ frequency: e.target.value })}
              />
              <Input
                label="Cut-off / Closing Schedule"
                placeholder="e.g. Closing export entry & VGM Friday"
                value={data.closingSchedule}
                onChange={(e) => onChange({ closingSchedule: e.target.value })}
              />
            </Box>
          ) : (
            <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, 1fr)', sm: 'repeat(4, 1fr)' }, gap: 1.5, fontSize: '0.75rem' }}>
              {data.carrierInfo && (
                <div>
                  <p className="text-muted-foreground text-[10px]">Carrier</p>
                  <p className="font-medium">{data.carrierInfo}</p>
                </div>
              )}
              {data.transitTime && (
                <div>
                  <p className="text-muted-foreground text-[10px]">Transit Time</p>
                  <p className="font-medium">{data.transitTime}</p>
                </div>
              )}
              {data.frequency && (
                <div>
                  <p className="text-muted-foreground text-[10px]">Frequency</p>
                  <p className="font-medium">{data.frequency}</p>
                </div>
              )}
              {data.closingSchedule && (
                <div>
                  <p className="text-muted-foreground text-[10px]">Closing Cut-off</p>
                  <p className="font-medium">{data.closingSchedule}</p>
                </div>
              )}
            </Box>
          )}
        </div>
      )}

      {/* SECTION 2: Cargo Dimensions & Packing */}
      {(data.showDimensions || (!editable && data.dimensionItems.length > 0)) && (
        <div className="border border-border/80 rounded-md p-3 bg-muted/15 flex flex-col gap-2.5">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-foreground flex items-center gap-1.5">
              <span>📦</span>
              <span>Cargo Dimensions & Packing Details</span>
            </span>
            {editable && (
              <button
                type="button"
                className="text-xs text-destructive hover:underline"
                onClick={() => onChange({ showDimensions: false, dimensionItems: [] })}
              >
                Remove
              </button>
            )}
          </div>

          <div className="overflow-x-auto border border-border/60 rounded">
            <table className="table w-full text-xs">
              <thead>
                <tr className="bg-muted/40">
                  <th className="py-1.5 px-2 text-left w-16">Qty</th>
                  <th className="py-1.5 px-2 text-left">Length (cm)</th>
                  <th className="py-1.5 px-2 text-left">Width (cm)</th>
                  <th className="py-1.5 px-2 text-left">Height (cm)</th>
                  <th className="py-1.5 px-2 text-right">Gross Wt (kg)</th>
                  <th className="py-1.5 px-2 text-right">Vol (CBM)</th>
                  {editable && <th className="py-1.5 px-1 w-8 text-center"></th>}
                </tr>
              </thead>
              <tbody>
                {data.dimensionItems.map((item, idx) => {
                  const vol =
                    ((Number(item.lengthCm) || 0) * (Number(item.widthCm) || 0) * (Number(item.heightCm) || 0)) /
                    1_000_000
                  const itemCbm = vol * (Number(item.quantity) || 0)
                  return (
                    <tr key={idx} className="border-t border-border/40">
                      <td className="py-1 px-1">
                        {editable ? (
                          <input
                            type="number"
                            min="1"
                            className="input py-0.5 px-1.5 text-xs w-full text-center"
                            value={item.quantity}
                            onChange={(e) => updateDimensionRow(idx, { quantity: Number(e.target.value) || 1 })}
                          />
                        ) : (
                          item.quantity
                        )}
                      </td>
                      <td className="py-1 px-1">
                        {editable ? (
                          <input
                            type="number"
                            step="0.1"
                            className="input py-0.5 px-1.5 text-xs w-full"
                            value={item.lengthCm}
                            onChange={(e) => updateDimensionRow(idx, { lengthCm: Number(e.target.value) || 0 })}
                          />
                        ) : (
                          `${item.lengthCm} cm`
                        )}
                      </td>
                      <td className="py-1 px-1">
                        {editable ? (
                          <input
                            type="number"
                            step="0.1"
                            className="input py-0.5 px-1.5 text-xs w-full"
                            value={item.widthCm}
                            onChange={(e) => updateDimensionRow(idx, { widthCm: Number(e.target.value) || 0 })}
                          />
                        ) : (
                          `${item.widthCm} cm`
                        )}
                      </td>
                      <td className="py-1 px-1">
                        {editable ? (
                          <input
                            type="number"
                            step="0.1"
                            className="input py-0.5 px-1.5 text-xs w-full"
                            value={item.heightCm}
                            onChange={(e) => updateDimensionRow(idx, { heightCm: Number(e.target.value) || 0 })}
                          />
                        ) : (
                          `${item.heightCm} cm`
                        )}
                      </td>
                      <td className="py-1 px-1 text-right">
                        {editable ? (
                          <input
                            type="number"
                            step="0.1"
                            className="input py-0.5 px-1.5 text-xs text-right w-full"
                            value={item.grossWeightKg}
                            onChange={(e) => updateDimensionRow(idx, { grossWeightKg: Number(e.target.value) || 0 })}
                          />
                        ) : (
                          `${item.grossWeightKg.toLocaleString()} kg`
                        )}
                      </td>
                      <td className="py-1 px-2 text-right font-numeric">{itemCbm.toFixed(3)} m³</td>
                      {editable && (
                        <td className="py-1 px-1 text-center">
                          <button
                            type="button"
                            className="text-destructive font-bold hover:opacity-80"
                            onClick={() => removeDimensionRow(idx)}
                          >
                            ✕
                          </button>
                        </td>
                      )}
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between flex-wrap gap-2 text-xs">
            {editable && (
              <Button type="button" variant="outline" className="text-xs py-0.5 px-2.5" onClick={addDimensionRow}>
                + Add box / package
              </Button>
            )}
            <div className="flex items-center gap-3 text-muted-foreground ml-auto">
              <span>
                Total Units: <strong>{totalQty}</strong>
              </span>
              <span>
                Volume: <strong>{totalCbm.toFixed(3)} m³</strong>
              </span>
              <span>
                Gross: <strong>{totalGrossWeight.toLocaleString()} kg</strong>
              </span>
              <span>
                Air CW: <strong>{volumetricAirWeight.toFixed(1)} kg</strong>
              </span>
            </div>
          </div>
        </div>
      )}

      {/* SECTION 3 & 4: Commercial & Insurance (Side-by-side or combined) */}
      {(data.showPayment || data.showInsurance || (!editable && (data.paymentTerms || data.insuranceStatus))) && (
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }}>
          {(data.showPayment || (!editable && data.paymentTerms)) && (
            <div className="border border-border/80 rounded-md p-3 bg-muted/15 flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-foreground flex items-center gap-1.5">
                  <span>💳</span>
                  <span>Payment & Credit Terms</span>
                </span>
                {editable && (
                  <button
                    type="button"
                    className="text-xs text-destructive hover:underline"
                    onClick={() => onChange({ showPayment: false, paymentTerms: '' })}
                  >
                    Remove
                  </button>
                )}
              </div>
              {editable ? (
                <>
                  <Input
                    label="Payment Terms"
                    placeholder="e.g. Credit 30 Days"
                    value={data.paymentTerms}
                    onChange={(e) => onChange({ paymentTerms: e.target.value })}
                  />
                  <div className="flex items-center gap-1 flex-wrap">
                    {PAYMENT_PRESETS.map((p) => (
                      <button
                        key={p}
                        type="button"
                        className="text-[10px] py-0.5 px-2 rounded-full border border-border bg-card hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                        onClick={() => onChange({ paymentTerms: p })}
                      >
                        {p}
                      </button>
                    ))}
                  </div>
                </>
              ) : (
                <p className="text-xs font-medium">{data.paymentTerms}</p>
              )}
            </div>
          )}

          {(data.showInsurance || (!editable && data.insuranceStatus)) && (
            <div className="border border-border/80 rounded-md p-3 bg-muted/15 flex flex-col gap-2">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-foreground flex items-center gap-1.5">
                  <span>🛡️</span>
                  <span>Cargo Insurance</span>
                </span>
                {editable && (
                  <button
                    type="button"
                    className="text-xs text-destructive hover:underline"
                    onClick={() => onChange({ showInsurance: false, insuranceStatus: '' })}
                  >
                    Remove
                  </button>
                )}
              </div>
              {editable ? (
                <>
                  <Input
                    label="Insurance Status"
                    placeholder="e.g. Declined"
                    value={data.insuranceStatus}
                    onChange={(e) => onChange({ insuranceStatus: e.target.value })}
                  />
                  <div className="flex items-center gap-1 flex-wrap">
                    {INSURANCE_PRESETS.map((p) => (
                      <button
                        key={p}
                        type="button"
                        className="text-[10px] py-0.5 px-2 rounded-full border border-border bg-card hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                        onClick={() => onChange({ insuranceStatus: p })}
                      >
                        {p.split(' ')[0]}
                      </button>
                    ))}
                  </div>
                </>
              ) : (
                <p className="text-xs font-medium">{data.insuranceStatus}</p>
              )}
            </div>
          )}
        </Box>
      )}

      {/* SECTION 5: Terms & Conditions / Disclaimer */}
      {(data.showTerms || (!editable && data.termsAndConditions)) && (
        <div className="border border-border/80 rounded-md p-3 bg-muted/15 flex flex-col gap-2">
          <div className="flex items-center justify-between">
            <span className="text-xs font-bold text-foreground flex items-center gap-1.5">
              <span>📝</span>
              <span>Terms & Conditions / Disclaimer</span>
            </span>
            {editable && (
              <button
                type="button"
                className="text-xs text-destructive hover:underline"
                onClick={() => onChange({ showTerms: false, termsAndConditions: '' })}
              >
                Remove
              </button>
            )}
          </div>

          {editable ? (
            <>
              <textarea
                className="input text-xs w-full font-sans leading-relaxed"
                rows={3}
                placeholder="Specify quotation conditions, revision clauses, exclusions, etc."
                value={data.termsAndConditions}
                onChange={(e) => onChange({ termsAndConditions: e.target.value })}
              />
              <div className="flex flex-col gap-1">
                <span className="text-[10px] text-muted-foreground">Quick-add common forwarder clauses:</span>
                <div className="flex items-center gap-1 flex-wrap">
                  {PRESET_TERMS.map((preset, i) => (
                    <button
                      key={i}
                      type="button"
                      className="text-[10px] py-0.5 px-2 rounded-full border border-border bg-card hover:bg-muted text-muted-foreground hover:text-foreground transition-colors text-left"
                      onClick={() => appendTermPreset(preset)}
                      title={preset}
                    >
                      + {preset.slice(0, 32)}…
                    </button>
                  ))}
                </div>
              </div>
            </>
          ) : (
            <div className="text-xs text-muted-foreground whitespace-pre-line leading-relaxed">
              {data.termsAndConditions}
            </div>
          )}
        </div>
      )}
    </div>
  )
}
