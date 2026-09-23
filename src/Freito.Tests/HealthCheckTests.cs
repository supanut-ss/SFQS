using Freito.Api.Controllers;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Freito.Tests;

/// <summary>
/// M0 placeholder — proves the test project can reference Api/Infrastructure
/// and exercise a controller against an in-memory provider. Real quote-engine
/// tests (T6) replace this once the schema lands in M1.
/// </summary>
public class HealthCheckTests
{
    private static FreitoDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FreitoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FreitoDbContext(options);
    }

    [Fact]
    public void Get_ReturnsOk()
    {
        var controller = new HealthController(CreateInMemoryContext());

        var result = controller.Get();

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Db_ReturnsOk_WhenDatabaseIsReachable()
    {
        var controller = new HealthController(CreateInMemoryContext());

        var result = await controller.Db();

        Assert.IsType<OkObjectResult>(result);
    }
}
