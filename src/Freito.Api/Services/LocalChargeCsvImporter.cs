using System.Globalization;
using System.Text;
using Freito.Api.Models;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Services;

public sealed class LocalChargeCsvImporter(
    FreitoDbContext db,
    LocalChargeService charges,
    AuditLogWriter audit)
{
    public const long MaxFileBytes = 2 * 1024 * 1024;

    private static readonly string[] RequiredColumns =
    [
        "port_code", "direction", "mode", "charge_type", "calc_basis",
        "amount_min", "amount_max", "currency_code", "charge_side",
    ];

    public async Task<CsvImportResult> ImportAsync(IFormFile file, int actorId, CancellationToken cancellationToken)
    {
        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return InvalidFile("Upload a .csv file.");
        if (file.Length is <= 0 or > FreightRateCsvImporter.MaxFileBytes)
            return InvalidFile($"CSV files must be between 1 byte and {FreightRateCsvImporter.MaxFileBytes} bytes.");

        using var reader = new StreamReader(file.OpenReadStream(), new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var (document, parseError) = CsvDocument.Parse(content, RequiredColumns);
        if (parseError is not null || document is null) return InvalidFile(parseError ?? "The CSV could not be read.");

        var ports = (await db.Ports.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
        var currencyCodes = new HashSet<string>(
            await db.Currencies.AsNoTracking().Select(x => x.Code).ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        var candidates = new List<(CsvRow Row, LocalCharge Charge)>();
        var issues = new List<CsvRowIssue>();

        foreach (var row in document.Rows)
        {
            var errors = new List<string>();
            var portCode = Required(row, "port_code", errors);
            var currencyCode = Required(row, "currency_code", errors).ToUpperInvariant();
            var chargeType = Required(row, "charge_type", errors);
            ports.TryGetValue(portCode, out var port);
            if (port is null) errors.Add($"Unknown port code '{portCode}'.");
            if (!currencyCodes.Contains(currencyCode)) errors.Add($"Unknown currency code '{currencyCode}'.");

            var direction = ParseEnum<ShipmentDirection>(Required(row, "direction", errors), "direction", errors);
            var mode = ParseEnum<TransportMode>(Required(row, "mode", errors), "mode", errors);
            var calcBasis = ParseEnum<ChargeCalcBasis>(Required(row, "calc_basis", errors), "calc_basis", errors);
            var chargeSide = ParseEnum<ChargeSide>(Required(row, "charge_side", errors), "charge_side", errors);
            var amountMin = ParseDecimal(Required(row, "amount_min", errors), "amount_min", errors);
            var amountMax = ParseDecimal(Required(row, "amount_max", errors), "amount_max", errors);
            var minimumCharge = ParseOptionalDecimal(document.Get(row, "minimum_charge"), "minimum_charge", errors);

            if (errors.Count > 0 || port is null)
            {
                issues.Add(new CsvRowIssue(row.Number, errors));
                continue;
            }

            candidates.Add((row, new LocalCharge
            {
                PortId = port.Id,
                Direction = direction,
                Mode = mode,
                ChargeType = chargeType,
                CalcBasis = calcBasis,
                AmountMin = amountMin,
                AmountMax = amountMax,
                MinimumCharge = minimumCharge,
                CurrencyCode = currencyCode,
                ChargeSide = chargeSide,
            }));
        }

        var validations = await charges.ValidateBatchAsync(candidates.Select(x => x.Charge).ToArray(), cancellationToken);
        for (var index = 0; index < validations.Count; index++)
        {
            var validation = validations[index];
            if (!validation.IsValid) issues.Add(new CsvRowIssue(candidates[index].Row.Number, validation.Errors));
        }

        if (issues.Count > 0) return new CsvImportResult(0, issues.OrderBy(x => x.Row).ToArray());

        var changes = new List<PendingAuditChange>(candidates.Count);
        foreach (var (_, charge) in candidates)
        {
            db.LocalCharges.Add(charge);
            changes.Add(PendingAuditChange.Created("LocalCharge", charge, () => charge.Id));
        }

        await audit.SaveAsync(actorId, changes, cancellationToken);
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
        errors.Add($"'{field}' must be a valid {typeof(T).Name} value.");
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

    private static CsvImportResult InvalidFile(string message) =>
        new(0, [new CsvRowIssue(1, [message])]);
}
