using System.Net;

namespace HowToSoftware.Core.Interfaces;

public interface IGeoIpService
{
    /// <summary>
    /// Returns the ISO 3166-1 alpha-2 country code for the given IP address,
    /// or null if the lookup fails or the database is unavailable.
    /// </summary>
    string? LookupCountry(IPAddress ipAddress);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
