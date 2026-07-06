using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Pages;

public class AccountModel(
    IMemberRepository memberRepository,
    INewsletterRepository newsletterRepository,
    INewsletterService newsletterService,
    ISettingsService settings) : PageModel
{
    private const string MemberCookieScheme = "MemberCookie";

    [BindProperty]
    public AccountInput Input { get; set; } = new();

    [BindProperty]
    public List<string> SubscribedNewsletterIds { get; set; } = [];

    public Member CurrentMember { get; set; } = null!;
    public List<Newsletter> AvailableNewsletters { get; set; } = [];
    public List<string> CurrentSubscriptionIds { get; set; } = [];
    public string SiteTitle { get; set; } = "";
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var member = await GetAuthenticatedMemberAsync(ct);
        if (member is null)
            return RedirectToPage("/Signin");

        await LoadPageDataAsync(member, ct);
        Input.Name = member.Name ?? "";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var member = await GetAuthenticatedMemberAsync(ct);
        if (member is null)
            return RedirectToPage("/Signin");

        await LoadPageDataAsync(member, ct);

        if (!ModelState.IsValid)
            return Page();

        // Update name
        var newName = string.IsNullOrWhiteSpace(Input.Name) ? null : Input.Name.Trim();
        if (member.Name != newName)
        {
            member.Name = newName;
            member.UpdatedAt = DateTime.UtcNow;
            await memberRepository.UpdateAsync(member, ct);

            // Update the session cookie to reflect the new name
            await RefreshMemberSessionAsync(member);
        }

        // Update newsletter subscriptions
        var currentSubIds = new HashSet<string>(CurrentSubscriptionIds, StringComparer.OrdinalIgnoreCase);
        var desiredSubIds = new HashSet<string>(SubscribedNewsletterIds, StringComparer.OrdinalIgnoreCase);
        var availableIds = new HashSet<string>(AvailableNewsletters.Select(n => n.Id), StringComparer.OrdinalIgnoreCase);

        // Only process newsletters that are actually available
        foreach (var nlId in availableIds)
        {
            if (desiredSubIds.Contains(nlId) && !currentSubIds.Contains(nlId))
            {
                await newsletterService.SubscribeAsync(member.Id, nlId, ct);
            }
            else if (!desiredSubIds.Contains(nlId) && currentSubIds.Contains(nlId))
            {
                await newsletterService.UnsubscribeAsync(member.Id, nlId, ct);
            }
        }

        // Reload subscriptions after update
        CurrentSubscriptionIds = await newsletterService.GetMemberSubscriptionsAsync(member.Id, ct);

        SuccessMessage = "Your settings have been saved.";
        return Page();
    }

    public async Task<IActionResult> OnPostSignOutAsync()
    {
        await HttpContext.SignOutAsync(MemberCookieScheme);
        return Redirect("/");
    }

    private async Task<Member?> GetAuthenticatedMemberAsync(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync(MemberCookieScheme);
        if (result.Succeeded != true)
            return null;

        var memberId = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(memberId))
            return null;

        return await memberRepository.GetByIdAsync(memberId, ct);
    }

    private async Task LoadPageDataAsync(Member member, CancellationToken ct)
    {
        CurrentMember = member;
        SiteTitle = await settings.GetStringAsync("title", ct) ?? "HowToSoftware";

        var activeNewsletters = await newsletterRepository.GetActiveAsync(ct);
        AvailableNewsletters = activeNewsletters
            .Where(n => n.Visibility == "members" || n.Visibility == "public")
            .ToList();

        CurrentSubscriptionIds = await newsletterService.GetMemberSubscriptionsAsync(member.Id, ct);
    }

    private async Task RefreshMemberSessionAsync(Member member)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, member.Id),
            new("member_uuid", member.Uuid),
            new(ClaimTypes.Email, member.Email),
            new("member_status", member.Status),
        };

        if (!string.IsNullOrEmpty(member.Name))
            claims.Add(new(ClaimTypes.Name, member.Name));

        var identity = new ClaimsIdentity(claims, MemberCookieScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(MemberCookieScheme, principal, new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
        });
    }
}

public class AccountInput
{
    [StringLength(191)]
    public string? Name { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
