using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;
using HowToSoftware.Infrastructure.Data;

namespace HowToSoftware.Infrastructure.Services;

public sealed partial class MentionService(
    AppDbContext db,
    IHttpClientFactory httpClientFactory,
    IPostRepository postRepository,
    IEmailService emailService,
    ISettingsService settingsService,
    IOptions<MailSettings> mailOptions,
    ILogger<MentionService> logger) : IMentionService
{
    public async Task<List<Mention>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Mentions
            .Where(m => !m.Deleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetPendingCountAsync(CancellationToken ct = default)
    {
        return await db.Mentions
            .CountAsync(m => !m.Deleted && m.Status == MentionStatus.Pending, ct);
    }

    public async Task<Mention?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        return await db.Mentions.FindAsync([id], ct);
    }

    public async Task<List<Mention>> GetByTargetAsync(string targetUrl, CancellationToken ct = default)
    {
        return await db.Mentions
            .Where(m => m.Target == targetUrl
                     && m.Verified
                     && !m.Deleted
                     && m.Status == MentionStatus.Approved)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Mention>> GetByResourceAsync(
        string resourceId, string resourceType, CancellationToken ct = default)
    {
        return await db.Mentions
            .Where(m => m.ResourceId == resourceId
                     && m.ResourceType == resourceType
                     && m.Verified
                     && !m.Deleted
                     && m.Status == MentionStatus.Approved)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Mention> ReceiveAsync(
        string source, string target, CancellationToken ct = default)
    {
        // Validate URLs
        if (!Uri.TryCreate(source, UriKind.Absolute, out var sourceUri)
            || (sourceUri.Scheme != "http" && sourceUri.Scheme != "https"))
            throw new ArgumentException("Invalid source URL");

        if (!Uri.TryCreate(target, UriKind.Absolute, out var targetUri)
            || (targetUri.Scheme != "http" && targetUri.Scheme != "https"))
            throw new ArgumentException("Invalid target URL");

        // Source and target must differ
        if (sourceUri == targetUri)
            throw new ArgumentException("Source and target must be different URLs");

        // Check for existing mention from same source→target
        var existing = await db.Mentions
            .FirstOrDefaultAsync(m => m.Source == source && m.Target == target, ct);

        // Fetch source page and verify it links to target
        var (verified, title, excerpt, author, siteTitle, favicon, featuredImage) =
            await FetchAndParseSourceAsync(sourceUri, targetUri, ct);

        // Resolve which post/page this target maps to
        string? resourceId = null;
        string? resourceType = null;
        var targetPath = targetUri.AbsolutePath.Trim('/');
        if (!string.IsNullOrEmpty(targetPath))
        {
            var post = await postRepository.GetBySlugAsync(targetPath, ct);
            if (post is not null)
            {
                resourceId = post.Id;
                resourceType = post.Type; // "post" or "page"
            }
        }

        if (existing is not null)
        {
            // Update existing mention
            existing.SourceTitle = title;
            existing.SourceSiteTitle = siteTitle;
            existing.SourceExcerpt = excerpt;
            existing.SourceAuthor = author;
            existing.SourceFeaturedImage = featuredImage;
            existing.SourceFavicon = favicon;
            existing.ResourceId = resourceId;
            existing.ResourceType = resourceType;
            existing.Verified = verified;
            existing.Deleted = false;

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Updated webmention {MentionId}: {Source} → {Target} (verified={Verified})",
                existing.Id, LogSanitizer.SanitizeForLog(source), LogSanitizer.SanitizeForLog(target), verified);

            return existing;
        }

        var mention = new Mention
        {
            Id = Guid.NewGuid().ToString("N")[..24],
            Source = source,
            SourceTitle = title,
            SourceSiteTitle = siteTitle,
            SourceExcerpt = excerpt,
            SourceAuthor = author,
            SourceFeaturedImage = featuredImage,
            SourceFavicon = favicon,
            Target = target,
            ResourceId = resourceId,
            ResourceType = resourceType,
            CreatedAt = DateTime.UtcNow,
            Verified = verified,
            Deleted = false,
            Status = MentionStatus.Pending,
        };

        db.Mentions.Add(mention);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Received webmention {MentionId}: {Source} → {Target} (verified={Verified})",
            mention.Id, LogSanitizer.SanitizeForLog(source), LogSanitizer.SanitizeForLog(target), verified);

        // Fire-and-forget admin email notifications. Wrapped in try/catch so
        // a mailgun outage cannot break the public webmention endpoint.
        await NotifyStaffOfNewMentionSafelyAsync(mention, ct);

        return mention;
    }

    public async Task VerifyAsync(string id, CancellationToken ct = default)
    {
        var mention = await db.Mentions.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Mention {id} not found");

        if (!Uri.TryCreate(mention.Source, UriKind.Absolute, out var sourceUri)
            || !Uri.TryCreate(mention.Target, UriKind.Absolute, out var targetUri))
        {
            mention.Verified = false;
            await db.SaveChangesAsync(ct);
            return;
        }

        var (verified, title, excerpt, author, siteTitle, favicon, featuredImage) =
            await FetchAndParseSourceAsync(sourceUri, targetUri, ct);

        mention.SourceTitle = title;
        mention.SourceSiteTitle = siteTitle;
        mention.SourceExcerpt = excerpt;
        mention.SourceAuthor = author;
        mention.SourceFeaturedImage = featuredImage;
        mention.SourceFavicon = favicon;
        mention.Verified = verified;

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Re-verified mention {MentionId}: verified={Verified}", id, verified);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var mention = await db.Mentions.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Mention {id} not found");

        mention.Deleted = true;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Soft-deleted mention {MentionId}", id);
    }

    public async Task ApproveAsync(string id, CancellationToken ct = default)
    {
        var mention = await db.Mentions.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Mention {id} not found");

        mention.Status = MentionStatus.Approved;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Approved mention {MentionId}", id);
    }

    public async Task RejectAsync(string id, CancellationToken ct = default)
    {
        var mention = await db.Mentions.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Mention {id} not found");

        mention.Status = MentionStatus.Rejected;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Rejected mention {MentionId}", id);
    }

    // ── Source page fetching & parsing ──────────────────────────

    private async Task<(bool Verified, string? Title, string? Excerpt, string? Author,
        string? SiteTitle, string? Favicon, string? FeaturedImage)>
        FetchAndParseSourceAsync(Uri sourceUri, Uri targetUri, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient("Webmention");
            using var response = await client.GetAsync(sourceUri, ct);

            if (!response.IsSuccessStatusCode)
                return (false, null, null, null, null, null, null);

            var html = await response.Content.ReadAsStringAsync(ct);

            // Verify the source page actually links to the target
            var targetString = targetUri.ToString();
            var verified = html.Contains(targetString, StringComparison.OrdinalIgnoreCase);

            // Extract metadata from HTML
            var title = ExtractMetaContent(html, "og:title")
                     ?? ExtractTagContent(html, "title");
            var excerpt = ExtractMetaContent(html, "og:description")
                       ?? ExtractMetaContent(html, "description");
            var author = ExtractMetaContent(html, "author");
            var siteTitle = ExtractMetaContent(html, "og:site_name");
            var featuredImage = ExtractMetaContent(html, "og:image");

            // Favicon: look for <link rel="icon"> or default /favicon.ico
            var favicon = ExtractFaviconHref(html);
            if (favicon is not null && !Uri.IsWellFormedUriString(favicon, UriKind.Absolute))
            {
                favicon = new Uri(sourceUri, favicon).ToString();
            }
            favicon ??= new Uri(sourceUri, "/favicon.ico").ToString();

            // Sanitize all extracted metadata: strip HTML from text, validate URLs
            title = SanitizeText(title, maxLength: 300);
            excerpt = SanitizeText(excerpt, maxLength: 2000);
            author = SanitizeText(author, maxLength: 300);
            siteTitle = SanitizeText(siteTitle, maxLength: 300);
            featuredImage = SanitizeUrl(featuredImage);
            favicon = SanitizeUrl(favicon);

            return (verified, title, excerpt, author, siteTitle, favicon, featuredImage);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch source page {Source} for webmention verification",
                LogSanitizer.SanitizeForLog(sourceUri.ToString()));
            return (false, null, null, null, null, null, null);
        }
    }

    /// <summary>
    /// Strip all HTML tags from a string and truncate to a max length.
    /// Returns null if the input is null or only whitespace after stripping.
    /// </summary>
    private static string? SanitizeText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Strip all HTML tags
        var stripped = StripHtmlTagsRegex().Replace(value, string.Empty);
        stripped = WebUtility.HtmlDecode(stripped).Trim();

        if (string.IsNullOrWhiteSpace(stripped))
            return null;

        return stripped.Length > maxLength ? stripped[..maxLength] : stripped;
    }

    /// <summary>
    /// Validate that a URL string is an absolute HTTP(S) URL. Returns null if invalid.
    /// </summary>
    private static string? SanitizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;

        return uri.Scheme is "http" or "https" ? uri.ToString() : null;
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex StripHtmlTagsRegex();

    private static string? ExtractMetaContent(string html, string nameOrProperty)
    {
        // Match <meta name="X" content="Y"> or <meta property="X" content="Y">
        var pattern = $"""<meta\s+(?:name|property)\s*=\s*["']{Regex.Escape(nameOrProperty)}["']\s+content\s*=\s*["']([^"']*)["']""";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success) return WebUtility.HtmlDecode(match.Groups[1].Value);

        // Also check reversed attribute order: content before name/property
        var reversed = $"""<meta\s+content\s*=\s*["']([^"']*)["']\s+(?:name|property)\s*=\s*["']{Regex.Escape(nameOrProperty)}["']""";
        match = Regex.Match(html, reversed, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value) : null;
    }

    private static string? ExtractTagContent(string html, string tagName)
    {
        var pattern = $"<{tagName}[^>]*>([^<]*)</{tagName}>";
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value.Trim()) : null;
    }

    private static string? ExtractFaviconHref(string html)
    {
        var match = Regex.Match(html,
            """<link\s+[^>]*rel\s*=\s*["'](?:shortcut\s+)?icon["'][^>]*href\s*=\s*["']([^"']*)["']""",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : null;
    }

    // ── Admin notifications (MENT.3) ────────────────────────────

    /// <summary>
    /// Sends a notification email to every staff member with role
    /// "Owner" or "Administrator" who has <see cref="User.MentionNotifications"/>
    /// enabled. Wrapped in try/catch — never throws — so the public webmention
    /// endpoint stays up even when the mail provider is unavailable.
    /// </summary>
    private async Task NotifyStaffOfNewMentionSafelyAsync(Mention mention, CancellationToken ct)
    {
        try
        {
            // Look up notifiable staff: must have an email, opted in to
            // MentionNotifications, and hold an Owner/Administrator role.
            var roleNames = new[] { "Owner", "Administrator" };
            var recipients = await (
                from u in db.Users
                join ru in db.RolesUsers on u.Id equals ru.UserId
                join r in db.Roles on ru.RoleId equals r.Id
                where u.MentionNotifications
                   && u.Status == "active"
                   && u.Email != null
                   && r.Name != null
                   && roleNames.Contains(r.Name)
                select new { u.Id, u.Email, u.Name }
            ).Distinct().ToListAsync(ct);

            if (recipients.Count == 0)
            {
                logger.LogDebug("No staff opted in to mention notifications; skipping email for {MentionId}", mention.Id);
                return;
            }

            var siteTitle = await settingsService.GetStringAsync("title", ct) ?? "HowToSoftware";
            var siteUrl = (await settingsService.GetStringAsync("url", ct) ?? "https://howtoosoftware.com").TrimEnd('/');
            var fromAddress = ResolveFromAddress(siteUrl);

            var subject = $"[{siteTitle}] New webmention received";
            var html = BuildMentionNotificationHtml(mention, siteTitle, siteUrl);

            foreach (var recipient in recipients)
            {
                if (string.IsNullOrWhiteSpace(recipient.Email)) continue;

                try
                {
                    var result = await emailService.SendAsync(new EmailMessage
                    {
                        From = $"{siteTitle} <{fromAddress}>",
                        To = recipient.Email!,
                        Subject = subject,
                        Html = html,
                    }, ct);

                    if (!result.Success)
                    {
                        logger.LogWarning("Mailgun rejected mention notification to {Email}: {Error}",
                            recipient.Email, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to email mention notification to {Email}", recipient.Email);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to dispatch mention notifications for {MentionId}", mention.Id);
        }
    }

    private static string BuildMentionNotificationHtml(Mention mention, string siteTitle, string siteUrl)
    {
        // Encode every untrusted field — Source/Target/Title/Excerpt come
        // from a third-party page that is also rendered to admins.
        var source = WebUtility.HtmlEncode(mention.Source);
        var target = WebUtility.HtmlEncode(mention.Target);
        var title = WebUtility.HtmlEncode(mention.SourceTitle ?? mention.Source);
        var excerpt = WebUtility.HtmlEncode(mention.SourceExcerpt ?? string.Empty);
        var author = WebUtility.HtmlEncode(mention.SourceAuthor ?? string.Empty);
        var siteEnc = WebUtility.HtmlEncode(siteTitle);
        var verifiedLabel = mention.Verified ? "Verified" : "Unverified";
        var verifiedColor = mention.Verified ? "#16a34a" : "#9ca3af";
        var statusLabel = mention.Status switch
        {
            MentionStatus.Approved => "Approved",
            MentionStatus.Rejected => "Rejected",
            _ => "Pending review",
        };
        var reviewUrl = $"{siteUrl}/ghost/mentions";

        var excerptBlock = string.IsNullOrWhiteSpace(excerpt)
            ? string.Empty
            : $"<blockquote style=\"margin:12px 0;padding:8px 12px;border-left:3px solid #e5e7eb;color:#374151;\">{excerpt}</blockquote>";

        var authorBlock = string.IsNullOrWhiteSpace(author)
            ? string.Empty
            : $"<p style=\"margin:4px 0;color:#6b7280;font-size:14px;\">By {author}</p>";

        return $"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color:#111827;">
                <h2 style="margin:0 0 8px 0;">New webmention on {siteEnc}</h2>
                <p style="margin:0 0 16px 0;color:#6b7280;">
                    <span style="color:{verifiedColor};font-weight:600;">{verifiedLabel}</span>
                    · Status: <strong>{statusLabel}</strong>
                </p>
                <p style="margin:0 0 4px 0;"><strong>{title}</strong></p>
                {authorBlock}
                {excerptBlock}
                <p style="margin:12px 0 4px 0;font-size:14px;"><strong>Source:</strong> <a href="{source}">{source}</a></p>
                <p style="margin:0 0 16px 0;font-size:14px;"><strong>Target:</strong> <a href="{target}">{target}</a></p>
                <p>
                    <a href="{reviewUrl}" style="display: inline-block; padding: 10px 18px; background: #7c3aed; color: #fff; text-decoration: none; border-radius: 6px; font-weight: 600;">
                        Review in admin
                    </a>
                </p>
                <p style="margin-top:24px;color:#9ca3af;font-size:12px;">
                    You're receiving this because mention notifications are enabled on your staff account.
                </p>
            </div>
            """;
    }

    private string ResolveFromAddress(string siteUrl)
    {
        var defaultFrom = mailOptions.Value.DefaultFrom;
        if (!string.IsNullOrWhiteSpace(defaultFrom)) return defaultFrom;

        var mailgunDomain = mailOptions.Value.MailgunDomain;
        if (!string.IsNullOrWhiteSpace(mailgunDomain)) return $"noreply@{mailgunDomain}";

        return Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri)
            ? $"noreply@{uri.Host}"
            : "noreply@localhost";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
