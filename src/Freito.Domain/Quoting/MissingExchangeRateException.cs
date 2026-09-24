namespace Freito.Domain.Quoting;

public sealed class MissingExchangeRateException(string currencyCode)
    : InvalidOperationException($"No exchange rate available for currency '{currencyCode}'.")
{
    public string CurrencyCode { get; } = currencyCode;
}
