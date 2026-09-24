namespace Freito.Api.Quotes.PdfExport;

/// <summary>Flat, PDF-ready view of a quotation — resolved names instead of the raw foreign
/// keys the Quotation entity stores, so QuotationPdfDocument never touches EF or the DbContext
/// directly. Built by QuotationPdfService.</summary>
public sealed record QuotationPdfModel(
    string QuoteNo,
    string CustomerName,
    string? CustomerCompany,
    string CustomerEmail,
    string CustomerPhone,
    string OriginPortName,
    string DestinationPortName,
    string Direction,
    string Mode,
    string CargoTypeName,
    string IncotermCode,
    string IncotermName,
    DateTime ReadyDate,
    DateTime ExpiresAt,
    string Status,
    string QuoteCurrency,
    IReadOnlyList<QuotationPdfLine> Lines,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal FinalPrice,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    string? TransitTime = null,
    string? Frequency = null,
    string? ClosingSchedule = null,
    string? CarrierInfo = null,
    string? PaymentTerms = null,
    string? InsuranceStatus = null,
    string? TermsAndConditions = null,
    string? DimensionsJson = null);

public sealed record QuotationPdfLine(string Description, string Basis, decimal Amount, string Currency);

public sealed record QuotationDimensionItem(int Quantity, decimal LengthCm, decimal WidthCm, decimal HeightCm, decimal GrossWeightKg);
