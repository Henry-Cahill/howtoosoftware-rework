using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Admin.Pages.Account;

[AllowAnonymous]
public class LoginModel(
    UserManager<User> userManager,
    IBruteForceService bruteForce) : PageModel
{
    private static readonly string[] StaffRoles =
        ["Owner", "Administrator", "Editor", "Author", "Contributor"];

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(GetSafeReturnUrl());

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Page();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var bruteKey = $"admin-login:{ip}";
        if (await bruteForce.TrackAsync(bruteKey, 5, TimeSpan.FromHours(1), ct))
        {
            ErrorMessage = "Too many login attempts. Please try again later.";
            return Page();
        }

        var user = await userManager.FindByEmailAsync(Input.Email);
        if (user is null || user.Status != "active")
        {
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        // Check lockout
        if (await userManager.IsLockedOutAsync(user))
        {
            ErrorMessage = "Account is locked. Please try again later.";
            return Page();
        }

        // Verify password
        if (!await userManager.CheckPasswordAsync(user, Input.Password))
        {
            await userManager.AccessFailedAsync(user);
            ErrorMessage = "Invalid email or password.";
            return Page();
        }

        // Verify user has a staff role
        var roles = await userManager.GetRolesAsync(user);
        var staffRoles = roles.Where(r => StaffRoles.Contains(r)).ToList();
        if (staffRoles.Count == 0)
        {
            ErrorMessage = "You do not have permission to access the admin panel.";
            return Page();
        }

        // Reset failed access count on successful login
        await userManager.ResetAccessFailedCountAsync(user);
        await bruteForce.ResetAsync(bruteKey, ct);

        // Update last-seen
        user.LastSeen = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        // Build claims and sign in
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email!),
        };
        foreach (var role in staffRoles)
            claims.Add(new(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = Input.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : null,
            });

        return LocalRedirect(GetSafeReturnUrl());
    }

    private string GetSafeReturnUrl()
    {
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return ReturnUrl;
        return Url.Content("~/");
    }
}

public class LoginInput
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
