using System.ComponentModel.DataAnnotations;
using Freito.Domain.Enums;

namespace Freito.Api.Models;

public sealed class PortRequest
{
    [Required, StringLength(8, MinimumLength = 3)]
    public string Code { get; init; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string City { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Country { get; init; } = string.Empty;

    [Required, EnumDataType(typeof(PortType))]
    public PortType? Type { get; init; }
}

public sealed class CarrierRequest
{
    [Required, StringLength(16, MinimumLength = 2)]
    public string Code { get; init; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Required, EnumDataType(typeof(CarrierType))]
    public CarrierType? Type { get; init; }
}

public sealed class CurrencyRequest
{
    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string Code { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    [Range(0, 4)]
    public int DecimalDigits { get; init; }
}

public sealed class ExchangeRateRequest
{
    [Required, RegularExpression("^[A-Za-z]{3}$")]
    public string CurrencyCode { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.00000001", "9999999999.99999999")]
    public decimal RateToBase { get; init; }

    public DateTime EffectiveDate { get; init; }
}

public sealed class CargoTypeRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;

    public bool IsDangerous { get; init; }
    public bool IsProhibited { get; init; }
}
