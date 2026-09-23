using Freito.Api.Models;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Freito.Api.Services;

public sealed class LocalChargeService(FreitoDbContext db)
{
    private const decimal MaxStoredMoney = 99_999_999_999_999.9999m;

    public async Task<EntityValidationResult> ValidateAsync(LocalCharge charge, int? excludedId, CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(cancellationToken);
        return ValidateAgainst(charge, excludedId, context);
    }

    public async Task<IReadOnlyList<EntityValidationResult>> ValidateBatchAsync(
        IReadOnlyList<LocalCharge> charges,
        CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(cancellationToken);
        var results = new List<EntityValidationResult>(charges.Count);
        foreach (var charge in charges)
        {
            var result = ValidateAgainst(charge, null, context);
            results.Add(result);
            if (result.IsValid) context.ExistingCharges.Add(charge);
        }

        return results;
    }

    private async Task<ValidationContext> LoadContextAsync(CancellationToken cancellationToken)
    {
        var ports = await db.Ports.AsNoTracking().ToListAsync(cancellationToken);
        var currencies = await db.Currencies.AsNoTracking().Select(x => x.Code).ToListAsync(cancellationToken);
        var charges = await db.LocalCharges.AsNoTracking().ToListAsync(cancellationToken);
        return new ValidationContext(ports, currencies, charges);
    }

    private static EntityValidationResult ValidateAgainst(LocalCharge charge, int? excludedId, ValidationContext context)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(charge.Direction)) errors.Add("Direction is not supported.");
        if (!Enum.IsDefined(charge.Mode)) errors.Add("Mode is not supported.");
        if (!Enum.IsDefined(charge.CalcBasis)) errors.Add("Calculation basis is not supported.");
        if (!Enum.IsDefined(charge.ChargeSide)) errors.Add("Charge side is not supported.");
        if (charge.PortId <= 0) errors.Add("A valid port is required.");
        if (string.IsNullOrWhiteSpace(charge.ChargeType)) errors.Add("Charge type cannot be blank.");
        else if (charge.ChargeType.Length > 100) errors.Add("Charge type cannot exceed 100 characters.");
        if (charge.AmountMin < 0 || charge.AmountMax < charge.AmountMin) errors.Add("Amount range must be non-negative and AmountMax must be at least AmountMin.");
        if (charge.MinimumCharge is < 0m) errors.Add("MinimumCharge cannot be negative.");
        if (charge.AmountMin > MaxStoredMoney || charge.AmountMax > MaxStoredMoney || charge.MinimumCharge is > MaxStoredMoney)
            errors.Add("Charge amounts exceed the supported maximum amount.");
        if (decimal.Round(charge.AmountMin, 4) != charge.AmountMin || decimal.Round(charge.AmountMax, 4) != charge.AmountMax ||
            (charge.MinimumCharge is not null && decimal.Round(charge.MinimumCharge.Value, 4) != charge.MinimumCharge.Value))
            errors.Add("Charge amounts support at most four decimal places.");
        if (string.IsNullOrWhiteSpace(charge.CurrencyCode)) errors.Add("A currency is required.");
        else if (charge.CurrencyCode.Length > 3) errors.Add("Currency codes cannot exceed three characters.");

        var port = context.Ports.FirstOrDefault(x => x.Id == charge.PortId);
        var currencyExists = context.CurrencyCodes.Contains(charge.CurrencyCode, StringComparer.OrdinalIgnoreCase);
        if (port is null) errors.Add("Port does not exist.");
        if (!currencyExists) errors.Add("Currency does not exist.");

        if (port is not null && Enum.IsDefined(charge.Mode))
        {
            var expectedType = charge.Mode == TransportMode.Air ? PortType.Air : PortType.Sea;
            if (port.Type != expectedType) errors.Add($"{charge.Mode} charges require a {expectedType} port.");
        }

        var expectedBasisMode = charge.CalcBasis switch
        {
            ChargeCalcBasis.PerContainer => TransportMode.Fcl,
            ChargeCalcBasis.PerRevenueTon or ChargeCalcBasis.PerCBM => TransportMode.Lcl,
            ChargeCalcBasis.PerKG => TransportMode.Air,
            _ => (TransportMode?)null,
        };
        if (expectedBasisMode is not null && charge.Mode != expectedBasisMode)
            errors.Add($"{charge.CalcBasis} charges must use {expectedBasisMode} mode.");

        if (errors.Count > 0) return new EntityValidationResult(errors);

        var duplicate = context.ExistingCharges.FirstOrDefault(existing =>
            existing.Id != excludedId &&
            existing.PortId == charge.PortId &&
            existing.Direction == charge.Direction &&
            existing.Mode == charge.Mode &&
            existing.ChargeSide == charge.ChargeSide &&
            existing.CalcBasis == charge.CalcBasis &&
            existing.CurrencyCode.Equals(charge.CurrencyCode, StringComparison.OrdinalIgnoreCase) &&
            existing.ChargeType.Equals(charge.ChargeType, StringComparison.OrdinalIgnoreCase));
        return duplicate is null
            ? new EntityValidationResult(Array.Empty<string>())
            : new EntityValidationResult(["A charge with this port, direction, mode, charge type, basis, side, and currency already exists."], IsConflict: true);
    }

    public static LocalCharge ToEntity(LocalChargeRequest request) => new()
    {
        PortId = request.PortId,
        Direction = request.Direction!.Value,
        Mode = request.Mode!.Value,
        ChargeType = request.ChargeType.Trim(),
        CalcBasis = request.CalcBasis!.Value,
        AmountMin = request.AmountMin,
        AmountMax = request.AmountMax,
        MinimumCharge = request.MinimumCharge,
        CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant(),
        ChargeSide = request.ChargeSide!.Value,
    };

    private sealed record ValidationContext(
        IReadOnlyList<Port> Ports,
        IReadOnlyList<string> CurrencyCodes,
        List<LocalCharge> ExistingCharges);
}
