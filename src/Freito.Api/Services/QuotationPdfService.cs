using Freito.Api.Quotes.PdfExport;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace Freito.Api.Services;

public enum QuotationPdfOutcome
{
    Success,
    NotFound,
    NotYetApproved,
}

public sealed record QuotationPdfResult(QuotationPdfOutcome Outcome, byte[]? Bytes = null, string? FileName = null);

public sealed class QuotationPdfService(FreitoDbContext db)
{
    /// <summary>
    /// Only ApprovedAndSent/Confirmed quotes can be downloaded — a Draft or PendingSaleApproval
    /// price is exactly what the approval gate (AC3, requirements.md §6) exists to keep away
    /// from customers, and a downloadable PDF would let Sale email it around that gate.
    /// </summary>
    public async Task<QuotationPdfResult> GeneratePdfAsync(int quotationId, CancellationToken cancellationToken)
    {
        var quotation = await db.Quotations.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quotationId, cancellationToken);
        if (quotation is null) return new QuotationPdfResult(QuotationPdfOutcome.NotFound);
        if (quotation.Status is not (QuotationStatus.ApprovedAndSent or QuotationStatus.Confirmed))
            return new QuotationPdfResult(QuotationPdfOutcome.NotYetApproved);

        var lines = await db.QuotationLines.AsNoTracking()
            .Where(l => l.QuotationId == quotationId).ToListAsync(cancellationToken);
        var origin = await db.Ports.AsNoTracking().FirstAsync(p => p.Id == quotation.OriginPortId, cancellationToken);
        var destination = await db.Ports.AsNoTracking().FirstAsync(p => p.Id == quotation.DestinationPortId, cancellationToken);
        var cargoType = await db.CargoTypes.AsNoTracking().FirstAsync(c => c.Id == quotation.CargoTypeId, cancellationToken);
        var incoterm = await db.Incoterms.AsNoTracking().FirstAsync(i => i.Code == quotation.IncotermCode, cancellationToken);
        var approver = quotation.ApprovedByUserId is int approverId
            ? await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == approverId, cancellationToken)
            : null;

        var model = new QuotationPdfModel(
            QuoteNo: quotation.QuoteNo,
            CustomerName: quotation.CustomerName,
            CustomerCompany: quotation.CustomerCompany,
            CustomerEmail: quotation.CustomerEmail,
            CustomerPhone: quotation.CustomerPhone,
            OriginPortName: $"{origin.Name} ({origin.Code})",
            DestinationPortName: $"{destination.Name} ({destination.Code})",
            Direction: quotation.Direction.ToString(),
            Mode: quotation.Mode.ToString(),
            CargoTypeName: cargoType.Name,
            IncotermCode: incoterm.Code,
            IncotermName: incoterm.Name,
            ReadyDate: quotation.ReadyDate,
            ExpiresAt: quotation.ExpiresAt,
            Status: quotation.Status.ToString(),
            QuoteCurrency: quotation.QuoteCurrency,
            Lines: lines.Select(l => new QuotationPdfLine(l.Description, l.Basis, l.Amount, l.Currency)).ToList(),
            Subtotal: quotation.Subtotal,
            DiscountAmount: quotation.DiscountAmount,
            FinalPrice: quotation.FinalPrice,
            ApprovedByName: approver?.Email,
            ApprovedAt: quotation.ApprovedAt);

        var bytes = new QuotationPdfDocument(model).GeneratePdf();
        return new QuotationPdfResult(QuotationPdfOutcome.Success, bytes, $"{quotation.QuoteNo}.pdf");
    }
}
