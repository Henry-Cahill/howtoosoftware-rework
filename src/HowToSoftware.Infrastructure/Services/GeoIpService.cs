using System.Net;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Services;

public sealed class GeoIpService : IGeoIpService, IDisposable
{
    private readonly DatabaseReader? _reader;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(ILogger<GeoIpService> logger, GeoIpOptions options)
    {
        _logger = logger;

        if (File.Exists(options.DatabasePath))
        {
            try
            {
                _reader = new DatabaseReader(options.DatabasePath);
                _logger.LogInformation("GeoIP database loaded from {Path}", options.DatabasePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load GeoIP database from {Path}", options.DatabasePath);
            }
        }
        else
        {
            _logger.LogWarning("GeoIP database not found at {Path} — country enrichment disabled", options.DatabasePath);
        }
    }

    public string? LookupCountry(IPAddress ipAddress)
    {
        if (_reader is null)
            return null;

        // Skip private/loopback addresses
        if (IPAddress.IsLoopback(ipAddress) || ipAddress.IsPrivate())
            return null;

        try
        {
            if (_reader.TryCountry(ipAddress, out var response))
                return response?.Country?.IsoCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GeoIP lookup failed for {IP}", ipAddress);
        }

        return null;
    }

    public void Dispose() => _reader?.Dispose();
}

public class GeoIpOptions
{
    public string DatabasePath { get; set; } = "GeoLite2-Country.mmdb";
}

internal static class IPAddressExtensions
{
    public static bool IsPrivate(this IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        byte[] bytes = address.GetAddressBytes();
        return bytes switch
        {
            [10, ..] => true,                                          // 10.0.0.0/8
            [172, >= 16 and <= 31, ..] => true,                        // 172.16.0.0/12
            [192, 168, ..] => true,                                    // 192.168.0.0/16
            [169, 254, ..] => true,                                    // 169.254.0.0/16 (link-local)
            [127, ..] => true,                                         // 127.0.0.0/8
            _ => false
        };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
