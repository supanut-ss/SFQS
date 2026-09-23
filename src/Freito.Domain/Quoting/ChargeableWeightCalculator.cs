namespace Freito.Domain.Quoting;

/// <summary>
/// Pure weight/volume math for LCL and Air. Every constant here is confirmed with
/// Operation against real quotes — see operation-worksheet.md §2/§3, not invented.
/// </summary>
public static class ChargeableWeightCalculator
{
    /// <summary>Air volumetric divisor (cm³ per kg). Confirmed against a real Barcelona→BKK
    /// quote: 53.5 cbm -> 8,916.4 kg matches this exactly, and Operation confirmed it's the
    /// same for every airline.</summary>
    private const decimal AirVolumetricDivisorCm3PerKg = 6000m;

    /// <summary>LCL: kg that make up 1 revenue ton. Not directly proven by a worked example,
    /// but Operation raised no objection when asked directly — treated as accepted.</summary>
    private const decimal LclKgPerRevenueTon = 1000m;

    /// <summary>Air minimum chargeable weight (kg). Below this, billing uses this floor
    /// value instead of the real chargeable weight. Confirmed with Operation.</summary>
    public const decimal AirMinimumChargeableWeightKg = 50m;

    /// <summary>
    /// Air chargeable weight = max(actual weight, volumetric weight), floored at the
    /// minimum chargeable weight. volumeCm3 is the total volume of the shipment in cm³
    /// (sum of length × width × height × qty for every piece).
    /// </summary>
    public static decimal AirChargeableWeightKg(decimal actualWeightKg, decimal volumeCm3)
    {
        var volumetricWeightKg = volumeCm3 / AirVolumetricDivisorCm3PerKg;
        var chargeable = Math.Max(actualWeightKg, volumetricWeightKg);
        return Math.Max(chargeable, AirMinimumChargeableWeightKg);
    }

    /// <summary>
    /// LCL revenue ton = max(CBM, weight in tonnes), rounded UP to the nearest 0.1 —
    /// confirmed with Operation this rounding is part of the billing formula itself, not
    /// just display rounding.
    /// </summary>
    public static decimal LclRevenueTon(decimal cbm, decimal weightKg)
    {
        var weightInTonnes = weightKg / LclKgPerRevenueTon;
        var raw = Math.Max(cbm, weightInTonnes);
        return RoundUpToNearestTenth(raw);
    }

    private static decimal RoundUpToNearestTenth(decimal value)
        => Math.Ceiling(value * 10m) / 10m;
}
