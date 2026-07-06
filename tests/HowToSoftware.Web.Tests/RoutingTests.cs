using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HowToSoftware.Core.Interfaces;
using System.Net;

namespace HowToSoftware.Web.Tests;

/// <summary>
/// Tests the trailing-slash redirect middleware and routing behavior
/// using an in-memory test server.
/// </summary>
public class RoutingTests : IDisposable
{
    private readonly IHost _host;
    private readonly HttpClient _client;

    public RoutingTests()
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
                    // Replicate the trailing-slash redirect middleware from Program.cs
                    app.Use(async (context, next) =>
                    {
                        var path = context.Request.Path.Value;
                        if (path is not null
                            && path != "/"
                            && !path.EndsWith('/')
                            && !Path.HasExtension(path)
                            && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
                        {
                            context.Response.StatusCode = 301;
                            context.Response.Headers.Location = path + "/" + context.Request.QueryString.Value;
                            return;
                        }
                        await next();
                    });

                    app.UseRouting();

                    app.UseEndpoints(endpoints =>
                    {
                        // Minimal test endpoints
                        endpoints.MapGet("/", () => "home");
                        endpoints.MapGet("/test-post/", () => "post");
                        endpoints.MapGet("/tag/csharp/", () => "tag");
                        endpoints.MapGet("/author/john/", () => "author");
                        endpoints.MapGet("/rss/", () => "rss");
                        endpoints.MapGet("/sitemap/", () => "sitemap");
                        endpoints.MapGet("/assets/style.css", () => "css");
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

    [Fact]
    public async Task TrailingSlash_Redirect_ForContentUrl()
    {
        var response = await _client.GetAsync("/test-post");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/test-post/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TrailingSlash_NoRedirect_WhenAlreadyHasSlash()
    {
        var response = await _client.GetAsync("/test-post/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TrailingSlash_NoRedirect_ForRoot()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TrailingSlash_NoRedirect_ForStaticFile()
    {
        var response = await _client.GetAsync("/assets/style.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TrailingSlash_Redirect_PreservesQueryString()
    {
        var response = await _client.GetAsync("/test-post?key=abc123");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/test-post/?key=abc123", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TrailingSlash_NoRedirect_ForPost()
    {
        var content = new StringContent("body");
        var response = await _client.PostAsync("/test-post", content);

        // POST requests should not be redirected
        Assert.NotEqual(HttpStatusCode.MovedPermanently, response.StatusCode);
    }

    [Fact]
    public async Task TrailingSlash_Redirect_ForTagUrl()
    {
        var response = await _client.GetAsync("/tag/csharp");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/tag/csharp/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TrailingSlash_Redirect_ForAuthorUrl()
    {
        var response = await _client.GetAsync("/author/john");

        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal("/author/john/", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task TagRoute_WithTrailingSlash_ReturnsOk()
    {
        var response = await _client.GetAsync("/tag/csharp/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthorRoute_WithTrailingSlash_ReturnsOk()
    {
        var response = await _client.GetAsync("/author/john/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
