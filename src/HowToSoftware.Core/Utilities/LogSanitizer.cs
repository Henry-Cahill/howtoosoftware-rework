using System.Security.Cryptography;
using System.Text;

namespace HowToSoftware.Core.Utilities;

/// <summary>
/// Helpers for making user-influenced values safe to write to application logs.
///
/// A value that flows from untrusted input (query strings, webhook payloads,
/// trigger slugs, form fields, …) into a log entry can be used to forge or
/// split log records by embedding carriage-return / line-feed characters
/// (CWE-117, CodeQL <c>cs/log-forging</c>). Route such values through
/// <see cref="SanitizeForLog"/> before passing them to the logger.
///
/// Personal data such as an email address must never be written to a log in
/// clear text (CWE-359, CodeQL <c>cs/exposure-of-sensitive-information</c>).
/// Use <see cref="MaskEmail"/> to emit a stable, non-reversible token that can
/// still be correlated across entries.
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Removes carriage-return and line-feed characters from
    /// <paramref name="value"/> so attacker-controlled text cannot inject
    /// additional log lines. Returns the input unchanged when it is
    /// <see langword="null"/> or empty.
    /// </summary>
    public static string? SanitizeForLog(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }

    /// <summary>
    /// Produces a stable, non-reversible token for an email address (or any
    /// other personal identifier) so occurrences can be correlated across log
    /// entries without ever writing the raw value to the log
    /// (CWE-359, CodeQL <c>cs/exposure-of-sensitive-information</c>). The same
    /// input always maps to the same token; the token cannot be reversed back
    /// to the original address. Being a fixed hex token, it is also free of
    /// carriage-return / line-feed characters and therefore safe against log
    /// forging. Returns <c>"(none)"</c> when the input is
    /// <see langword="null"/> or empty.
    /// </summary>
    public static string MaskEmail(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "(none)";

        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim().ToLowerInvariant()), hash);
        return "sha256:" + Convert.ToHexString(hash[..6]).ToLowerInvariant();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
