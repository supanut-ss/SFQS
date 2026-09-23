using Freito.Api.Models;
using Freito.Api.Services;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Controllers;

[ApiController]
[Route("api/rates")]
public sealed class RatesController(
    FreitoDbContext db,
    FreightRateService rates,
    FreightRateCsvImporter csvImporter,
    AuditLogWriter audit) : ProtectedControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] int? originPortId = null,
        [FromQuery] int? destinationPortId = null,
        [FromQuery] TransportMode? mode = null,
        [FromQuery] ShipmentDirection? direction = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] DateTime? validOn = null,
        CancellationToken cancellationToken = default)
    {
        var rejection = RequireRoles(UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest("Page must be positive and pageSize must be between 1 and 200.");

        var query = db.FreightRates.AsNoTracking();
        if (originPortId is not null) query = query.Where(x => x.OriginPortId == originPortId);
        if (destinationPortId is not null) query = query.Where(x => x.DestinationPortId == destinationPortId);
        if (mode is not null) query = query.Where(x => x.Mode == mode.Value);
        if (direction is not null) query = query.Where(x => x.Direction == direction.Value);
        if (isActive is not null) query = query.Where(x => x.IsActive == isActive.Value);
        if (validOn is not null)
        {
            var date = UtcDate(validOn.Value);
            query = query.Where(x => x.ValidFrom <= date && x.ValidTo >= date);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.OriginPortId).ThenBy(x => x.DestinationPortId)
            .ThenBy(x => x.Mode).ThenBy(x => x.ValidFrom)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireRoles(UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        var rate = await db.FreightRates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return rate is null ? NotFound() : Ok(rate);
    }

    [HttpPost]
    public async Task<IActionResult> Create(FreightRateRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        await using var transaction = await rates.BeginWriteTransactionAsync(cancellationToken);
        var rate = ToEntity(request);
        var validation = await rates.ValidateAsync(rate, null, cancellationToken);
        if (!validation.IsValid) return Invalid(validation);

        db.FreightRates.Add(rate);
        await audit.SaveAsync(actorId, [PendingAuditChange.Created("FreightRate", rate, () => rate.Id)], cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = rate.Id }, rate);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, FreightRateRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        await using var transaction = await rates.BeginWriteTransactionAsync(cancellationToken);
        var previous = await db.FreightRates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (previous is null) return NotFound();
        if (!previous.IsActive) return Conflict("An inactive rate cannot be revised.");

        var revised = ToEntity(request);
        if (!HasSameSlot(previous, revised))
            return BadRequest("A rate revision must keep the same route, mode, direction, carrier, and container or weight bracket.");
        if (revised.ValidFrom.Date <= previous.ValidFrom.Date)
            return BadRequest("A rate revision must start after the existing rate's start date.");

        var validation = await rates.ValidateAsync(revised, previous.Id, cancellationToken);
        if (!validation.IsValid) return Invalid(validation);

        string? previousBefore = null;
        if (revised.ValidFrom.Date <= previous.ValidTo.Date)
        {
            previousBefore = AuditLogWriter.Snapshot(previous);
            previous.ValidTo = UtcEndOfDay(revised.ValidFrom.Date.AddDays(-1));
        }

        db.FreightRates.Add(revised);
        var changes = new List<PendingAuditChange>();
        if (previousBefore is not null)
            changes.Add(PendingAuditChange.Updated("FreightRate", previous.Id, previousBefore, previous));
        changes.Add(PendingAuditChange.Created("FreightRate", revised, () => revised.Id));
        await audit.SaveAsync(actorId, changes, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = revised.Id }, revised);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        var rate = await db.FreightRates.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rate is null) return NotFound();
        if (rate.IsActive)
        {
            var before = AuditLogWriter.Snapshot(rate);
            rate.IsActive = false;
            await audit.SaveAsync(actorId, [PendingAuditChange.Updated("FreightRate", rate.Id, before, rate)], cancellationToken);
        }

        return NoContent();
    }

    [HttpPost("import")]
    [RequestSizeLimit(FreightRateCsvImporter.MaxFileBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FreightRateCsvImporter.MaxFileBytes)]
    public async Task<IActionResult> Import([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (file is null) return BadRequest("A CSV file is required.");

        var result = await csvImporter.ImportAsync(file, actorId, cancellationToken);
        return result.Errors.Count > 0 ? UnprocessableEntity(result) : Ok(result);
    }

    private static FreightRate ToEntity(FreightRateRequest request) => new()
    {
        OriginPortId = request.OriginPortId,
        DestinationPortId = request.DestinationPortId,
        Mode = request.Mode!.Value,
        Direction = request.Direction!.Value,
        CarrierId = request.CarrierId,
        ContainerSize = string.IsNullOrWhiteSpace(request.ContainerSize) ? null : request.ContainerSize.Trim().ToUpperInvariant(),
        WeightBreakMin = request.WeightBreakMin,
        WeightBreakMax = request.WeightBreakMax,
        PriceMin = request.PriceMin,
        PriceMax = request.PriceMax,
        CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
        ValidFrom = UtcDate(request.ValidFrom!.Value),
        ValidTo = UtcEndOfDay(request.ValidTo!.Value),
        IsActive = true,
    };

    private static bool HasSameSlot(FreightRate existing, FreightRate revised) =>
        existing.OriginPortId == revised.OriginPortId &&
        existing.DestinationPortId == revised.DestinationPortId &&
        existing.Mode == revised.Mode &&
        existing.Direction == revised.Direction &&
        existing.CarrierId == revised.CarrierId &&
        string.Equals(existing.ContainerSize, revised.ContainerSize, StringComparison.OrdinalIgnoreCase) &&
        existing.WeightBreakMin == revised.WeightBreakMin &&
        existing.WeightBreakMax == revised.WeightBreakMax;

    private static DateTime UtcDate(DateTime value) => DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

    private static DateTime UtcEndOfDay(DateTime value) => DateTime.SpecifyKind(value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

    private static IActionResult Invalid(EntityValidationResult validation) => validation.IsConflict
        ? new ConflictObjectResult(new { errors = validation.Errors })
        : new BadRequestObjectResult(new { errors = validation.Errors });
}
