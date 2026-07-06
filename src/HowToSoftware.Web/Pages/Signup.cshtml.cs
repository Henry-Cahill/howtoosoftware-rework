using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Pages;

public class SignupModel(
    IMemberService memberService,
    INewsletterRepository newsletterRepository,
    ISettingsService settings,
    IBruteForceService bruteForce) : PageModel
{
    [BindProperty]
    public SignupInput Input { get; set; } = new();

    [BindProperty]
    public List<string> NewsletterIds { get; set; } = [];

    public List<Newsletter> AvailableNewsletters { get; set; } = [];
    public string SiteTitle { get; set; } = "";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadPageDataAsync();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadPageDataAsync(ct);

        if (!ModelState.IsValid)
            return Page();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (await bruteForce.TrackAsync($"signup:{ip}", 5, TimeSpan.FromMinutes(10), ct))
        {
            ErrorMessage = "Too many attempts. Please try again later.";
            return Page();
        }

        var siteUrl = $"{Request.Scheme}://{Request.Host}";

        var member = await memberService.SignupAsync(new MemberSignupRequest
        {
            Email = Input.Email,
            Name = string.IsNullOrWhiteSpace(Input.Name) ? null : Input.Name.Trim(),
            NewsletterIds = NewsletterIds.Count > 0 ? NewsletterIds : null,
            SiteUrl = siteUrl,
        }, ct);

        if (member is null)
        {
            // Email already exists — show generic success to prevent enumeration
            SuccessMessage = "Great! Check your inbox for a confirmation email.";
            return Page();
        }

        SuccessMessage = "You've successfully subscribed! Check your inbox for a welcome email.";
        return Page();
    }

    private async Task LoadPageDataAsync(CancellationToken ct = default)
    {
        SiteTitle = await settings.GetStringAsync("title", ct) ?? "HowToSoftware";
        var activeNewsletters = await newsletterRepository.GetActiveAsync(ct);
        AvailableNewsletters = activeNewsletters
            .Where(n => n.Visibility == "members" || n.Visibility == "public")
            .ToList();
    }
}

public class SignupInput
{
    [EmailAddress]
    [Required(ErrorMessage = "Email is required.")]
    [StringLength(191)]
    public string Email { get; set; } = "";

    [StringLength(191)]
    public string? Name { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
