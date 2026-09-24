// Mirrors src/Freito.Domain/Entities/Quotation.cs, QuotationLine.cs and
// src/Freito.Api/Models/QuoteRequests.cs (RefreshRateResponse). Kept in sync by hand.

import type { ShipmentDirection, TransportMode } from '../ops/types'

export type QuotationStatus = 'draft' | 'pendingSaleApproval' | 'approved' | 'approvedAndSent' | 'confirmed' | 'rejected' | 'expired'
export type RateSource = 'system' | 'refreshed' | 'manual'

export interface Quotation {
  id: number
  quoteNo: string
  customerName: string
  customerCompany: string | null
  customerEmail: string
  customerPhone: string
  originPortId: number
  destinationPortId: number
  direction: ShipmentDirection
  mode: TransportMode
  cargoTypeId: number
  qty: number
  containerSize: string | null
  cbm: number | null
  weightKg: number | null
  incotermCode: string
  readyDate: string
  quoteCurrency: string
  fxRateUsed: number
  rateSource: RateSource
  freightCost: number
  localChargeTotal: number
  subtotal: number
  discountAmount: number
  finalPrice: number
  status: QuotationStatus
  version: number
  createdByUserId: number | null
  approvedByUserId: number | null
  approvedAt: string | null
  sentAt: string | null
  expiresAt: string
}

export interface QuotationLine {
  id: number
  quotationId: number
  description: string
  basis: string
  unitPrice: number
  qty: number
  amount: number
  currency: string
  sourceRateId: number | null
  sourceLocalChargeId: number | null
}

export interface QuotationDetail {
  quotation: Quotation
  lines: QuotationLine[]
}

export interface RefreshRateLineDelta {
  chargeType: string
  previousAmount: number
  currentAmount: number
  delta: number
}

export interface RefreshRateResponse {
  previousFreightCost: number
  currentFreightCost: number
  freightDelta: number
  previousSubtotal: number
  currentSubtotal: number
  subtotalDelta: number
  currency: string
  currentRateFound: boolean
  localChargeDeltas: RefreshRateLineDelta[]
}
