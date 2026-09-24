// Mirrors src/Freito.Api/Models/QuoteRequests.cs and MasterDataRequests.cs (T7/T4). Keep in
// sync by hand — no shared codegen between the .NET and TS projects yet.

export type TransportMode = 'fcl' | 'lcl' | 'air'
export type ShipmentDirection = 'import' | 'export'
export type PortType = 'sea' | 'air'
export type RateSource = 'system' | 'refreshed' | 'manual'

export interface Port {
  id: number
  code: string
  name: string
  city: string
  country: string
  type: PortType
}

export interface Incoterm {
  code: string
  name: string
  riskTransferPoint: string
  sellerPaysFreight: boolean
}

export interface CargoType {
  id: number
  name: string
  isDangerous: boolean
  isProhibited: boolean
}

export interface QuoteShipmentInput {
  mode: TransportMode
  direction: ShipmentDirection
  originPortId: number
  destinationPortId: number
  incotermCode: string
  readyDate: string // yyyy-mm-dd
  containerSize?: string
  containerQty?: number
  cbm?: number
  weightKg?: number
  actualWeightKg?: number
  volumeCm3?: number
}

export interface QuoteSubmitInput extends QuoteShipmentInput {
  customerName: string
  customerCompany?: string
  customerEmail: string
  customerPhone: string
  cargoTypeId: number
}

export interface LocalChargeLine {
  chargeType: string
  basis: string
  amount: number
  currency: string
  isNotQuotable: boolean
}

export interface FreightAlternative {
  carrierId: number
  price: number
  currency: string
}

export interface QuoteCalculationResult {
  quoteId: number | null
  quoteNo: string | null
  expiresAt: string | null
  rateFound: boolean
  rateSource: RateSource
  quoteCurrency: string
  fxRateUsed: number
  freightCost: number
  localChargeTotal: number
  subtotal: number
  chargeableWeightKg: number | null
  revenueTon: number | null
  localChargeLines: LocalChargeLine[]
  notQuotableNotes: string[]
  alternatives: FreightAlternative[]
  noRateFoundReason: string | null
}
