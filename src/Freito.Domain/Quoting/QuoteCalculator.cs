using Freito.Domain.Entities;
using Freito.Domain.Enums;

namespace Freito.Domain.Quoting;

/// <summary>
/// Orchestrates one mode's calculation: resolve a freight rate, apply the mode's formula,
/// sum local charges scoped to the requesting customer. Pure — takes already-fetched
/// candidate data, does no I/O, so it's directly unit-testable against the real Operation
/// quotes in operation-worksheet.md §4. See technical-plan.md §3 for the formulas.
/// </summary>
public static class QuoteCalculator
{
    public static QuoteCalculationResult Calculate(
        QuoteRequest request,
        IReadOnlyList<FreightRate> freightRateCandidates,
        IReadOnlyList<LocalCharge> localChargeCandidates,
        IReadOnlyList<IncotermChargeRule> incotermRules,
        IReadOnlyDictionary<string, decimal> exchangeRatesToBase)
    {
        decimal? chargeableWeightKg = null;
        decimal? revenueTon = null;

        if (request.Mode == TransportMode.Lcl)
        {
            revenueTon = ChargeableWeightCalculator.LclRevenueTon(
                request.Cbm ?? throw new ArgumentException("LCL requires Cbm.", nameof(request)),
                request.WeightKg ?? throw new ArgumentException("LCL requires WeightKg.", nameof(request)));
        }
        else if (request.Mode == TransportMode.Air)
        {
            chargeableWeightKg = ChargeableWeightCalculator.AirChargeableWeightKg(
                request.ActualWeightKg ?? throw new ArgumentException("Air requires ActualWeightKg.", nameof(request)),
                request.VolumeCm3 ?? throw new ArgumentException("Air requires VolumeCm3.", nameof(request)));
        }

        var resolution = RateResolver.Resolve(
            freightRateCandidates,
            request.ReadyDate,
            request.Mode == TransportMode.Fcl ? request.ContainerSize : null,
            chargeableWeightKg,
            exchangeRatesToBase);

        var (localLines, localTotal) = LocalChargeCalculator.Calculate(
            localChargeCandidates,
            request.Direction,
            request.IncotermCode,
            incotermRules,
            request.ContainerQty,
            request.Cbm,
            revenueTon,
            chargeableWeightKg);

        var notQuotableNotes = localLines
            .Where(l => l.IsNotQuotable)
            .Select(l => $"{l.ChargeType}: อาจมีค่าใช้จ่ายเพิ่มเติมตามจริง แจ้งราคาสุดท้ายอีกครั้งหลังยืนยันออเดอร์")
            .ToList();

        if (!resolution.Found)
        {
            return new QuoteCalculationResult
            {
                RateFound = false,
                RateSource = RateSource.Manual,
                FreightCost = 0,
                LocalChargeTotal = localTotal,
                Subtotal = localTotal,
                ChargeableWeightKg = chargeableWeightKg,
                RevenueTon = revenueTon,
                LocalChargeLines = localLines,
                NotQuotableNotes = notQuotableNotes,
                NoRateFoundReason = "ไม่พบเรทที่ตรงในระบบ — ให้ Sale กรอกราคาค่าระวางเอง (rate_source = Manual)",
            };
        }

        var quantity = request.Mode switch
        {
            TransportMode.Fcl => request.ContainerQty,
            TransportMode.Lcl => revenueTon!.Value,
            TransportMode.Air => chargeableWeightKg!.Value,
            _ => throw new InvalidOperationException($"Unhandled mode: {request.Mode}"),
        };

        var freightCost = resolution.Rate!.PriceMax * quantity;

        return new QuoteCalculationResult
        {
            RateFound = true,
            RateSource = resolution.Source,
            FreightCost = freightCost,
            LocalChargeTotal = localTotal,
            Subtotal = freightCost + localTotal,
            ChargeableWeightKg = chargeableWeightKg,
            RevenueTon = revenueTon,
            LocalChargeLines = localLines,
            NotQuotableNotes = notQuotableNotes,
        };
    }
}
