using Microsoft.AspNetCore.Identity;
using HowToSoftware.Core.Entities;

namespace HowToSoftware.Infrastructure.Services;

/// <summary>
/// Custom password hasher that supports Ghost CMS bcrypt hashes alongside ASP.NET Identity PBKDF2.
/// On first successful bcrypt login, rehashes the password using Identity's format and clears the legacy hash.
/// </summary>
public sealed class GhostBcryptPasswordHasher : IPasswordHasher<User>
{
    private readonly PasswordHasher<User> _identity = new();

    public string HashPassword(User user, string password)
        => _identity.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        // 1. If there's a standard Identity hash, try it first
        if (!string.IsNullOrEmpty(hashedPassword))
        {
            try
            {
                var result = _identity.VerifyHashedPassword(user, hashedPassword, providedPassword);
                if (result != PasswordVerificationResult.Failed)
                    return result;
            }
            catch (FormatException)
            {
                // hashedPassword is not a valid Identity PBKDF2 hash (e.g. placeholder) — fall through to bcrypt
            }
        }

        // 2. Fall back to Ghost's bcrypt hash in GhostPassword
        if (!string.IsNullOrEmpty(user.GhostPassword) && user.GhostPassword.StartsWith("$2"))
        {
            if (BCrypt.Net.BCrypt.Verify(providedPassword, user.GhostPassword))
            {
                // Clear the legacy bcrypt hash — it will be replaced by Identity hash via SuccessRehashNeeded
                // Use empty string instead of null because the DB column has a NOT NULL constraint
                user.GhostPassword = "";
                return PasswordVerificationResult.SuccessRehashNeeded;
            }
        }

        return PasswordVerificationResult.Failed;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
