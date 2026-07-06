using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Admin.Pages.Account;

[AllowAnonymous]
public class AcceptInviteModel(
    IInviteRepository invites,
    IUserRepository users,
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    ISlugGenerator slugGenerator,
    AppDbContext db,
    ILogger<AcceptInviteModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public AcceptInviteInput Input { get; set; } = new();

    public string? ErrorMessage { get; set; }
    public string Email { get; set; } = "";
    public string RoleName { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var invite = await ResolveInviteAsync(ct);
        if (invite is null)
            return Page(); // ErrorMessage is set by ResolveInviteAsync

        Email = invite.Email;
        RoleName = invite.Role?.Name ?? "Staff";
        Input.Email = invite.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var invite = await ResolveInviteAsync(ct);
        if (invite is null)
            return Page();

        Email = invite.Email;
        RoleName = invite.Role?.Name ?? "Staff";

        if (!ModelState.IsValid)
            return Page();

        var name = Input.Name.Trim();
        var existing = await userManager.FindByEmailAsync(invite.Email);

        User user;
        bool isNewUser = existing is null;

        if (existing is not null)
        {
            // The invite token is itself proof of email control, so allow finishing setup
            // for any existing row with a matching email — this also recovers from earlier
            // partial provisioning (e.g. a row created without a role assignment).
            user = existing;
            if (string.IsNullOrWhiteSpace(user.Name)) user.Name = name;
            if (string.IsNullOrWhiteSpace(user.Slug))
            {
                user.Slug = await slugGenerator.GenerateUniqueSlugAsync(
                    name,
                    async candidate => await users.GetBySlugAsync(candidate, ct) is not null,
                    ct);
            }
            user.EmailConfirmed = true;
            user.Status = "active";
            user.UpdatedAt = DateTime.UtcNow;

            // Reset password (works whether or not one was set previously).
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var pwdResult = await userManager.ResetPasswordAsync(user, resetToken, Input.Password);
            if (!pwdResult.Succeeded)
            {
                ErrorMessage = string.Join(" ", pwdResult.Errors.Select(e => e.Description));
                return Page();
            }
            await userManager.UpdateAsync(user);
        }
        else
        {
            var slug = await slugGenerator.GenerateUniqueSlugAsync(
                name,
                async candidate => await users.GetBySlugAsync(candidate, ct) is not null,
                ct);

            user = new User
            {
                Id = Guid.NewGuid().ToString("N")[..24],
                UserName = invite.Email,
                Email = invite.Email,
                EmailConfirmed = true,
                Name = name,
                Slug = slug,
                Status = "active",
                Visibility = "public",
                // Legacy Ghost [password] column is NOT NULL with no default — store an
                // empty string; the real Identity password lives in [password_hash].
                GhostPassword = string.Empty,
                CreatedAt = DateTime.UtcNow,
            };

            var createResult = await userManager.CreateAsync(user, Input.Password);
            if (!createResult.Succeeded)
            {
                ErrorMessage = string.Join(" ", createResult.Errors.Select(e => e.Description));
                return Page();
            }
        }

        // Assign the invited role. The custom RolesUser entity has a single-Id PK
        // (not Identity's composite (UserId, RoleId)), so UserManager.IsInRoleAsync /
        // AddToRoleAsync throw "single key property, but 2 values were passed". Insert
        // the join row directly via AppDbContext.
        var role = await roleManager.FindByIdAsync(invite.RoleId);
        if (role is not null)
        {
            var alreadyAssigned = await db.Set<RolesUser>()
                .AnyAsync(ru => ru.UserId == user.Id && ru.RoleId == role.Id, ct);
            if (!alreadyAssigned)
            {
                db.Set<RolesUser>().Add(new RolesUser
                {
                    Id = Guid.NewGuid().ToString("N")[..24],
                    UserId = user.Id,
                    RoleId = role.Id,
                });
                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to assign role {RoleName} to user {Email}",
                        role.Name, user.Email);
                    if (isNewUser)
                        await userManager.DeleteAsync(user);
                    ErrorMessage = "Failed to assign role to account. Please contact an administrator.";
                    return Page();
                }
            }
        }
        else
        {
            logger.LogWarning("Invite {InviteId} references unknown role {RoleId}; user created without staff role.",
                invite.Id, invite.RoleId);
        }

        // Consume the invite
        await invites.DeleteAsync(invite.Id, ct);

        // Sign the new user in
        var roles = role?.Name is not null ? new[] { role.Name } : Array.Empty<string>();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email!),
        };
        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        logger.LogInformation("Invite {InviteId} accepted by {Email} as {Role}", invite.Id, user.Email, role?.Name);

        return LocalRedirect("~/");
    }

    private async Task<Invite?> ResolveInviteAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "This invitation link is missing its token.";
            return null;
        }

        var invite = await invites.GetByTokenAsync(Token, ct);
        if (invite is null)
        {
            ErrorMessage = "This invitation is invalid or has already been used.";
            return null;
        }

        // Expires is stored as a Unix epoch in milliseconds (Ghost convention).
        var expiresUtc = DateTimeOffset.FromUnixTimeMilliseconds(invite.Expires).UtcDateTime;
        if (expiresUtc < DateTime.UtcNow)
        {
            ErrorMessage = "This invitation has expired. Ask an administrator to send a new one.";
            return null;
        }

        return invite;
    }
}

public class AcceptInviteInput
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 10, ErrorMessage = "Password must be at least 10 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
