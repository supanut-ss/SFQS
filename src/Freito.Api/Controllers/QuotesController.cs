using Freito.Api.Models;
using Freito.Api.Services;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Controllers;

/// <summary>Guest-facing calculate/submit endpoints — no authentication, so guarded only by
/// input validation. Rate limiting/honeypot against spam is a separate concern, tracked as an
/// open risk in work-plan.md §E, not implemented here.</summary>
[ApiController]
[Route("api/public/quotes")]
public sealed class PublicQuotesController(QuotationService quotes) : ControllerBase
{
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate(QuoteShipmentRequest request, CancellationToken cancellationToken)
    {
        var validation = await quotes.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(new { errors = validation.Errors });

        var result = await quotes.ComputeAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Submit(QuoteSubmitRequest request, CancellationToken cancellationToken)
    {
        var validation = await quotes.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(new { errors = validation.Errors });

        var (quotation, computation) = await quotes.SubmitAsync(request, cancellationToken);
        return CreatedAtAction(nameof(QuotesController.Get), "Quotes", new { id = quotation.Id }, computation);
    }
}

/// <summary>Sale-facing quotation views. Gated by RequireRoles — until T8 wires up
/// authentication, every call here returns 401 (same interim state as T4's controllers).</summary>
[ApiController]
[Route("api/quotes")]
public sealed class QuotesController(FreitoDbContext db, QuotationService quotes, QuotationPdfService pdf) : ProtectedControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] QuotationStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var rejection = RequireRoles(UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest("Page must be positive and pageSize must be between 1 and 200.");

        var query = db.Quotations.AsNoTracking();
        if (status is not null) query = query.Where(q => q.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(q => q.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireRoles(UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;

        var quotation = await db.Quotations.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        if (quotation is null) return NotFound();

        var lines = await db.QuotationLines.AsNoTracking()
            .Where(l => l.QuotationId == id).ToListAsync(cancellationToken);
        return Ok(new { quotation, lines });
    }

    [HttpPost("{id:int}/refresh-rate")]
    public async Task<IActionResult> RefreshRate(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireRoles(UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;

        var delta = await quotes.RefreshRateAsync(id, cancellationToken);
        return delta is null ? NotFound() : Ok(delta);
    }

    /// <summary>Sale or Admin can edit quotation lines during review in Draft or PendingSaleApproval.</summary>
    [HttpPut("{id:int}/lines")]
    public async Task<IActionResult> UpdateLines(int id, UpdateQuoteLinesRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;

        var result = await quotes.UpdateLinesAsync(id, actorId, request.Lines, request.FinalPrice, request.Note, cancellationToken);
        return result.Outcome switch
        {
            QuotationActionOutcome.NotFound => NotFound(),
            QuotationActionOutcome.InvalidTransition => Conflict(result.Error),
            _ => Ok(new
            {
                quotation = result.Quotation,
                lines = await db.QuotationLines.AsNoTracking().Where(l => l.QuotationId == id).ToListAsync(cancellationToken),
            }),
        };
    }

    /// <summary>Sale or Admin can approve/reject quotations per business permissions.
    /// Listing/detail/pdf are also open to both.</summary>
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, ApproveQuoteRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;

        var result = await quotes.ApproveAsync(id, actorId, request.FinalPrice, request.Note, cancellationToken, request.Lines);
        return result.Outcome switch
        {
            QuotationActionOutcome.NotFound => NotFound(),
            QuotationActionOutcome.InvalidTransition => Conflict(result.Error),
            _ => Ok(result.Quotation),
        };
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectQuoteRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;

        var result = await quotes.RejectAsync(id, actorId, request.Note, cancellationToken);
        return result.Outcome switch
        {
            QuotationActionOutcome.NotFound => NotFound(),
            QuotationActionOutcome.InvalidTransition => Conflict(result.Error),
            _ => Ok(result.Quotation),
        };
    }

    /// <summary>Sale or Admin manually dispatches quotation to customer.</summary>
    [HttpPost("{id:int}/send")]
    public async Task<IActionResult> Send(int id, SendQuoteRequest? request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;

        var result = await quotes.SendAsync(id, actorId, request?.Note, cancellationToken);
        return result.Outcome switch
        {
            QuotationActionOutcome.NotFound => NotFound(),
            QuotationActionOutcome.InvalidTransition => Conflict(result.Error),
            _ => Ok(result.Quotation),
        };
    }

    /// <summary>T9 — the PDF Sale downloads and attaches to their own email/Outlook (no SMTP
    /// integration in-system, technical-plan.md §7). Only ApprovedAndSent/Confirmed quotes can
    /// be downloaded — see QuotationPdfService's doc comment for why.</summary>
    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> DownloadPdf(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireRoles(UserRole.Sale, UserRole.Admin);
        if (rejection is not null) return rejection;

        var result = await pdf.GeneratePdfAsync(id, cancellationToken);
        return result.Outcome switch
        {
            QuotationPdfOutcome.NotFound => NotFound(),
            QuotationPdfOutcome.NotYetApproved => Conflict("Only an approved quotation can be downloaded as a PDF."),
            _ => File(result.Bytes!, "application/pdf", result.FileName),
        };
    }
}
