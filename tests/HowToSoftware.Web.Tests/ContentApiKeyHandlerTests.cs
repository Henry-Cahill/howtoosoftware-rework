using System.Net;
using System.Net.Http.Headers;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HowToSoftware.Web.Tests;

public class ContentApiKeyHandlerTests : IAsyncDisposable
{
    private const string ValidKey = "test-content-api-key-secret";

    private readonly FakeApiKeyRepository _apiKeyRepo = new();
    private readonly IHost _host;
    private readonly HttpClient _client;

    public ContentApiKeyHandlerTests()
    {
        _apiKeyRepo.Keys.Add(new ApiKey
        {
            Id = "key-1",
            Type = "content",
            Secret = ValidKey,
            IntegrationId = "integration-1",
            CreatedAt = DateTime.UtcNow
        });

        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddSingleton<IApiKeyRepository>(_apiKeyRepo);
                    services.AddAuthentication(ContentApiDefaults.AuthenticationScheme)
                        .AddScheme<ContentApiKeyOptions, ContentApiKeyHandler>(
                            ContentApiDefaults.AuthenticationScheme, _ => { });
                    services.AddAuthorizationBuilder()
                        .AddPolicy("ContentApi", policy =>
                            policy.AddAuthenticationSchemes(ContentApiDefaults.AuthenticationScheme)
                                  .RequireAuthenticatedUser());
                });
                webBuilder.Configure(app =>
                {
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.Map("/api/content/test", appBuilder =>
                        appBuilder.Run(async context =>
                        {
                            var result = await context.AuthenticateAsync(ContentApiDefaults.AuthenticationScheme);
                            if (!result.Succeeded)
                            {
                                context.Response.StatusCode = 401;
                                return;
                            }
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("OK");
                        }));
                });
            })
            .Start();

        _client = _host.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    // ── Bearer header authentication ────────────────────────────

    [Fact]
    public async Task BearerHeader_ValidKey_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ValidKey);

        var response = await _client.GetAsync("/api/content/test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Deprecation"));
    }

    [Fact]
    public async Task BearerHeader_InvalidKey_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "wrong-key");

        var response = await _client.GetAsync("/api/content/test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BearerHeader_EmptyValue_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "   ");

        var response = await _client.GetAsync("/api/content/test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Query string authentication (deprecated fallback) ───────

    [Fact]
    public async Task QueryString_ValidKey_Returns200WithDeprecationHeaders()
    {
        var response = await _client.GetAsync($"/api/content/test?key={ValidKey}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("true", response.Headers.GetValues("Deprecation").Single());
        Assert.True(response.Headers.Contains("Sunset"));
        Assert.True(response.Headers.Contains("Link"));
    }

    [Fact]
    public async Task QueryString_InvalidKey_Returns401()
    {
        var response = await _client.GetAsync("/api/content/test?key=wrong-key");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task QueryString_EmptyKey_Returns401()
    {
        var response = await _client.GetAsync("/api/content/test?key=");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── No credentials ──────────────────────────────────────────

    [Fact]
    public async Task NoCredentials_Returns401()
    {
        var response = await _client.GetAsync("/api/content/test");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── Bearer takes precedence over query string ───────────────

    [Fact]
    public async Task BearerHeader_TakesPrecedence_OverQueryString()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ValidKey);

        var response = await _client.GetAsync($"/api/content/test?key={ValidKey}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Bearer was used, so no deprecation headers
        Assert.False(response.Headers.Contains("Deprecation"));
    }

    // ── Fake repository ─────────────────────────────────────────

    private class FakeApiKeyRepository : IApiKeyRepository
    {
        public List<ApiKey> Keys { get; } = [];

        public Task<ApiKey?> GetBySecretAsync(string secret, CancellationToken ct = default)
            => Task.FromResult(Keys.FirstOrDefault(k => k.Secret == secret));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
