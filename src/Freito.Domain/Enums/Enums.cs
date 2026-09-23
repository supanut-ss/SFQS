namespace Freito.Domain.Enums;

/// <summary>Sea (FCL/LCL) vs Air shipment.</summary>
public enum TransportMode
{
    Fcl,
    Lcl,
    Air,
}

/// <summary>Which side of the trade the quote's customer is on.</summary>
public enum ShipmentDirection
{
    Import,
    Export,
}

/// <summary>Sea port vs airport, for <see cref="Freito.Domain.Entities.Port"/>.</summary>
public enum PortType
{
    Sea,
    Air,
}

/// <summary>Shipping line vs airline, for <see cref="Freito.Domain.Entities.Carrier"/>.</summary>
public enum CarrierType
{
    ShippingLine,
    Airline,
}

/// <summary>Which end of the route a local charge or Incoterm responsibility applies to.</summary>
public enum ChargeSide
{
    Origin,
    Destination,
}

/// <summary>Who is responsible for a charge under a given Incoterm.</summary>
public enum Payer
{
    Seller,
    Buyer,
}

/// <summary>
/// How a local charge is calculated. NotQuotable covers DDP-style charges that can't be
/// pre-priced (customs duty as % of cargo value, at-cost items, time-based charges like
/// detention/storage) — confirmed with Operation to be excluded from the quoted total
/// entirely and shown as a disclaimer instead. See technical-plan.md §2.
/// </summary>
public enum ChargeCalcBasis
{
    PerShipment,
    PerContainer,
    PerRevenueTon,
    PerCBM,
    PerKG,
    NotQuotable,
}

/// <summary>Where a quote's freight rate came from. Manual is the expected common path for
/// Air shipments over 500kg — see technical-plan.md §3.</summary>
public enum RateSource
{
    System,
    Refreshed,
    Manual,
}

/// <summary>Quotation lifecycle — see requirements.md §6. PendingSaleApproval is a mandatory
/// gate; a quote can never reach ApprovedAndSent without passing through it.</summary>
public enum QuotationStatus
{
    Draft,
    PendingSaleApproval,
    ApprovedAndSent,
    Confirmed,
    Rejected,
    Expired,
}

/// <summary>Internal user roles. No self-signup — created by Admin only.</summary>
public enum UserRole
{
    Sale,
    Operation,
    Admin,
}
