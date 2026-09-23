using System.ComponentModel.DataAnnotations;
using Freito.Domain.Enums;

namespace Freito.Api.Models;

public sealed class FreightRateRequest
{
    [Range(1, int.MaxValue)]
    public int OriginPortId { get; init; }

    [Range(1, int.MaxValue)]
    public int DestinationPortId { get; init; }

    [Required, EnumDataType(typeof(TransportMode))]
    public TransportMode? Mode { get; init; }

    [Required, EnumDataType(typeof(ShipmentDirection))]
    public ShipmentDirection? Direction { get; init; }

    [Range(1, int.MaxValue)]
    public int CarrierId { get; init; }

    [StringLength(16)]
    public string? ContainerSize { get; init; }

    [Range(typeof(decimal), "0", "999999999.999")]
    public decimal? WeightBreakMin { get; init; }

    [Range(typeof(decimal), "0", "999999999.999")]
    public decimal? WeightBreakMax { get; init; }

    [Range(typeof(decimal), "0", "99999999999999.9999")]
    public decimal PriceMin { get; init; }

    [Range(typeof(decimal), "0", "99999999999999.9999")]
    public decimal PriceMax { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string CurrencyCode { get; init; } = string.Empty;

    [Required]
    public DateTime? ValidFrom { get; init; }

    [Required]
    public DateTime? ValidTo { get; init; }
}

public sealed class LocalChargeRequest
{
    [Range(1, int.MaxValue)]
    public int PortId { get; init; }

    [Required, EnumDataType(typeof(ShipmentDirection))]
    public ShipmentDirection? Direction { get; init; }

    [Required, EnumDataType(typeof(TransportMode))]
    public TransportMode? Mode { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string ChargeType { get; init; } = string.Empty;

    [Required, EnumDataType(typeof(ChargeCalcBasis))]
    public ChargeCalcBasis? CalcBasis { get; init; }

    [Range(typeof(decimal), "0", "99999999999999.9999")]
    public decimal AmountMin { get; init; }

    [Range(typeof(decimal), "0", "99999999999999.9999")]
    public decimal AmountMax { get; init; }

    [Range(typeof(decimal), "0", "99999999999999.9999")]
    public decimal? MinimumCharge { get; init; }

    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string CurrencyCode { get; init; } = string.Empty;

    [Required, EnumDataType(typeof(ChargeSide))]
    public ChargeSide? ChargeSide { get; init; }
}

public sealed record CsvRowIssue(int Row, IReadOnlyList<string> Errors);

public sealed record CsvImportResult(int Imported, IReadOnlyList<CsvRowIssue> Errors);
