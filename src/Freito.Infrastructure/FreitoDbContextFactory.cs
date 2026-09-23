using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Freito.Infrastructure;

public sealed class FreitoDbContextFactory : IDesignTimeDbContextFactory<FreitoDbContext>
{
    public FreitoDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost;Database=freito_design;User ID=design_only;Password=design_only;";
        var options = new DbContextOptionsBuilder<FreitoDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new FreitoDbContext(options);
    }
}
