using Freito.Domain.Enums;

namespace Freito.Domain.Entities;

/// <summary>
/// An internal user (Sale/Operation/Admin). No self-signup — created by Admin only.
/// Customers never get an account; they use the guest quote form. See technical-plan.md
/// §2 — identity provider (ASP.NET Core Identity vs. this simple table) still to be
/// decided before T8.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
