using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Freito.Infrastructure;

namespace Freito.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly FreitoDbContext _db;

    public HealthController(FreitoDbContext db) => _db = db;

    /// <summary>Liveness check — API is up, no DB required.</summary>
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ok", service = "Freito.Api", timeUtc = DateTime.UtcNow });

    /// <summary>Readiness check — confirms the API can actually reach MySQL on this host.</summary>
    [HttpGet("db")]
    public async Task<IActionResult> Db()
    {
        var canConnect = await _db.Database.CanConnectAsync();
        return canConnect
            ? Ok(new { status = "ok", database = "reachable" })
            : StatusCode(503, new { status = "error", database = "unreachable" });
    }
}
