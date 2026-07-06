using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Authentication;

public static class ContentApiDefaults
{
    public const string AuthenticationScheme = "ContentApiKey";
}

public class ContentApiKeyOptions : AuthenticationSchemeOptions { }

public class ContentApiKeyHandler(
    IOptionsMonitor<ContentApiKeyOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository apiKeyRepository)
    : AuthenticationHandler<ContentApiKeyOptions>(options, logger, encoder)
{
    private static readonly DateTimeOffset QueryStringKeySunset = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? key = ExtractKeyFromBearerHeader();
        bool isDeprecatedQueryString = false;

        if (key is null)
        {
            key = ExtractKeyFromQueryString();
            isDeprecatedQueryString = key is not null;
        }

        if (key is null)
            return AuthenticateResult.NoResult();

        if (string.IsNullOrWhiteSpace(key))
            return AuthenticateResult.Fail("API key is empty.");

        var apiKey = await apiKeyRepository.GetBySecretAsync(key, Context.RequestAborted);
        if (apiKey is null)
            return AuthenticateResult.Fail("Invalid API key.");

        if (isDeprecatedQueryString)
        {
            Logger.LogWarning("Content API key passed via query string (integration: {IntegrationId}). " +
                "Migrate to Authorization: Bearer header before {Sunset:yyyy-MM-dd}.",
                apiKey.IntegrationId ?? apiKey.Id, QueryStringKeySunset);

            Response.Headers["Deprecation"] = "true";
            Response.Headers["Sunset"] = QueryStringKeySunset.ToString("R");
            Response.Headers.Append("Link", "</docs/content-api/#authentication>; rel=\"successor-version\"");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, apiKey.IntegrationId ?? apiKey.Id),
            new Claim("api_key_id", apiKey.Id),
            new Claim("api_key_type", apiKey.Type),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Update last-seen timestamp
        apiKey.LastSeenAt = DateTime.UtcNow;

        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractKeyFromBearerHeader()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return authorization["Bearer ".Length..].Trim();

        return null;
    }

    private string? ExtractKeyFromQueryString()
    {
        if (Request.Query.TryGetValue("key", out var keyValues))
            return keyValues.ToString();

        return null;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
