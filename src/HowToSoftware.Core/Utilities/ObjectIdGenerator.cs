using System.Security.Cryptography;
using System.Threading;

namespace HowToSoftware.Core.Utilities;

/// <summary>
/// Generates Ghost-compatible 24-char hexadecimal entity IDs.
///
/// Most tables in this database originated from a Ghost CMS migration and
/// keep Ghost's <c>NVARCHAR(24)</c> id convention (Ghost itself derives
/// these from a BSON ObjectId-style 12-byte value rendered as 24 hex
/// chars). Generating GUID-shaped 36-char strings here would overflow
/// those columns and SQL Server would reject the insert with error 2628
/// ("String or binary data would be truncated").
///
/// The 24-char ids are still strongly random (96 bits of entropy from
/// <see cref="RandomNumberGenerator"/>), with a monotonic counter on the
/// last 3 bytes to keep them roughly sortable within a process — which
/// matches Ghost's ObjectId structure closely enough for our needs.
///
/// Use this for the primary <c>Id</c> column of any entity. The
/// separate <c>Uuid</c>/<c>TransientId</c> columns are <c>NVARCHAR(36)</c>
/// and should keep using <c>Guid.NewGuid().ToString("D")</c>.
/// </summary>
public static class ObjectIdGenerator
{
    private static int _counter = RandomNumberGenerator.GetInt32(0, 0x00FFFFFF);

    /// <summary>Returns a new 24-char lowercase hex id.</summary>
    public static string New()
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes[..9]);

        var counter = Interlocked.Increment(ref _counter) & 0x00FFFFFF;
        bytes[9]  = (byte)(counter >> 16);
        bytes[10] = (byte)(counter >> 8);
        bytes[11] = (byte)counter;

        return Convert.ToHexStringLower(bytes);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
