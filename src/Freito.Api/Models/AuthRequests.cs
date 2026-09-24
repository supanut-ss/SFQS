using System.ComponentModel.DataAnnotations;

namespace Freito.Api.Models;

public sealed class LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Password { get; init; } = string.Empty;
}
