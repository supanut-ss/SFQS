using System.Security.Cryptography;

namespace Freito.Api.Auth;

/// <summary>
/// PBKDF2-HMACSHA256 password hashing — no external Identity package needed for just 3 internal
/// roles with no self-signup (technical-plan.md §2 decision: simple table over ASP.NET Core
/// Identity). Encoded as "{iterations}.{saltBase64}.{keyBase64}" so the iteration count can be
/// bumped later without invalidating existing hashes.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int KeySizeBytes = 32;
    private const int DefaultIterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public static string Hash(string password) => HashWithSalt(password, RandomNumberGenerator.GetBytes(SaltSizeBytes), DefaultIterations);

    /// <summary>Exposed only so a deterministic hash can be computed once for the seeded bootstrap
    /// Admin account in a migration — never use a fixed salt for a user-chosen password.</summary>
    public static string HashWithSalt(string password, byte[] salt, int iterations)
    {
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, KeySizeBytes);
        return $"{iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string encodedHash)
    {
        var parts = encodedHash.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations)) return false;

        byte[] salt, expectedKey;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedKey = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedKey.Length);
        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
