using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
public sealed class AuditLogsController(FreitoDbContext db) : ProtectedControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? entity,
        [FromQuery] int? entityId,
        [FromQuery] int? changedByUserId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var rejection = RequireRoles(UserRole.Operation, UserRole.Admin);
        if (rejection is not null) return rejection;
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest("Page must be positive and pageSize must be between 1 and 200.");
        if (from is not null && to is not null && from.Value > to.Value) return BadRequest("The from date cannot be after the to date.");

        var query = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(entity)) query = query.Where(x => x.Entity == entity.Trim());
        if (entityId is not null) query = query.Where(x => x.EntityId == entityId.Value);
        if (changedByUserId is not null) query = query.Where(x => x.ChangedByUserId == changedByUserId.Value);
        if (from is not null) query = query.Where(x => x.ChangedAt >= from.Value);
        if (to is not null) query = query.Where(x => x.ChangedAt <= to.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.ChangedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(new { items, page, pageSize, total });
    }
}
