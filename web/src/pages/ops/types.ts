// Mirrors src/Freito.Domain/Entities/*.cs and src/Freito.Api/Models/*.cs (T4/T8). Kept in sync
// by hand — no shared codegen between the .NET and TS projects yet.

export type TransportMode = 'fcl' | 'lcl' | 'air'
export type ShipmentDirection = 'import' | 'export'
export type PortType = 'sea' | 'air'
export type CarrierType = 'shippingLine' | 'airline'
export type ChargeSide = 'origin' | 'destination'
export type ChargeCalcBasis = 'perShipment' | 'perContainer' | 'perRevenueTon' | 'perCBM' | 'perKG' | 'notQuotable'
export type UserRole = 'Sale' | 'Operation' | 'Admin'

export interface Port {
  id: number
  code: string
  name: string
  city: string
  country: string
  type: PortType
}

export interface Carrier {
  id: number
  code: string
  name: string
  type: CarrierType
}

export interface Currency {
  code: string
  name: string
  decimalDigits: number
}

export interface ExchangeRate {
  id: number
  currencyCode: string
  rateToBase: number
  effectiveDate: string
}

export interface FreightRate {
  id: number
  originPortId: number
  destinationPortId: number
  mode: TransportMode
  direction: ShipmentDirection
  carrierId: number
  containerSize: string | null
  weightBreakMin: number | null
  weightBreakMax: number | null
  priceMin: number
  priceMax: number
  currencyCode: string
  validFrom: string
  validTo: string
  isActive: boolean
}

export interface LocalCharge {
  id: number
  portId: number
  direction: ShipmentDirection
  mode: TransportMode
  chargeType: string
  calcBasis: ChargeCalcBasis
  amountMin: number
  amountMax: number
  minimumCharge: number | null
  currencyCode: string
  chargeSide: ChargeSide
}

export interface OpsUser {
  id: number
  email: string
  role: UserRole
  isActive: boolean
  createdAt: string
}

export interface AuditLog {
  id: number
  entity: string
  entityId: number
  action: string
  changedByUserId: number
  changedAt: string
  beforeJson: string | null
  afterJson: string | null
}

export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export interface CsvRowIssue {
  row: number
  errors: string[]
}

export interface CsvImportResult {
  imported: number
  errors: CsvRowIssue[]
}
