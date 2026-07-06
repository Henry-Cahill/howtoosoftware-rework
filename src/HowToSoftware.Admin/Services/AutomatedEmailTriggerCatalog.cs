namespace HowToSoftware.Admin.Services;

/// <summary>
/// Maps known automated-email slugs to human-readable descriptions of
/// when each email fires. Used by the automated-emails admin UI to give
/// editors guidance about programmatic triggers.
/// </summary>
public static class AutomatedEmailTriggerCatalog
{
    private static readonly Dictionary<string, string> Descriptions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["welcome"] = "Sent when a new free member signs up.",
        ["welcome-paid"] = "Sent when a member starts a paid subscription.",
        ["signup"] = "Sent when a new free member signs up.",
        ["signup-paid"] = "Sent when a member starts a paid subscription.",
        ["subscription-canceled"] = "Sent when a member cancels their paid subscription.",
        ["subscription-cancelled"] = "Sent when a member cancels their paid subscription.",
        ["payment-failed"] = "Sent when a recurring payment fails.",
        ["trial-ending"] = "Sent shortly before a member's free trial ends.",
        ["password-reset"] = "Sent when a staff or member requests a password reset.",
        ["magic-link"] = "Sent when a member requests a sign-in link.",
    };

    /// <summary>
    /// Returns a human-readable description for the given slug, or
    /// <c>null</c> if the slug is not in the known catalog.
    /// </summary>
    public static string? GetDescription(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        return Descriptions.TryGetValue(slug.Trim(), out var description) ? description : null;
    }

    /// <summary>
    /// Returns either the known description for the slug or a generic
    /// fallback explaining the trigger is custom / programmatic.
    /// </summary>
    public static string GetDescriptionOrFallback(string? slug)
    {
        return GetDescription(slug)
            ?? "Custom trigger — fired programmatically using this slug.";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
