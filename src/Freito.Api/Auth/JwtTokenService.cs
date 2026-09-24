using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Freito.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Freito.Api.Auth;

/// <summary>Issues the JWT set as an httpOnly cookie on login (technical-plan.md §1: "JWT
/// cookie-based auth + role claim"). ClaimTypes.NameIdentifier/Role are what
/// ProtectedControllerBase reads, so this is the one place that must stay in sync with it.</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public TimeSpan Expiry => TimeSpan.FromMinutes(options.Value.ExpiryMinutes);

    public string IssueToken(User user)
    {
        var opts = options.Value;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: opts.Issuer,
            audience: opts.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(Expiry),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
