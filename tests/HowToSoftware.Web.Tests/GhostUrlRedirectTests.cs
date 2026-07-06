using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;

namespace HowToSoftware.Web.Tests;

public class GhostUrlRedirectTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public GhostUrlRedirectTests()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(builder =>
            {
                builder.UseTestServer();
                builder.ConfigureServices(services =>
                {
                    services.AddRouting();
                });
                builder.Configure(app =>
                {
                    app.UseMiddleware<GhostUrlRedirectMiddleware>();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/", () => "home");
                        endpoints.MapGet("/api/content/posts/", () => "posts");
                        endpoints.MapGet("/sitemap.xml", () => "sitemap");
                        endpoints.MapGet("/rss/", () => "rss");
                        endpoints.MapGet("/welcome/", () => "post");
                    });
                });
            })
            .Start();

        _client = _host.GetTestServer().CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    // ── Ghost versioned Content API redirects ───────────────────

    [Theory]
    [InlineData("/ghost/api/v3/content/posts/", "/api/content/posts/")]
    [InlineData("/ghost/api/v4/content/posts/", "/api/content/posts/")]
    [InlineData("/ghost/api/v5/content/posts/", "/api/content/posts/")]
    [InlineData("/ghost/api/v3/content/tags/", "/api/content/tags/")]
    [InlineData("/ghost/api/v4/content/authors/", "/api/content/authors/")]
    public async Task GhostVersionedContentApi_Redirects_ToNewApi(string ghostPath, string expectedTarget)
    {
        var response = await _client.GetAsync(ghostPath);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(expectedTarget, response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GhostVersionedContentApi_PreservesQueryString()
    {
        var response = await _client.GetAsync("/ghost/api/v4/content/posts/?key=abc123&include=tags");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/api/content/posts/?key=abc123&include=tags", response.Headers.Location?.ToString());
    }

    // ── Ghost unversioned Content API redirects ─────────────────

    [Fact]
    public async Task GhostUnversionedContentApi_Redirects_ToNewApi()
    {
        var response = await _client.GetAsync("/ghost/api/content/posts/");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/api/content/posts/", response.Headers.Location?.ToString());
    }

    // ── Ghost sitemap variant redirects ─────────────────────────

    [Theory]
    [InlineData("/ghost/sitemap.xml")]
    [InlineData("/sitemap-posts.xml")]
    [InlineData("/sitemap-pages.xml")]
    [InlineData("/sitemap-tags.xml")]
    [InlineData("/sitemap-authors.xml")]
    public async Task GhostSitemapVariants_Redirect_ToSitemap(string ghostPath)
    {
        var response = await _client.GetAsync(ghostPath);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/sitemap.xml", response.Headers.Location?.ToString());
    }

    // ── Non-Ghost URLs are not redirected ───────────────────────

    [Theory]
    [InlineData("/")]
    [InlineData("/welcome/")]
    [InlineData("/rss/")]
    [InlineData("/sitemap.xml")]
    [InlineData("/api/content/posts/")]
    public async Task NonGhostUrls_AreNotRedirected(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── POST requests are not redirected ────────────────────────

    [Fact]
    public async Task PostMethod_IsNotRedirected()
    {
        var response = await _client.PostAsync("/ghost/api/v4/content/posts/", null);

        // POST should pass through, not redirect
        Assert.NotEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
    }

    // ── HEAD requests are redirected ────────────────────────────

    [Fact]
    public async Task HeadMethod_IsRedirected()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, "/ghost/api/v4/content/posts/");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/api/content/posts/", response.Headers.Location?.ToString());
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
