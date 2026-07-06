using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure;
using HowToSoftware.Infrastructure.Data;
using HowToSoftware.Web.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Ghost-style pagination routes: /page/2/, /tag/{slug}/page/2/, /author/{slug}/page/2/
    options.Conventions.AddPageRoute("/Index", "/page/{pageNumber:int}");
    options.Conventions.AddPageRoute("/Tag", "/tag/{slug}/page/{pageNumber:int}");
    options.Conventions.AddPageRoute("/Author", "/author/{slug}/page/{pageNumber:int}");
    options.Conventions.AddPageRoute("/NewsletterArchive", "/newsletter/{slug}/archive/page/{pageNumber:int}");
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddInfrastructure(builder.Configuration, images =>
    images.RootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));

builder.Services.AddControllers();

builder.Services.AddAuthentication()
    .AddScheme<ContentApiKeyOptions, ContentApiKeyHandler>(
        ContentApiDefaults.AuthenticationScheme, _ => { })
    .AddCookie("MemberCookie", options =>
    {
        options.Cookie.Name = "hts_member";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/signin/";
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("ContentApi", policy =>
        policy.AddAuthenticationSchemes(ContentApiDefaults.AuthenticationScheme)
              .RequireAuthenticatedUser());

// Rate limiting — per-IP and per-key sliding windows for public endpoints
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Per-IP: analytics event ingestion — 60 req/min
    options.AddPolicy("analytics", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Per-IP: donation checkout — 5 req/min
    options.AddPolicy("donations", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Per-IP: auth endpoints (magic-link, signup) — 10 req/min
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Per-IP: recommendation click/subscribe — 30 req/min
    options.AddPolicy("recommendations", httpContext =>
        RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            }));

    // Per-API-key: content API — 100 req/min per key
    options.AddPolicy("content-api", httpContext =>
    {
        var key = httpContext.Request.Headers.Authorization.FirstOrDefault()
                      ?.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
                  ?? httpContext.Request.Query["key"].FirstOrDefault()
                  ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
foreach (var proxy in app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
{
    if (System.Net.IPAddress.TryParse(proxy, out var ip))
        forwardedHeadersOptions.KnownProxies.Add(ip);
}
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseStatusCodePagesWithReExecute("/Error/{0}");

// Disable status-code error pages for API routes — return JSON, not HTML
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IStatusCodePagesFeature>();
        if (feature is not null) feature.Enabled = false;
    }
    await next();
});

// HTTPS redirect is handled by Caddy reverse proxy; only redirect in dev without proxy
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// Serve uploaded images from /content/images/ with aggressive caching
var imagesPath = Path.Combine(app.Environment.WebRootPath, "content", "images");
Directory.CreateDirectory(imagesPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imagesPath),
    RequestPath = "/content/images",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    }
});

// Redirect legacy Ghost CMS URLs (API paths, sitemap variants) to their .NET equivalents
app.UseMiddleware<HowToSoftware.Web.GhostUrlRedirectMiddleware>();

// Redirect non-trailing-slash content URLs to trailing-slash (Ghost URL compatibility)
// Skip API routes — they don't use trailing-slash conventions
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path is not null
        && path != "/"
        && !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
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

// Custom URL redirects (301/302) stored in DB — skip API routes
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path is not null
        && !path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
        && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)))
    {
        var redirectService = context.RequestServices.GetRequiredService<IRedirectService>();
        var match = await redirectService.MatchAsync(path);
        if (match is not null)
        {
            var target = match.Target;
            if (context.Request.QueryString.HasValue)
                target += context.Request.QueryString.Value;

            context.Response.StatusCode = 301;
            context.Response.Headers.Location = target;
            await redirectService.IncrementHitCountAsync(match.Id);
            return;
        }
    }
    await next();
});

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

// IndexNow key verification: search engines request /{apiKey}.txt to verify ownership
app.MapGet("/{key}.txt", async (string key, ISettingsService settingsService, CancellationToken ct) =>
{
    var apiKey = await settingsService.GetStringAsync("indexnow_api_key", ct);
    if (string.IsNullOrEmpty(apiKey) || !string.Equals(key, apiKey, StringComparison.OrdinalIgnoreCase))
        return Results.NotFound();

    return Results.Text(apiKey, "text/plain");
});

app.Run();

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
