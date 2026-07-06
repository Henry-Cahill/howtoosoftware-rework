using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Pages;

public class SigninModel(
    IMagicLinkService magicLink,
    ISettingsService settings,
    IBruteForceService bruteForce) : PageModel
{
    [BindProperty]
    public SigninInput Input { get; set; } = new();

    public string SiteTitle { get; set; } = "";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        SiteTitle = await settings.GetStringAsync("title", ct) ?? "HowToSoftware";
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        SiteTitle = await settings.GetStringAsync("title", ct) ?? "HowToSoftware";

        if (!ModelState.IsValid)
            return Page();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (await bruteForce.TrackAsync($"magic-link:{ip}", 5, TimeSpan.FromMinutes(10), ct))
        {
            ErrorMessage = "Too many attempts. Please try again later.";
            return Page();
        }

        var siteUrl = $"{Request.Scheme}://{Request.Host}";

        await magicLink.SendMagicLinkAsync(new MagicLinkRequest
        {
            Email = Input.Email,
            SiteUrl = siteUrl,
        }, ct);

        // Always show success to prevent email enumeration
        SuccessMessage = "Check your inbox! We just sent you a sign-in link.";
        return Page();
    }
}

public class SigninInput
{
    [EmailAddress]
    [Required(ErrorMessage = "Email is required.")]
    [StringLength(191)]
    public string Email { get; set; } = "";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
