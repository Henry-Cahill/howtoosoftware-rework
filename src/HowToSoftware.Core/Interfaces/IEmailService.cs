using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Renders an email template with the given model and returns the HTML string.
    /// </summary>
    Task<string> RenderTemplateAsync(string templateName, EmailTemplateModel model, CancellationToken ct = default);

    /// <summary>
    /// Sends a single email via Mailgun SMTP relay.
    /// </summary>
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default);

    /// <summary>
    /// Sends emails to a batch of recipients. Handles chunking and rate limiting internally.
    /// Returns one result per recipient.
    /// </summary>
    Task<List<EmailSendResult>> SendBatchAsync(EmailBatchRequest request, CancellationToken ct = default);
}

public record EmailMessage
{
    public required string From { get; init; }
    public string? ReplyTo { get; init; }
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string Html { get; init; }
    public string? Plaintext { get; init; }
}

public record EmailBatchRequest
{
    public required string From { get; init; }
    public string? ReplyTo { get; init; }
    public required string Subject { get; init; }
    public required string Html { get; init; }
    public string? Plaintext { get; init; }

    /// <summary>
    /// Per-recipient variable substitutions. Key = email address,
    /// Value = dictionary of placeholder → replacement pairs.
    /// The service replaces <c>%%key%%</c> tokens in the HTML/plaintext for each recipient.
    /// </summary>
    public required Dictionary<string, Dictionary<string, string>> Recipients { get; init; }
}

public record EmailSendResult
{
    public required string RecipientEmail { get; init; }
    public bool Success { get; init; }
    public string? ProviderId { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ErrorCode { get; init; }
}

public record EmailTemplateModel
{
    // Site settings
    public string? SiteTitle { get; init; }
    public string? SiteUrl { get; init; }
    public string? SiteIconUrl { get; init; }
    public string? AccentColor { get; init; }

    // Post content
    public string? PostTitle { get; init; }
    public string? PostUrl { get; init; }
    public string? FeatureImage { get; init; }
    public string? HtmlBody { get; init; }
    public string? AuthorName { get; init; }
    public string? Excerpt { get; init; }
    public DateTime? PublishedAt { get; init; }

    // Newsletter design — visibility toggles
    public string? HeaderImage { get; init; }
    public bool ShowBadge { get; init; } = true;
    public bool ShowHeaderIcon { get; init; } = true;
    public bool ShowHeaderTitle { get; init; } = true;
    public bool ShowHeaderName { get; init; } = true;
    public bool ShowFeatureImage { get; init; } = true;
    public bool ShowExcerpt { get; init; }
    public bool ShowPostTitleSection { get; init; } = true;
    public bool ShowCommentCta { get; init; } = true;
    public bool FeedbackEnabled { get; init; }

    // Newsletter design — typography & colors
    public string BackgroundColor { get; init; } = "light";
    public string TitleFontCategory { get; init; } = "sans_serif";
    public string TitleAlignment { get; init; } = "center";
    public string TitleFontWeight { get; init; } = "bold";
    public string? PostTitleColor { get; init; }
    public string BodyFontCategory { get; init; } = "sans_serif";
    public string HeaderBackgroundColor { get; init; } = "transparent";
    public string? DividerColor { get; init; }

    // Newsletter design — buttons & links
    public string ButtonCorners { get; init; } = "rounded";
    public string ButtonStyle { get; init; } = "fill";
    public string? ButtonColor { get; init; } = "accent";
    public string LinkStyle { get; init; } = "underline";
    public string? LinkColor { get; init; } = "accent";
    public string ImageCorners { get; init; } = "square";

    // Footer
    public string? FooterContent { get; init; }
    public string? UnsubscribeUrl { get; init; }
    public string? ManageSubscriptionUrl { get; init; }
    public string? CommentUrl { get; init; }

    // Per-recipient placeholders (%%token%% replaced by batch sender)
    public string? FeedbackPositiveUrl { get; init; }
    public string? FeedbackNegativeUrl { get; init; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
