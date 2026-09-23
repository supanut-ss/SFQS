using Freito.Domain.Entities;
using Freito.Domain.Enums;

namespace Freito.Domain.Quoting;

/// <summary>
/// Sums local charges for one side's customer. Confirmed model (operation-worksheet.md §1):
/// a quotation is issued to exactly one customer (the Buyer on Import, the Seller on
/// Export), and that customer pays 100% of every charge scoped to them by
/// IncotermChargeRule — never a split within one document.
/// </summary>
public static class LocalChargeCalculator
{
    public static (IReadOnlyList<LocalChargeLineResult> Lines, decimal Total) Calculate(
        IReadOnlyList<LocalCharge> candidates,
        ShipmentDirection direction,
        string incotermCode,
        IReadOnlyList<IncotermChargeRule> incotermRules,
        int containerQty,
        decimal? cbm,
        decimal? revenueTon,
        decimal? chargeableWeightKg)
    {
        var customerPayer = direction == ShipmentDirection.Import ? Payer.Buyer : Payer.Seller;

        var rulesByCharge = incotermRules
            .Where(r => r.IncotermCode.Equals(incotermCode, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(r => r.ChargeSide, r => r.Payer);

        var lines = new List<LocalChargeLineResult>();
        decimal total = 0;

        foreach (var charge in candidates)
        {
            if (!rulesByCharge.TryGetValue(charge.ChargeSide, out var payer) || payer != customerPayer)
            {
                continue; // out of scope for this customer under this Incoterm
            }

            if (charge.CalcBasis == ChargeCalcBasis.NotQuotable)
            {
                lines.Add(new LocalChargeLineResult
                {
                    ChargeType = charge.ChargeType,
                    Basis = "NotQuotable",
                    Amount = 0,
                    Currency = charge.CurrencyCode,
                    IsNotQuotable = true,
                });
                continue;
            }

            var amount = CalculateAmount(charge, containerQty, cbm, revenueTon, chargeableWeightKg);
            lines.Add(new LocalChargeLineResult
            {
                ChargeType = charge.ChargeType,
                Basis = charge.CalcBasis.ToString(),
                Amount = amount,
                Currency = charge.CurrencyCode,
                IsNotQuotable = false,
            });
            total += amount;
        }

        return (lines, total);
    }

    /// <summary>
    /// ASSUMPTION (same as RateResolver): ranged charges use AmountMax to avoid
    /// under-quoting. MinimumCharge is a floor applied after the per-unit calculation.
    /// </summary>
    private static decimal CalculateAmount(
        LocalCharge charge, int containerQty, decimal? cbm, decimal? revenueTon, decimal? chargeableWeightKg)
    {
        var unitPrice = charge.AmountMax;

        var raw = charge.CalcBasis switch
        {
            ChargeCalcBasis.PerShipment => unitPrice,
            ChargeCalcBasis.PerContainer => unitPrice * containerQty,
            ChargeCalcBasis.PerRevenueTon => unitPrice * (revenueTon
                ?? throw new InvalidOperationException("PerRevenueTon charge requires a revenue ton value (LCL only).")),
            ChargeCalcBasis.PerCBM => unitPrice * (cbm
                ?? throw new InvalidOperationException("PerCBM charge requires a CBM value (LCL only).")),
            ChargeCalcBasis.PerKG => unitPrice * (chargeableWeightKg
                ?? throw new InvalidOperationException("PerKG charge requires a chargeable weight value.")),
            _ => throw new InvalidOperationException($"Unhandled calc basis: {charge.CalcBasis}"),
        };

        return charge.MinimumCharge is { } minimum ? Math.Max(raw, minimum) : raw;
    }
}
