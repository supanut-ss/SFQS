using Freito.Api.Models;
using Freito.Api.Services;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Controllers;

[ApiController]
[Route("api/local-charges")]
public sealed class LocalChargesController(
    FreitoDbContext db,
    LocalChargeService charges,
    LocalChargeCsvImporter csvImporter,
    AuditLogWriter audit) : ProtectedControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] int? portId = null,
        [FromQuery] TransportMode? mode = null,
        [FromQuery] ShipmentDirection? direction = null,
        [FromQuery] ChargeSide? chargeSide = null,
        CancellationToken cancellationToken = default)
    {
        var rejection = RequireRoles(UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest("Page must be positive and pageSize must be between 1 and 200.");

        var query = db.LocalCharges.AsNoTracking();
        if (portId is not null) query = query.Where(x => x.PortId == portId.Value);
        if (mode is not null) query = query.Where(x => x.Mode == mode.Value);
        if (direction is not null) query = query.Where(x => x.Direction == direction.Value);
        if (chargeSide is not null) query = query.Where(x => x.ChargeSide == chargeSide.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.PortId).ThenBy(x => x.Mode).ThenBy(x => x.ChargeType)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireRoles(UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        var charge = await db.LocalCharges.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return charge is null ? NotFound() : Ok(charge);
    }

    [HttpPost]
    public async Task<IActionResult> Create(LocalChargeRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        var charge = LocalChargeService.ToEntity(request);
        var validation = await charges.ValidateAsync(charge, null, cancellationToken);
        if (!validation.IsValid) return Invalid(validation);

        db.LocalCharges.Add(charge);
        await audit.SaveAsync(actorId, [PendingAuditChange.Created("LocalCharge", charge, () => charge.Id)], cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = charge.Id }, charge);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, LocalChargeRequest request, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        var charge = await db.LocalCharges.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (charge is null) return NotFound();

        var revised = LocalChargeService.ToEntity(request);
        var validation = await charges.ValidateAsync(revised, charge.Id, cancellationToken);
        if (!validation.IsValid) return Invalid(validation);

        var before = AuditLogWriter.Snapshot(charge);
        charge.PortId = revised.PortId;
        charge.Direction = revised.Direction;
        charge.Mode = revised.Mode;
        charge.ChargeType = revised.ChargeType;
        charge.CalcBasis = revised.CalcBasis;
        charge.AmountMin = revised.AmountMin;
        charge.AmountMax = revised.AmountMax;
        charge.MinimumCharge = revised.MinimumCharge;
        charge.CurrencyCode = revised.CurrencyCode;
        charge.ChargeSide = revised.ChargeSide;
        await audit.SaveAsync(actorId, [PendingAuditChange.Updated("LocalCharge", charge.Id, before, charge)], cancellationToken);
        return Ok(charge);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        var charge = await db.LocalCharges.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (charge is null) return NotFound();
        if (await db.QuotationLines.AnyAsync(x => x.SourceLocalChargeId == id, cancellationToken))
            return Conflict("A local charge referenced by a quotation cannot be deleted.");
        var before = AuditLogWriter.Snapshot(charge);
        db.LocalCharges.Remove(charge);
        await audit.SaveAsync(actorId, [PendingAuditChange.Deleted("LocalCharge", charge.Id, before)], cancellationToken);
        return NoContent();
    }

    [HttpPost("import")]
    [RequestSizeLimit(LocalChargeCsvImporter.MaxFileBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LocalChargeCsvImporter.MaxFileBytes)]
    public async Task<IActionResult> Import([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        var rejection = RequireActor(out var actorId, UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (file is null) return BadRequest("A CSV file is required.");

        var result = await csvImporter.ImportAsync(file, actorId, cancellationToken);
        return result.Errors.Count > 0 ? UnprocessableEntity(result) : Ok(result);
    }

    private static IActionResult Invalid(EntityValidationResult validation) => validation.IsConflict
        ? new ConflictObjectResult(new { errors = validation.Errors })
        : new BadRequestObjectResult(new { errors = validation.Errors });
}
