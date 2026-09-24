namespace Freito.Api.Auth;

/// <summary>Bound from configuration section "Jwt". Key must be overridden per environment
/// (appsettings.Development.json has a dev-only value) — never rely on the default in production.</summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Freito";
    public string Audience { get; set; } = "Freito";
    public int ExpiryMinutes { get; set; } = 480; // one working day
}
