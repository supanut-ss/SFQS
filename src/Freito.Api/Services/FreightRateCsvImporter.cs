using System.Globalization;
using System.Text;
using Freito.Api.Models;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Services;

public sealed class FreightRateCsvImporter(
    FreitoDbContext db,
    FreightRateService rates,
    AuditLogWriter audit)
{
    public const long MaxFileBytes = 2 * 1024 * 1024;

    private static readonly string[] RequiredColumns =
    [
        "origin_code", "destination_code", "mode", "direction", "carrier_code",
        "price_min", "price_max", "currency_code", "valid_from", "valid_to",
    ];

    public async Task<CsvImportResult> ImportAsync(IFormFile file, int actorId, CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return InvalidFile("Upload a .csv file.");
        if (file.Length is <= 0 or > MaxFileBytes) return InvalidFile($"CSV files must be between 1 byte and {MaxFileBytes} bytes.");

        using var reader = new StreamReader(file.OpenReadStream(), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var (document, parseError) = CsvDocument.Parse(content, RequiredColumns);
        if (parseError is not null || document is null) return InvalidFile(parseError ?? "The CSV could not be read.");

        var ports = (await db.Ports.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var carriers = (await db.Carriers.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var currencyCodes = new HashSet<string>(
            await db.Currencies.AsNoTracking().Select(x => x.Code).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        var candidates = new List<(CsvRow Row, FreightRate Rate)>();
        var issues = new List<CsvRowIssue>();

        foreach (var row in document.Rows)
        {
            var errors = new List<string>();
            var originCode = Required(row, "origin_code", errors);
            var destinationCode = Required(row, "destination_code", errors);
            var carrierCode = Required(row, "carrier_code", errors);
            var currencyCode = Required(row, "currency_code", errors).ToUpperInvariant();

            ports.TryGetValue(originCode, out var origin);
            ports.TryGetValue(destinationCode, out var destination);
            carriers.TryGetValue(carrierCode, out var carrier);
            if (origin is null) errors.Add($"Unknown origin code '{originCode}'.");
            if (destination is null) errors.Add($"Unknown destination code '{destinationCode}'.");
            if (carrier is null) errors.Add($"Unknown carrier code '{carrierCode}'.");

            var mode = ParseEnum<TransportMode>(Required(row, "mode", errors), "mode", errors);
            var direction = ParseEnum<ShipmentDirection>(Required(row, "direction", errors), "direction", errors);
            var priceMin = ParseDecimal(Required(row, "price_min", errors), "price_min", errors);
            var priceMax = ParseDecimal(Required(row, "price_max", errors), "price_max", errors);
            var validFrom = ParseDate(Required(row, "valid_from", errors), "valid_from", errors);
            var validTo = ParseDate(Required(row, "valid_to", errors), "valid_to", errors);
            var weightMin = ParseOptionalDecimal(document.Get(row, "weight_break_min"), "weight_break_min", errors);
            var weightMax = ParseOptionalDecimal(document.Get(row, "weight_break_max"), "weight_break_max", errors);
            var containerSize = document.Get(row, "container_size");

            if (!currencyCodes.Contains(currencyCode))
                errors.Add($"Unknown currency code '{currencyCode}'.");

            if (errors.Count > 0 || origin is null || destination is null || carrier is null)
            {
                issues.Add(new CsvRowIssue(row.Number, errors));
                continue;
            }

            candidates.Add((row, new FreightRate
            {
                OriginPortId = origin.Id,
                DestinationPortId = destination.Id,
                Mode = mode,
                Direction = direction,
                CarrierId = carrier.Id,
                ContainerSize = string.IsNullOrWhiteSpace(containerSize) ? null : containerSize.Trim().ToUpperInvariant(),
                WeightBreakMin = weightMin,
                WeightBreakMax = weightMax,
                PriceMin = priceMin,
                PriceMax = priceMax,
                CurrencyCode = currencyCode,
                ValidFrom = validFrom,
                ValidTo = UtcEndOfDay(validTo),
                IsActive = true,
            }));
        }

        await using var transaction = await rates.BeginWriteTransactionAsync(cancellationToken);
        var validations = await rates.ValidateBatchAsync(
            candidates.Select(x => x.Rate).ToArray(), cancellationToken, allowExistingUpdate: true);
        for (var index = 0; index < validations.Count; index++)
        {
            var validation = validations[index];
            if (!validation.IsValid) issues.Add(new CsvRowIssue(candidates[index].Row.Number, validation.Errors));
        }

        if (issues.Count > 0) return new CsvImportResult(0, issues.OrderBy(x => x.Row).ToArray());

        var routeKeys = candidates.Select(x => (x.Rate.OriginPortId, x.Rate.DestinationPortId, x.Rate.Mode, x.Rate.Direction, x.Rate.CarrierId))
            .Distinct().ToList();
        var originIds = routeKeys.Select(x => x.OriginPortId).Distinct().ToList();
        var destinationIds = routeKeys.Select(x => x.DestinationPortId).Distinct().ToList();
        var modes = routeKeys.Select(x => x.Mode).Distinct().ToList();
        var directions = routeKeys.Select(x => x.Direction).Distinct().ToList();
        var carrierIds = routeKeys.Select(x => x.CarrierId).Distinct().ToList();

        var existingRates = await db.FreightRates
            .Where(x => x.IsActive && originIds.Contains(x.OriginPortId) && destinationIds.Contains(x.DestinationPortId) &&
                modes.Contains(x.Mode) && directions.Contains(x.Direction) && carrierIds.Contains(x.CarrierId))
            .ToListAsync(cancellationToken);

        var changes = new List<PendingAuditChange>(candidates.Count);
        foreach (var (_, rate) in candidates)
        {
            var match = existingRates.FirstOrDefault(other =>
                other.OriginPortId == rate.OriginPortId &&
                other.DestinationPortId == rate.DestinationPortId &&
                other.Mode == rate.Mode &&
                other.Direction == rate.Direction &&
                other.CarrierId == rate.CarrierId &&
                FreightRateService.SameRateSlot(other, rate) &&
                rate.ValidFrom.Date <= other.ValidTo.Date && other.ValidFrom.Date <= rate.ValidTo.Date);

            if (match is not null)
            {
                var before = AuditLogWriter.Snapshot(match);
                match.PriceMin = rate.PriceMin;
                match.PriceMax = rate.PriceMax;
                match.CurrencyCode = rate.CurrencyCode;
                match.ValidFrom = rate.ValidFrom;
                match.ValidTo = rate.ValidTo;
                changes.Add(PendingAuditChange.Updated("FreightRate", match.Id, before, match));
            }
            else
            {
                db.FreightRates.Add(rate);
                existingRates.Add(rate);
                changes.Add(PendingAuditChange.Created("FreightRate", rate, () => rate.Id));
            }
        }

        await audit.SaveAsync(actorId, changes, cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return new CsvImportResult(candidates.Count, Array.Empty<CsvRowIssue>());
    }

    private static string Required(CsvRow row, string name, List<string> errors)
    {
        var value = row.Values.TryGetValue(name, out var found) ? found.Trim() : string.Empty;
        if (value.Length == 0) errors.Add($"'{name}' is required.");
        return value;
    }

    private static T ParseEnum<T>(string value, string field, List<string> errors) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out var parsed) && Enum.IsDefined(parsed)) return parsed;
        var validOptions = string.Join(", ", Enum.GetNames<T>());
        errors.Add($"'{field}' must be one of: {validOptions} (received '{value}').");
        return default;
    }

    private static decimal ParseDecimal(string value, string field, List<string> errors)
    {
        if (decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        errors.Add($"'{field}' must be a decimal number using a dot as the decimal separator.");
        return 0;
    }

    private static decimal? ParseOptionalDecimal(string? value, string field, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return ParseDecimal(value, field, errors);
    }

    private static DateTime ParseDate(string value, string field, List<string> errors)
    {
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            return DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        errors.Add($"'{field}' must use YYYY-MM-DD format.");
        return default;
    }

    private static DateTime UtcEndOfDay(DateTime value) => DateTime.SpecifyKind(value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

    private static CsvImportResult InvalidFile(string message) =>
        new(0, [new CsvRowIssue(1, [message])]);
}
