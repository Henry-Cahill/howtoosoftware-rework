using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public sealed class MagicLinkService : IMagicLinkService
{
    private readonly AppDbContext _db;
    private readonly IMemberRepository _members;
    private readonly IEmailService _email;
    private readonly ISettingsService _settings;
    private readonly MailSettings _mailSettings;
    private readonly ILogger<MagicLinkService> _logger;

    /// <summary>How long a magic-link token stays valid.</summary>
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);

    public MagicLinkService(
        AppDbContext db,
        IMemberRepository members,
        IEmailService email,
        ISettingsService settings,
        IOptions<MailSettings> mailSettings,
        ILogger<MagicLinkService> logger)
    {
        _db = db;
        _members = members;
        _email = email;
        _settings = settings;
        _mailSettings = mailSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendMagicLinkAsync(MagicLinkRequest request, CancellationToken ct = default)
    {
        var member = await _members.GetByEmailAsync(request.Email, ct);
        if (member is null)
        {
            // Don't reveal whether the email exists — return success silently.
            _logger.LogInformation("Magic-link requested for unknown email (suppressed)");
            return true;
        }

        // Generate a cryptographically random token
        var rawToken = GenerateToken();
        var hashedToken = HashToken(rawToken);

        var tokenEntity = new Token
        {
            Id = ObjectIdGenerator.New(),
            TokenValue = hashedToken,
            Uuid = Guid.NewGuid().ToString("D"),
            Data = JsonSerializer.Serialize(new TokenData
            {
                MemberId = member.Id,
                Email = member.Email,
                RedirectUrl = request.RedirectUrl,
            }),
            CreatedAt = DateTime.UtcNow,
            UsedCount = 0,
        };

        _db.Set<Token>().Add(tokenEntity);
        await _db.SaveChangesAsync(ct);

        // Build the verification URL
        var siteUrl = request.SiteUrl.TrimEnd('/');
        var verifyUrl = $"{siteUrl}/api/member/magic-link/verify?token={Uri.EscapeDataString(rawToken)}";

        // Build and send the email
        var siteTitle = await _settings.GetStringAsync("title", ct) ?? "HowToSoftware";
        var defaultFrom = await _settings.GetStringAsync("members_support_address", ct)
                          ?? await BuildDefaultSenderAsync(siteUrl, ct);

        var html = BuildMagicLinkEmailHtml(siteTitle, siteUrl, verifyUrl);

        var result = await _email.SendAsync(new EmailMessage
        {
            From = $"{siteTitle} <{defaultFrom}>",
            To = member.Email,
            Subject = $"Sign in to {siteTitle}",
            Html = html,
            Plaintext = $"Sign in to {siteTitle}\n\nClick this link to sign in:\n{verifyUrl}\n\nThis link expires in 10 minutes and can only be used once.",
        }, ct);

        if (!result.Success)
        {
            _logger.LogError("Failed to send magic-link email to {Email}: {Error}", member.Email, result.ErrorMessage);
            return false;
        }

        _logger.LogInformation("Magic-link email sent to member {MemberId}", member.Id);
        return true;
    }

    public async Task<Member?> VerifyTokenAsync(string tokenValue, CancellationToken ct = default)
    {
        var hashedToken = HashToken(tokenValue);

        var token = await _db.Set<Token>()
            .FirstOrDefaultAsync(t => t.TokenValue == hashedToken, ct);

        if (token is null)
        {
            _logger.LogWarning("Magic-link verification failed: token not found");
            return null;
        }

        // Check expiry
        if (DateTime.UtcNow - token.CreatedAt > TokenLifetime)
        {
            _logger.LogWarning("Magic-link verification failed: token expired (created {CreatedAt})", token.CreatedAt);
            return null;
        }

        // Check single-use
        if (token.UsedCount > 0)
        {
            _logger.LogWarning("Magic-link verification failed: token already used");
            return null;
        }

        // Parse embedded data
        TokenData? data = null;
        if (!string.IsNullOrEmpty(token.Data))
        {
            data = JsonSerializer.Deserialize<TokenData>(token.Data);
        }

        if (data?.MemberId is null)
        {
            _logger.LogWarning("Magic-link verification failed: token data missing member ID");
            return null;
        }

        // Mark token as used
        token.UsedCount++;
        token.FirstUsedAt ??= DateTime.UtcNow;
        token.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        // Retrieve member
        var member = await _members.GetByIdAsync(data.MemberId, ct);
        if (member is null)
        {
            _logger.LogWarning("Magic-link verification failed: member {MemberId} not found", data.MemberId);
            return null;
        }

        // Record login event
        _db.Set<MembersLoginEvent>().Add(new MembersLoginEvent
        {
            Id = ObjectIdGenerator.New(),
            MemberId = member.Id,
            CreatedAt = DateTime.UtcNow,
        });

        // Update member last-seen
        member.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Magic-link verified for member {MemberId}", member.Id);
        return member;
    }

    public async Task<bool> SendSignupVerificationAsync(Member member, string siteUrl, CancellationToken ct = default)
    {
        var rawToken = GenerateToken();
        var hashedToken = HashToken(rawToken);

        var tokenEntity = new Token
        {
            Id = ObjectIdGenerator.New(),
            TokenValue = hashedToken,
            Uuid = Guid.NewGuid().ToString("D"),
            Data = JsonSerializer.Serialize(new TokenData
            {
                MemberId = member.Id,
                Email = member.Email,
                Type = "signup-verification",
            }),
            CreatedAt = DateTime.UtcNow,
            UsedCount = 0,
        };

        _db.Set<Token>().Add(tokenEntity);
        await _db.SaveChangesAsync(ct);

        var url = siteUrl.TrimEnd('/');
        var verifyUrl = $"{url}/api/member/signup/verify?token={Uri.EscapeDataString(rawToken)}";

        var siteTitle = await _settings.GetStringAsync("title", ct) ?? "HowToSoftware";
        var defaultFrom = await _settings.GetStringAsync("members_support_address", ct)
                          ?? await BuildDefaultSenderAsync(url, ct);

        var html = BuildSignupVerificationEmailHtml(siteTitle, url, verifyUrl);

        var result = await _email.SendAsync(new EmailMessage
        {
            From = $"{siteTitle} <{defaultFrom}>",
            To = member.Email,
            Subject = $"Confirm your email for {siteTitle}",
            Html = html,
            Plaintext = $"Confirm your email for {siteTitle}\n\nClick this link to confirm your email and activate your account:\n{verifyUrl}\n\nThis link expires in 10 minutes and can only be used once.",
        }, ct);

        if (!result.Success)
        {
            _logger.LogError("Failed to send signup verification email to {Email}: {Error}", member.Email, result.ErrorMessage);
            return false;
        }

        _logger.LogInformation("Signup verification email sent to member {MemberId}", member.Id);
        return true;
    }

    public async Task<Member?> VerifySignupTokenAsync(string tokenValue, CancellationToken ct = default)
    {
        var hashedToken = HashToken(tokenValue);

        var token = await _db.Set<Token>()
            .FirstOrDefaultAsync(t => t.TokenValue == hashedToken, ct);

        if (token is null)
        {
            _logger.LogWarning("Signup verification failed: token not found");
            return null;
        }

        if (DateTime.UtcNow - token.CreatedAt > TokenLifetime)
        {
            _logger.LogWarning("Signup verification failed: token expired (created {CreatedAt})", token.CreatedAt);
            return null;
        }

        if (token.UsedCount > 0)
        {
            _logger.LogWarning("Signup verification failed: token already used");
            return null;
        }

        TokenData? data = null;
        if (!string.IsNullOrEmpty(token.Data))
        {
            data = JsonSerializer.Deserialize<TokenData>(token.Data);
        }

        if (data?.MemberId is null || data.Type != "signup-verification")
        {
            _logger.LogWarning("Signup verification failed: token data invalid or wrong type");
            return null;
        }

        token.UsedCount++;
        token.FirstUsedAt ??= DateTime.UtcNow;
        token.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var member = await _members.GetByIdAsync(data.MemberId, ct);
        if (member is null)
        {
            _logger.LogWarning("Signup verification failed: member {MemberId} not found", data.MemberId);
            return null;
        }

        _db.Set<MembersLoginEvent>().Add(new MembersLoginEvent
        {
            Id = ObjectIdGenerator.New(),
            MemberId = member.Id,
            CreatedAt = DateTime.UtcNow,
        });

        member.LastSeenAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Signup verification confirmed for member {MemberId}", member.Id);
        return member;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds a default sender address that aligns with the configured Mailgun
    /// sending domain (so SPF + DKIM pass). Resolution order:
    ///   1. "mailgun_domain" setting in the DB (admin-configurable override)
    ///   2. MailSettings.MailgunDomain (env / appsettings \u2014 the domain that
    ///      SMTP is actually authenticated against, so SPF/DKIM align)
    ///   3. Site host as a last resort \u2014 that path will likely spam-fold
    ///      because the apex SPF usually does not authorize Mailgun.
    /// </summary>
    private async Task<string> BuildDefaultSenderAsync(string siteUrl, CancellationToken ct)
    {
        var mailgunDomain = await _settings.GetStringAsync("mailgun_domain", ct);
        if (!string.IsNullOrWhiteSpace(mailgunDomain)) return $"noreply@{mailgunDomain}";
        if (!string.IsNullOrWhiteSpace(_mailSettings.MailgunDomain))
            return $"noreply@{_mailSettings.MailgunDomain}";
        return $"noreply@{new Uri(siteUrl).Host}";
    }

    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }

    private static string BuildMagicLinkEmailHtml(string siteTitle, string siteUrl, string verifyUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"></head>
            <body style="margin:0;padding:0;background-color:#f4f4f4;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,Ubuntu,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;padding:40px;">
                    <tr><td style="text-align:center;padding-bottom:24px;">
                      <h1 style="margin:0;font-size:24px;color:#15171a;">{Encode(siteTitle)}</h1>
                    </td></tr>
                    <tr><td style="text-align:center;padding-bottom:24px;">
                      <p style="margin:0;font-size:16px;color:#394047;line-height:1.5;">
                        Hey there,<br>click the button below to sign in.
                      </p>
                    </td></tr>
                    <tr><td style="text-align:center;padding-bottom:24px;">
                      <a href="{Encode(verifyUrl)}"
                         style="display:inline-block;padding:12px 32px;background:#15171a;color:#ffffff;
                                text-decoration:none;border-radius:5px;font-size:16px;font-weight:600;">
                        Sign in
                      </a>
                    </td></tr>
                    <tr><td style="text-align:center;">
                      <p style="margin:0;font-size:13px;color:#92999f;line-height:1.4;">
                        This link expires in 10 minutes and can only be used once.<br>
                        If you did not request this email you can safely ignore it.
                      </p>
                    </td></tr>
                  </table>
                  <p style="margin-top:20px;font-size:12px;color:#92999f;">
                    Sent from <a href="{Encode(siteUrl)}" style="color:#92999f;">{Encode(siteTitle)}</a>
                  </p>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string BuildSignupVerificationEmailHtml(string siteTitle, string siteUrl, string verifyUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"></head>
            <body style="margin:0;padding:0;background-color:#f4f4f4;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Oxygen,Ubuntu,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:8px;padding:40px;">
                    <tr><td style="text-align:center;padding-bottom:24px;">
                      <h1 style="margin:0;font-size:24px;color:#15171a;">{Encode(siteTitle)}</h1>
                    </td></tr>
                    <tr><td style="text-align:center;padding-bottom:24px;">
                      <p style="margin:0;font-size:16px;color:#394047;line-height:1.5;">
                        Welcome! Please confirm your email address<br>by clicking the button below.
                      </p>
                    </td></tr>
                    <tr><td style="text-align:center;padding-bottom:24px;">
                      <a href="{Encode(verifyUrl)}"
                         style="display:inline-block;padding:12px 32px;background:#15171a;color:#ffffff;
                                text-decoration:none;border-radius:5px;font-size:16px;font-weight:600;">
                        Confirm your email
                      </a>
                    </td></tr>
                    <tr><td style="text-align:center;">
                      <p style="margin:0;font-size:13px;color:#92999f;line-height:1.4;">
                        This link expires in 10 minutes and can only be used once.<br>
                        If you did not sign up you can safely ignore this email.
                      </p>
                    </td></tr>
                  </table>
                  <p style="margin-top:20px;font-size:12px;color:#92999f;">
                    Sent from <a href="{Encode(siteUrl)}" style="color:#92999f;">{Encode(siteTitle)}</a>
                  </p>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Encode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);

    private sealed class TokenData
    {
        public string? MemberId { get; set; }
        public string? Email { get; set; }
        public string? RedirectUrl { get; set; }
        public string? Type { get; set; }
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
