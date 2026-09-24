using System.Text;
using Freito.Api.Services;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Freito.Tests;

public class CsvImporterUpsertTests
{
    private static FreitoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FreitoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new FreitoDbContext(options);

        db.Ports.AddRange(
            new Port { Id = 1, Code = "THBKK", Name = "Bangkok Port", City = "Bangkok", Country = "Thailand", Type = PortType.Sea },
            new Port { Id = 2, Code = "SGSIN", Name = "Singapore Port", City = "Singapore", Country = "Singapore", Type = PortType.Sea });
        db.Carriers.Add(new Carrier { Id = 1, Code = "MAERSK", Name = "Maersk", Type = CarrierType.ShippingLine });
        db.Currencies.AddRange(
            new Currency { Code = "USD", Name = "US Dollar", DecimalDigits = 2 },
            new Currency { Code = "THB", Name = "Thai Baht", DecimalDigits = 2 });
        db.FreightRates.Add(new FreightRate
        {
            Id = 1, OriginPortId = 1, DestinationPortId = 2, Mode = TransportMode.Fcl,
            Direction = ShipmentDirection.Export, CarrierId = 1, ContainerSize = "40",
            PriceMin = 200m, PriceMax = 200m, CurrencyCode = "USD",
            ValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ValidTo = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true,
        });
        db.LocalCharges.Add(new LocalCharge
        {
            Id = 1, PortId = 1, Direction = ShipmentDirection.Export, Mode = TransportMode.Fcl,
            ChargeType = "THC", CalcBasis = ChargeCalcBasis.PerContainer,
            AmountMin = 1000m, AmountMax = 1000m, CurrencyCode = "THB", ChargeSide = ChargeSide.Origin,
        });
        db.SaveChanges();
        return db;
    }

    private static IFormFile CreateCsvFile(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", "import.csv");
    }

    [Fact]
    public async Task LocalChargeCsvImporter_WhenChargeAlreadyExists_UpdatesExistingCharge()
    {
        using var db = CreateContext();
        var audit = new AuditLogWriter(db);
        var service = new LocalChargeService(db);
        var importer = new LocalChargeCsvImporter(db, service, audit);

        // CSV with existing THC charge updated to 1500-1800, plus a new D/O charge
        var csv = """
            port_code,direction,mode,charge_type,calc_basis,amount_min,amount_max,currency_code,charge_side,minimum_charge
            THBKK,Export,Fcl,THC,PerContainer,1500,1800,THB,Origin,
            THBKK,Export,Fcl,D/O,PerShipment,500,500,THB,Origin,
            """;

        var file = CreateCsvFile(csv);
        var result = await importer.ImportAsync(file, actorId: 99, CancellationToken.None);

        Assert.Equal(2, result.Imported);
        Assert.Empty(result.Errors);

        // THC should be updated in place (ID = 1), not duplicated
        var charges = await db.LocalCharges.ToListAsync();
        Assert.Equal(2, charges.Count);

        var thc = charges.First(c => c.ChargeType == "THC");
        Assert.Equal(1, thc.Id);
        Assert.Equal(1500m, thc.AmountMin);
        Assert.Equal(1800m, thc.AmountMax);

        var dO = charges.First(c => c.ChargeType == "D/O");
        Assert.Equal(500m, dO.AmountMin);
    }

    [Fact]
    public async Task FreightRateCsvImporter_WhenRateAlreadyExists_UpdatesExistingRate()
    {
        using var db = CreateContext();
        var audit = new AuditLogWriter(db);
        var service = new FreightRateService(db);
        var importer = new FreightRateCsvImporter(db, service, audit);

        // CSV with existing 40ft rate updated from 200 to 350-400
        var csv = """
            origin_code,destination_code,mode,direction,carrier_code,price_min,price_max,currency_code,valid_from,valid_to,container_size
            THBKK,SGSIN,Fcl,Export,MAERSK,350,400,USD,2025-01-01,2030-01-01,40
            """;

        var file = CreateCsvFile(csv);
        var result = await importer.ImportAsync(file, actorId: 99, CancellationToken.None);

        Assert.Equal(1, result.Imported);
        Assert.Empty(result.Errors);

        var rates = await db.FreightRates.ToListAsync();
        Assert.Single(rates);
        Assert.Equal(1, rates[0].Id);
        Assert.Equal(350m, rates[0].PriceMin);
        Assert.Equal(400m, rates[0].PriceMax);
    }
}
