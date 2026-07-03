using Microsoft.AspNetCore.Identity;
using TraceMonitor.Core.Models;

namespace TraceMonitor.Core.Security;

/// <summary>Wraps ASP.NET Core's PBKDF2-based PasswordHasher — unlike ApiKeyUtil's unsalted
/// SHA-256 (fine for high-entropy random API keys), user passwords need a salted, slow hash.</summary>
public static class UserPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    public static string Hash(User user, string password) => Hasher.HashPassword(user, password);

    public static bool Verify(User user, string hash, string password) =>
        Hasher.VerifyHashedPassword(user, hash, password) != PasswordVerificationResult.Failed;
}
