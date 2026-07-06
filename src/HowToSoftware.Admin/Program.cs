using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using HowToSoftware.Admin.Components;
using HowToSoftware.Admin.Hubs;
using HowToSoftware.Admin.Services;
using HowToSoftware.Infrastructure;
using HowToSoftware.Infrastructure.Data;
using HowToSoftware.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Data layer
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddInfrastructure(builder.Configuration, images =>
    images.RootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));

// Persist Data Protection keys so antiforgery tokens survive container restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("HowToSoftware.Admin");

// Cookie authentication for staff login
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.Name = ".HowToSoftware.Admin";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Owner", "Administrator"));

    options.AddPolicy("EditorOrAbove", policy =>
        policy.RequireRole("Owner", "Administrator", "Editor"));
});
builder.Services.AddCascadingAuthenticationState();

// Razor Pages for login/logout (Blazor can't set cookies during interactive rendering)
builder.Services.AddRazorPages();

// SignalR for real-time analytics
builder.Services.AddSignalR();

// Background service for hourly/daily analytics rollups
builder.Services.AddHostedService<AnalyticsRollupService>();

// Background service for real-time live visitor count + recent pageviews
builder.Services.AddHostedService<LiveAnalyticsService>();

// Background service for email batch sending (pending → batched → sent via Mailgun)
builder.Services.AddHostedService<EmailBatchSenderService>();

// Background service for A/B subject-line winner resolution and holdout send
builder.Services.AddHostedService<EmailAbTestWinnerService>();

// Background service for delayed / drip-sequence automated emails
builder.Services.AddHostedService<AutomatedEmailDripService>();

// Blazor Server components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UsePathBase("/ghost");
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// HTTPS redirect is handled by Caddy reverse proxy; only redirect in dev without proxy
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapStaticAssets().AllowAnonymous();
app.MapRazorPages();
app.MapHub<AnalyticsHub>("/hubs/analytics");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Image upload API for the post editor
app.MapPost("/api/upload/image", async (
    HttpRequest request,
    HowToSoftware.Core.Interfaces.IImageStorageService imageStorage) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Expected multipart form data.");

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");

    if (file.Length > 10 * 1024 * 1024)
        return Results.BadRequest("File exceeds 10 MB limit.");

    using var stream = file.OpenReadStream();
    var result = await imageStorage.UploadAsync(stream, file.FileName, file.ContentType);
    return Results.Ok(new { result.Url, result.Width, result.Height, result.FileSize });
}).RequireAuthorization();

app.Run();

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
