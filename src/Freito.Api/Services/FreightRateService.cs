using System.Data;
using Freito.Domain.Entities;
using Freito.Domain.Enums;
using Freito.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Freito.Api.Services;

public sealed class FreightRateService(FreitoDbContext db)
{
    private const decimal MaxStoredMoney = 99_999_999_999_999.9999m;

    public async Task<IDbContextTransaction?> BeginWriteTransactionAsync(CancellationToken cancellationToken)
    {
        if (!db.Database.IsRelational() || db.Database.CurrentTransaction is not null) return null;
        return await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    public async Task<EntityValidationResult> ValidateAsync(FreightRate rate, int? excludedId, CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync([rate], cancellationToken);
        return ValidateAgainst(rate, excludedId, context);
    }

    public async Task<IReadOnlyList<EntityValidationResult>> ValidateBatchAsync(
        IReadOnlyList<FreightRate> rates,
        CancellationToken cancellationToken)
    {
        var context = await LoadContextAsync(rates, cancellationToken);
        var results = new List<EntityValidationResult>(rates.Count);
        foreach (var rate in rates)
        {
            var result = ValidateAgainst(rate, null, context);
            results.Add(result);
            if (result.IsValid) context.ActiveRates.Add(rate);
        }

        return results;
    }

    private async Task<ValidationContext> LoadContextAsync(IReadOnlyList<FreightRate> candidates, CancellationToken cancellationToken)
    {
        var ports = await db.Ports.AsNoTracking().ToListAsync(cancellationToken);
        var carriers = await db.Carriers.AsNoTracking().ToListAsync(cancellationToken);
        var currencies = await db.Currencies.AsNoTracking().Select(x => x.Code).ToListAsync(cancellationToken);
        // Lists, not arrays: EF's client-eval interpreter (ExpressionTreeFuncletizer) chokes on
        // array.Contains(x) inside a query under .NET 10/EF Core 9 — Enumerable.Contains has a
        // ReadOnlySpan<T> fast path for arrays that the interpreter can't reflectively invoke
        // (TypeLoadException: ReadOnlySpan violates a generic constraint). List<T>.Contains
        // doesn't hit that path and translates to SQL IN(...) exactly the same.
        var routeKeys = candidates.Select(x => new RateRouteKey(x.OriginPortId, x.DestinationPortId, x.Mode, x.Direction, x.CarrierId))
            .Distinct().ToList();
        var originIds = routeKeys.Select(x => x.OriginPortId).Distinct().ToList();
        var destinationIds = routeKeys.Select(x => x.DestinationPortId).Distinct().ToList();
        var modes = routeKeys.Select(x => x.Mode).Distinct().ToList();
        var directions = routeKeys.Select(x => x.Direction).Distinct().ToList();
        var carrierIds = routeKeys.Select(x => x.CarrierId).Distinct().ToList();
        var activeRates = await db.FreightRates.AsNoTracking()
            .Where(x => x.IsActive && originIds.Contains(x.OriginPortId) && destinationIds.Contains(x.DestinationPortId) &&
                modes.Contains(x.Mode) && directions.Contains(x.Direction) && carrierIds.Contains(x.CarrierId))
            .ToListAsync(cancellationToken);
        return new ValidationContext(ports, carriers, currencies, activeRates);
    }

    private static EntityValidationResult ValidateAgainst(FreightRate rate, int? excludedId, ValidationContext context)
    {
        var errors = new List<string>();
        if (!Enum.IsDefined(rate.Mode)) errors.Add("Mode is not supported.");
        if (!Enum.IsDefined(rate.Direction)) errors.Add("Direction is not supported.");
        if (rate.OriginPortId <= 0 || rate.DestinationPortId <= 0 || rate.OriginPortId == rate.DestinationPortId)
            errors.Add("Origin and destination must be different existing ports.");
        if (rate.CarrierId <= 0) errors.Add("A carrier is required.");
        if (rate.PriceMin < 0 || rate.PriceMax < rate.PriceMin) errors.Add("Price range must be non-negative and PriceMax must be at least PriceMin.");
        if (rate.PriceMin > MaxStoredMoney || rate.PriceMax > MaxStoredMoney) errors.Add("Prices exceed the supported maximum amount.");
        if (decimal.Round(rate.PriceMin, 4) != rate.PriceMin || decimal.Round(rate.PriceMax, 4) != rate.PriceMax)
            errors.Add("Prices support at most four decimal places.");
        if ((rate.WeightBreakMin is not null && decimal.Round(rate.WeightBreakMin.Value, 3) != rate.WeightBreakMin.Value) ||
            (rate.WeightBreakMax is not null && decimal.Round(rate.WeightBreakMax.Value, 3) != rate.WeightBreakMax.Value))
            errors.Add("Weight breaks support at most three decimal places.");
        if (string.IsNullOrWhiteSpace(rate.CurrencyCode)) errors.Add("A currency is required.");
        else if (rate.CurrencyCode.Length > 3) errors.Add("Currency codes cannot exceed three characters.");
        if (rate.ContainerSize?.Length > 16) errors.Add("Container size cannot exceed 16 characters.");
        if (rate.ValidFrom == default || rate.ValidTo == default || rate.ValidFrom.Date > rate.ValidTo.Date)
            errors.Add("Validity dates must be present and ValidTo cannot be before ValidFrom.");

        var origin = context.Ports.FirstOrDefault(x => x.Id == rate.OriginPortId);
        var destination = context.Ports.FirstOrDefault(x => x.Id == rate.DestinationPortId);
        var carrier = context.Carriers.FirstOrDefault(x => x.Id == rate.CarrierId);
        if (origin is null) errors.Add("Origin port does not exist.");
        if (destination is null) errors.Add("Destination port does not exist.");
        if (carrier is null) errors.Add("Carrier does not exist.");
        if (!context.CurrencyCodes.Contains(rate.CurrencyCode, StringComparer.OrdinalIgnoreCase)) errors.Add("Currency does not exist.");

        if (origin is not null && destination is not null && Enum.IsDefined(rate.Mode))
        {
            var expectedType = rate.Mode == TransportMode.Air ? PortType.Air : PortType.Sea;
            if (origin.Type != expectedType || destination.Type != expectedType)
                errors.Add($"{rate.Mode} rates require both ports to be {expectedType} ports.");
        }

        if (carrier is not null && Enum.IsDefined(rate.Mode))
        {
            var expectedType = rate.Mode == TransportMode.Air ? CarrierType.Airline : CarrierType.ShippingLine;
            if (carrier.Type != expectedType) errors.Add($"{rate.Mode} rates require a {expectedType} carrier.");
        }

        switch (rate.Mode)
        {
            case TransportMode.Fcl:
                if (string.IsNullOrWhiteSpace(rate.ContainerSize)) errors.Add("FCL rates require a container size.");
                if (rate.WeightBreakMin is not null || rate.WeightBreakMax is not null) errors.Add("FCL rates cannot have weight breaks.");
                break;
            case TransportMode.Lcl:
                if (!string.IsNullOrWhiteSpace(rate.ContainerSize) || rate.WeightBreakMin is not null || rate.WeightBreakMax is not null)
                    errors.Add("LCL rates cannot have a container size or weight breaks.");
                break;
            case TransportMode.Air:
                if (!string.IsNullOrWhiteSpace(rate.ContainerSize)) errors.Add("Air rates cannot have a container size.");
                if (rate.WeightBreakMin is null || rate.WeightBreakMax is null || rate.WeightBreakMin < 0 || rate.WeightBreakMin >= rate.WeightBreakMax)
                    errors.Add("Air rates require a valid weight range.");
                else if (rate.WeightBreakMax > 500)
                    errors.Add("Air weight breaks cannot exceed 500 kg; higher weights are entered manually by Sale.");
                break;
            default:
                break;
        }

        if (errors.Count > 0) return new EntityValidationResult(errors);

        var collision = context.ActiveRates.FirstOrDefault(other =>
            other.Id != excludedId &&
            other.OriginPortId == rate.OriginPortId &&
            other.DestinationPortId == rate.DestinationPortId &&
            other.Mode == rate.Mode &&
            other.Direction == rate.Direction &&
            other.CarrierId == rate.CarrierId &&
            SameRateSlot(other, rate) &&
            rate.ValidFrom.Date <= other.ValidTo.Date && other.ValidFrom.Date <= rate.ValidTo.Date);
        return collision is null
            ? new EntityValidationResult(Array.Empty<string>())
            : new EntityValidationResult(["An active rate for this route, carrier, and validity window already exists."], IsConflict: true);
    }

    private static bool SameRateSlot(FreightRate left, FreightRate right)
    {
        if (left.Mode == TransportMode.Air)
        {
            // Adjacent brackets may share an endpoint because weight breaks are half-open, except at 500 kg.
            return left.WeightBreakMin < right.WeightBreakMax && right.WeightBreakMin < left.WeightBreakMax;
        }

        if (left.Mode == TransportMode.Fcl)
        {
            return string.Equals(left.ContainerSize, right.ContainerSize, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private sealed record ValidationContext(
        IReadOnlyList<Port> Ports,
        IReadOnlyList<Carrier> Carriers,
        IReadOnlyList<string> CurrencyCodes,
        List<FreightRate> ActiveRates);

    private readonly record struct RateRouteKey(
        int OriginPortId,
        int DestinationPortId,
        TransportMode Mode,
        ShipmentDirection Direction,
        int CarrierId);
}

public sealed record FreightRateRequestData(
    int OriginPortId,
    int DestinationPortId,
    TransportMode Mode,
    ShipmentDirection Direction,
    int CarrierId,
    string? ContainerSize,
    decimal? WeightBreakMin,
    decimal? WeightBreakMax,
    decimal PriceMin,
    decimal PriceMax,
    string CurrencyCode,
    DateTime ValidFrom,
    DateTime ValidTo);
