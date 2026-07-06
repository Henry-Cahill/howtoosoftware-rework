using Ganss.Xss;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Services;

public class HtmlContentSanitizer : IContentSanitizer
{
    private readonly Ganss.Xss.HtmlSanitizer _sanitizer;

    public HtmlContentSanitizer()
    {
        _sanitizer = new Ganss.Xss.HtmlSanitizer();

        // Ghost/Lexical content uses iframes for embeds (YouTube, Twitter, etc.)
        _sanitizer.AllowedTags.Add("iframe");

        // Allow figure/figcaption used by Ghost cards (images, embeds, bookmarks)
        _sanitizer.AllowedTags.Add("figure");
        _sanitizer.AllowedTags.Add("figcaption");

        // Allow common attributes needed for blog content
        _sanitizer.AllowedAttributes.Add("class");
        _sanitizer.AllowedAttributes.Add("id");
        _sanitizer.AllowedAttributes.Add("loading");
        _sanitizer.AllowedAttributes.Add("decoding");
        _sanitizer.AllowedAttributes.Add("srcset");
        _sanitizer.AllowedAttributes.Add("sizes");
        _sanitizer.AllowedAttributes.Add("width");
        _sanitizer.AllowedAttributes.Add("height");

        // iframe attributes for embeds
        _sanitizer.AllowedAttributes.Add("src");
        _sanitizer.AllowedAttributes.Add("frameborder");
        _sanitizer.AllowedAttributes.Add("allowfullscreen");
        _sanitizer.AllowedAttributes.Add("allow");
        _sanitizer.AllowedAttributes.Add("title");

        // data attributes used by Ghost cards
        _sanitizer.AllowedAttributes.Add("data-*");

        // Allow embedded content from trusted sources only
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");

        // Allow target="_blank" for links
        _sanitizer.AllowedAttributes.Add("target");
        _sanitizer.AllowedAttributes.Add("rel");
    }

    public string Sanitize(string html)
    {
        if (string.IsNullOrEmpty(html))
            return html;

        return _sanitizer.Sanitize(html);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
