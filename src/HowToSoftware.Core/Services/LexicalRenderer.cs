using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Services;

public sealed partial class LexicalRenderer : ILexicalRenderer
{
    [Flags]
    private enum TextFormat
    {
        None = 0,
        Bold = 1,
        Italic = 2,
        Strikethrough = 4,
        Underline = 8,
        Code = 16,
        Subscript = 32,
        Superscript = 64,
    }

    public string Render(string lexicalJson)
    {
        if (string.IsNullOrWhiteSpace(lexicalJson))
            return string.Empty;

        using var doc = JsonDocument.Parse(lexicalJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("root", out var rootNode))
            return string.Empty;

        var sb = new StringBuilder();
        RenderChildren(rootNode, sb);
        return sb.ToString();
    }

    private static void RenderChildren(JsonElement node, StringBuilder sb)
    {
        if (!node.TryGetProperty("children", out var children))
            return;

        foreach (var child in children.EnumerateArray())
        {
            RenderNode(child, sb);
        }
    }

    private static void RenderNode(JsonElement node, StringBuilder sb)
    {
        var type = node.GetProperty("type").GetString();

        switch (type)
        {
            case "paragraph":
                RenderParagraph(node, sb);
                break;
            case "heading":
            case "extended-heading":
                RenderHeading(node, sb);
                break;
            case "text":
            case "extended-text":
                RenderText(node, sb);
                break;
            case "link":
                RenderLink(node, sb);
                break;
            case "list":
                RenderList(node, sb);
                break;
            case "listitem":
                RenderListItem(node, sb);
                break;
            case "linebreak":
                sb.Append("<br>");
                break;
            case "horizontalrule":
                sb.Append("<hr>");
                break;
            case "image":
                RenderImage(node, sb);
                break;
            case "quote":
            case "extended-quote":
                RenderBlockquote(node, sb);
                break;
            case "aside":
                RenderAside(node, sb);
                break;
            case "codeblock":
            case "extended-codeblock":
                RenderCodeBlock(node, sb);
                break;
            case "bookmark":
                RenderBookmark(node, sb);
                break;
            case "embed":
                RenderEmbed(node, sb);
                break;
            case "callout":
                RenderCallout(node, sb);
                break;
            case "toggle":
                RenderToggle(node, sb);
                break;
            case "video":
                RenderVideo(node, sb);
                break;
            case "audio":
                RenderAudio(node, sb);
                break;
            case "file":
                RenderFile(node, sb);
                break;
            case "button":
                RenderButton(node, sb);
                break;
            case "gallery":
                RenderGallery(node, sb);
                break;
            case "html":
                RenderHtmlCard(node, sb);
                break;
            case "markdown":
                RenderMarkdownCard(node, sb);
                break;
            case "divider":
                sb.Append("<hr>");
                break;
            default:
                // Unknown node type — render children if present
                RenderChildren(node, sb);
                break;
        }
    }

    private static void RenderParagraph(JsonElement node, StringBuilder sb)
    {
        sb.Append("<p>");
        RenderChildren(node, sb);
        sb.Append("</p>");
    }

    private static void RenderHeading(JsonElement node, StringBuilder sb)
    {
        var tag = "h2";
        if (node.TryGetProperty("tag", out var tagProp))
        {
            var tagVal = tagProp.GetString();
            if (tagVal is "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
                tag = tagVal;
        }

        var id = GenerateHeadingId(node);
        sb.Append(CultureInfo.InvariantCulture, $"<{tag} id=\"{Encode(id)}\">");
        RenderChildren(node, sb);
        sb.Append(CultureInfo.InvariantCulture, $"</{tag}>");
    }

    private static void RenderText(JsonElement node, StringBuilder sb)
    {
        var text = node.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? "" : "";
        var format = node.TryGetProperty("format", out var formatProp) && formatProp.ValueKind == JsonValueKind.Number
            ? (TextFormat)formatProp.GetInt32()
            : TextFormat.None;

        var encoded = Encode(text);

        if (format == TextFormat.None)
        {
            sb.Append(encoded);
            return;
        }

        // Code format wraps everything else
        if (format.HasFlag(TextFormat.Code))
        {
            sb.Append("<code>");
            sb.Append(encoded);
            sb.Append("</code>");
            return;
        }

        if (format.HasFlag(TextFormat.Bold)) sb.Append("<strong>");
        if (format.HasFlag(TextFormat.Italic)) sb.Append("<em>");
        if (format.HasFlag(TextFormat.Strikethrough)) sb.Append("<s>");
        if (format.HasFlag(TextFormat.Underline)) sb.Append("<u>");
        if (format.HasFlag(TextFormat.Subscript)) sb.Append("<sub>");
        if (format.HasFlag(TextFormat.Superscript)) sb.Append("<sup>");

        sb.Append(encoded);

        if (format.HasFlag(TextFormat.Superscript)) sb.Append("</sup>");
        if (format.HasFlag(TextFormat.Subscript)) sb.Append("</sub>");
        if (format.HasFlag(TextFormat.Underline)) sb.Append("</u>");
        if (format.HasFlag(TextFormat.Strikethrough)) sb.Append("</s>");
        if (format.HasFlag(TextFormat.Italic)) sb.Append("</em>");
        if (format.HasFlag(TextFormat.Bold)) sb.Append("</strong>");
    }

    private static void RenderLink(JsonElement node, StringBuilder sb)
    {
        var url = node.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "";
        var rel = node.TryGetProperty("rel", out var relProp) && relProp.ValueKind != JsonValueKind.Null
            ? relProp.GetString() : null;
        var target = node.TryGetProperty("target", out var targetProp) && targetProp.ValueKind != JsonValueKind.Null
            ? targetProp.GetString() : null;
        var title = node.TryGetProperty("title", out var titleProp) && titleProp.ValueKind != JsonValueKind.Null
            ? titleProp.GetString() : null;

        sb.Append(CultureInfo.InvariantCulture, $"<a href=\"{Encode(url)}\"");
        if (!string.IsNullOrEmpty(rel)) sb.Append(CultureInfo.InvariantCulture, $" rel=\"{Encode(rel)}\"");
        if (!string.IsNullOrEmpty(target)) sb.Append(CultureInfo.InvariantCulture, $" target=\"{Encode(target)}\"");
        if (!string.IsNullOrEmpty(title)) sb.Append(CultureInfo.InvariantCulture, $" title=\"{Encode(title)}\"");
        sb.Append('>');
        RenderChildren(node, sb);
        sb.Append("</a>");
    }

    private static void RenderList(JsonElement node, StringBuilder sb)
    {
        var listType = node.TryGetProperty("listType", out var ltProp) ? ltProp.GetString() : "bullet";
        var tag = listType == "number" ? "ol" : "ul";

        if (tag == "ol" && node.TryGetProperty("start", out var startProp) && startProp.GetInt32() != 1)
        {
            sb.Append(CultureInfo.InvariantCulture, $"<ol start=\"{startProp.GetInt32()}\">");
        }
        else
        {
            sb.Append(CultureInfo.InvariantCulture, $"<{tag}>");
        }

        RenderChildren(node, sb);
        sb.Append(CultureInfo.InvariantCulture, $"</{tag}>");
    }

    private static void RenderListItem(JsonElement node, StringBuilder sb)
    {
        sb.Append("<li>");
        RenderChildren(node, sb);
        sb.Append("</li>");
    }

    private static void RenderImage(JsonElement node, StringBuilder sb)
    {
        var src = GetStringProp(node, "src");
        var alt = GetStringProp(node, "alt");
        var width = GetIntProp(node, "width");
        var height = GetIntProp(node, "height");
        var caption = GetStringProp(node, "caption");
        var cardWidth = GetStringProp(node, "cardWidth");
        var href = GetStringProp(node, "href");

        var widthClass = cardWidth switch
        {
            "wide" => " kg-width-wide",
            "full" => " kg-width-full",
            _ => "",
        };

        sb.Append(CultureInfo.InvariantCulture, $"<figure class=\"kg-card kg-image-card{widthClass}\">");

        if (!string.IsNullOrEmpty(href))
            sb.Append(CultureInfo.InvariantCulture, $"<a href=\"{Encode(href)}\">");

        sb.Append(CultureInfo.InvariantCulture, $"<img src=\"{Encode(src)}\" class=\"kg-image\" alt=\"{Encode(alt)}\" loading=\"lazy\"");
        if (width > 0) sb.Append(CultureInfo.InvariantCulture, $" width=\"{width}\"");
        if (height > 0) sb.Append(CultureInfo.InvariantCulture, $" height=\"{height}\"");
        sb.Append('>');

        if (!string.IsNullOrEmpty(href))
            sb.Append("</a>");

        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<figcaption>{caption}</figcaption>");

        sb.Append("</figure>");
    }

    private static void RenderBlockquote(JsonElement node, StringBuilder sb)
    {
        sb.Append("<blockquote>");
        RenderChildren(node, sb);
        sb.Append("</blockquote>");
    }

    private static void RenderAside(JsonElement node, StringBuilder sb)
    {
        sb.Append("<blockquote class=\"kg-blockquote-alt\">");
        RenderChildren(node, sb);
        sb.Append("</blockquote>");
    }

    private static void RenderCodeBlock(JsonElement node, StringBuilder sb)
    {
        var language = GetStringProp(node, "language");
        var code = GetStringProp(node, "code");
        var caption = GetStringProp(node, "caption");

        sb.Append("<pre>");
        if (!string.IsNullOrEmpty(language))
            sb.Append(CultureInfo.InvariantCulture, $"<code class=\"language-{Encode(language)}\">");
        else
            sb.Append("<code>");

        sb.Append(Encode(code));
        sb.Append("</code></pre>");

        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<figcaption>{caption}</figcaption>");
    }

    private static void RenderBookmark(JsonElement node, StringBuilder sb)
    {
        var url = GetStringProp(node, "url");
        var title = GetStringProp(node, "title");
        var description = GetStringProp(node, "description");
        var icon = GetStringProp(node, "icon");
        var author = GetStringProp(node, "author");
        var publisher = GetStringProp(node, "publisher");
        var thumbnail = GetStringProp(node, "thumbnail");
        var caption = GetStringProp(node, "caption");

        sb.Append("<figure class=\"kg-card kg-bookmark-card\">");
        sb.Append(CultureInfo.InvariantCulture, $"<a class=\"kg-bookmark-container\" href=\"{Encode(url)}\">");
        sb.Append("<div class=\"kg-bookmark-content\">");
        sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-bookmark-title\">{Encode(title)}</div>");
        sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-bookmark-description\">{Encode(description)}</div>");
        sb.Append("<div class=\"kg-bookmark-metadata\">");

        if (!string.IsNullOrEmpty(icon))
            sb.Append(CultureInfo.InvariantCulture, $"<img class=\"kg-bookmark-icon\" src=\"{Encode(icon)}\" alt=\"\">");

        if (!string.IsNullOrEmpty(author))
            sb.Append(CultureInfo.InvariantCulture, $"<span class=\"kg-bookmark-author\">{Encode(author)}</span>");

        if (!string.IsNullOrEmpty(publisher))
            sb.Append(CultureInfo.InvariantCulture, $"<span class=\"kg-bookmark-publisher\">{Encode(publisher)}</span>");

        sb.Append("</div></div>"); // metadata + content

        if (!string.IsNullOrEmpty(thumbnail))
            sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-bookmark-thumbnail\"><img src=\"{Encode(thumbnail)}\" alt=\"\"></div>");

        sb.Append("</a></figure>");

        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<figcaption>{caption}</figcaption>");
    }

    private static void RenderEmbed(JsonElement node, StringBuilder sb)
    {
        var html = GetStringProp(node, "html");
        var caption = GetStringProp(node, "caption");

        sb.Append("<figure class=\"kg-card kg-embed-card\">");
        sb.Append(html); // Embed HTML is passed through as-is (iframe, etc.)
        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<figcaption>{caption}</figcaption>");
        sb.Append("</figure>");
    }

    private static void RenderCallout(JsonElement node, StringBuilder sb)
    {
        var emoji = GetStringProp(node, "calloutEmoji");
        var color = GetStringProp(node, "backgroundColor");

        var colorClass = !string.IsNullOrEmpty(color) ? $" kg-callout-card-{Encode(color)}" : "";
        sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-card kg-callout-card{colorClass}\">");

        if (!string.IsNullOrEmpty(emoji))
            sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-callout-emoji\">{Encode(emoji)}</div>");

        sb.Append("<div class=\"kg-callout-text\">");
        RenderChildren(node, sb);
        sb.Append("</div></div>");
    }

    private static void RenderToggle(JsonElement node, StringBuilder sb)
    {
        var heading = GetStringProp(node, "heading");

        sb.Append("<div class=\"kg-card kg-toggle-card\" data-kg-toggle-state=\"close\">");
        sb.Append("<div class=\"kg-toggle-heading\">");
        sb.Append(CultureInfo.InvariantCulture, $"<h4 class=\"kg-toggle-heading-text\">{Encode(heading)}</h4>");
        sb.Append("<button class=\"kg-toggle-card-icon\">");
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"><path d=\"M18.71 8.21a1 1 0 0 0-1.42 0l-4.58 4.58a1 1 0 0 1-1.42 0L6.71 8.21a1 1 0 0 0-1.42 1.42l4.58 4.58a3 3 0 0 0 4.24 0l4.58-4.58a1 1 0 0 0 .02-1.42z\"/></svg>");
        sb.Append("</button></div>");
        sb.Append("<div class=\"kg-toggle-content\">");
        RenderChildren(node, sb);
        sb.Append("</div></div>");
    }

    private static void RenderVideo(JsonElement node, StringBuilder sb)
    {
        var src = GetStringProp(node, "src");
        var caption = GetStringProp(node, "caption");
        var customThumbnail = GetStringProp(node, "customThumbnailSrc");
        var width = GetIntProp(node, "width");
        var height = GetIntProp(node, "height");
        var cardWidth = GetStringProp(node, "cardWidth");

        var widthClass = cardWidth switch
        {
            "wide" => " kg-width-wide",
            "full" => " kg-width-full",
            _ => "",
        };

        sb.Append(CultureInfo.InvariantCulture, $"<figure class=\"kg-card kg-video-card{widthClass}\">");
        sb.Append("<div class=\"kg-video-container\">");
        sb.Append(CultureInfo.InvariantCulture, $"<video src=\"{Encode(src)}\" controls");
        if (!string.IsNullOrEmpty(customThumbnail))
            sb.Append(CultureInfo.InvariantCulture, $" poster=\"{Encode(customThumbnail)}\"");
        if (width > 0) sb.Append(CultureInfo.InvariantCulture, $" width=\"{width}\"");
        if (height > 0) sb.Append(CultureInfo.InvariantCulture, $" height=\"{height}\"");
        sb.Append("></video></div>");

        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<figcaption>{caption}</figcaption>");
        sb.Append("</figure>");
    }

    private static void RenderAudio(JsonElement node, StringBuilder sb)
    {
        var src = GetStringProp(node, "src");
        var title = GetStringProp(node, "title");
        var caption = GetStringProp(node, "caption");

        sb.Append("<figure class=\"kg-card kg-audio-card\">");
        sb.Append("<div class=\"kg-audio-player-container\">");

        if (!string.IsNullOrEmpty(title))
            sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-audio-title\">{Encode(title)}</div>");

        sb.Append(CultureInfo.InvariantCulture, $"<audio src=\"{Encode(src)}\" controls></audio>");
        sb.Append("</div>");

        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<figcaption>{caption}</figcaption>");
        sb.Append("</figure>");
    }

    private static void RenderFile(JsonElement node, StringBuilder sb)
    {
        var src = GetStringProp(node, "src");
        var fileName = GetStringProp(node, "fileName");
        var fileTitle = GetStringProp(node, "fileTitle");
        var caption = GetStringProp(node, "fileCaption");
        var fileSize = GetIntProp(node, "fileSize");

        sb.Append("<figure class=\"kg-card kg-file-card\">");
        sb.Append(CultureInfo.InvariantCulture, $"<a class=\"kg-file-card-container\" href=\"{Encode(src)}\">");
        sb.Append("<div class=\"kg-file-card-contents\">");
        sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-file-card-title\">{Encode(!string.IsNullOrEmpty(fileTitle) ? fileTitle : fileName)}</div>");

        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-file-card-caption\">{Encode(caption)}</div>");

        sb.Append("<div class=\"kg-file-card-metadata\">");
        sb.Append(CultureInfo.InvariantCulture, $"<span class=\"kg-file-card-filename\">{Encode(fileName)}</span>");
        if (fileSize > 0)
            sb.Append(CultureInfo.InvariantCulture, $"<span class=\"kg-file-card-filesize\">{FormatFileSize(fileSize)}</span>");
        sb.Append("</div></div>");

        sb.Append("<div class=\"kg-file-card-icon\"><svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\"><path d=\"M19 13h-6v6h-2v-6H5v-2h6V5h2v6h6v2z\"/></svg></div>");
        sb.Append("</a></figure>");
    }

    private static void RenderButton(JsonElement node, StringBuilder sb)
    {
        var url = GetStringProp(node, "url");
        var text = GetStringProp(node, "buttonText");
        var alignment = GetStringProp(node, "alignment");

        var alignClass = alignment == "center" ? " kg-align-center" : "";
        sb.Append(CultureInfo.InvariantCulture, $"<div class=\"kg-card kg-button-card{alignClass}\">");
        sb.Append(CultureInfo.InvariantCulture, $"<a href=\"{Encode(url)}\" class=\"kg-btn kg-btn-accent\">{Encode(text)}</a>");
        sb.Append("</div>");
    }

    private static void RenderGallery(JsonElement node, StringBuilder sb)
    {
        sb.Append("<figure class=\"kg-card kg-gallery-card kg-width-wide\">");
        sb.Append("<div class=\"kg-gallery-container\">");

        if (node.TryGetProperty("images", out var images))
        {
            sb.Append("<div class=\"kg-gallery-row\">");
            foreach (var img in images.EnumerateArray())
            {
                var src = GetStringProp(img, "src");
                var alt = GetStringProp(img, "alt");
                var width = GetIntProp(img, "width");
                var height = GetIntProp(img, "height");

                sb.Append("<div class=\"kg-gallery-image\">");
                sb.Append(CultureInfo.InvariantCulture, $"<img src=\"{Encode(src)}\" alt=\"{Encode(alt)}\" loading=\"lazy\"");
                if (width > 0) sb.Append(CultureInfo.InvariantCulture, $" width=\"{width}\"");
                if (height > 0) sb.Append(CultureInfo.InvariantCulture, $" height=\"{height}\"");
                sb.Append('>');
                sb.Append("</div>");
            }
            sb.Append("</div>");
        }

        var caption = GetStringProp(node, "caption");
        sb.Append("</div>");
        if (!string.IsNullOrEmpty(caption))
            sb.Append(CultureInfo.InvariantCulture, $"<figcaption>{caption}</figcaption>");
        sb.Append("</figure>");
    }

    private static void RenderHtmlCard(JsonElement node, StringBuilder sb)
    {
        var html = GetStringProp(node, "html");
        sb.Append("<!--kg-card-begin: html-->");
        sb.Append(html);
        sb.Append("<!--kg-card-end: html-->");
    }

    private static void RenderMarkdownCard(JsonElement node, StringBuilder sb)
    {
        // Markdown cards store pre-rendered HTML in the html property
        var html = GetStringProp(node, "html");
        sb.Append("<div class=\"kg-card kg-markdown-card\">");
        sb.Append(html);
        sb.Append("</div>");
    }

    // --- Helpers ---

    private static string GenerateHeadingId(JsonElement node)
    {
        var textContent = ExtractTextContent(node);
        return Slugify(textContent);
    }

    private static string ExtractTextContent(JsonElement node)
    {
        var sb = new StringBuilder();
        ExtractTextRecursive(node, sb);
        return sb.ToString();
    }

    private static void ExtractTextRecursive(JsonElement node, StringBuilder sb)
    {
        if (node.TryGetProperty("text", out var textProp))
            sb.Append(textProp.GetString());

        if (node.TryGetProperty("children", out var children))
        {
            foreach (var child in children.EnumerateArray())
                ExtractTextRecursive(child, sb);
        }
    }

    private static string Slugify(string text)
    {
        var slug = text.ToLowerInvariant().Trim();
        slug = SlugUnsafeChars().Replace(slug, "");
        slug = SlugWhitespace().Replace(slug, "-");
        slug = SlugMultipleDashes().Replace(slug, "-");
        return slug.Trim('-');
    }

    private static string GetStringProp(JsonElement node, string name) =>
        node.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? ""
            : "";

    private static int GetIntProp(JsonElement node, string name) =>
        node.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : 0;

    private static string Encode(string value) =>
        System.Net.WebUtility.HtmlEncode(value);

    private static string FormatFileSize(int bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1_048_576 => $"{bytes / 1024} KB",
        < 1_073_741_824 => $"{bytes / 1_048_576} MB",
        _ => $"{bytes / 1_073_741_824} GB",
    };

    [GeneratedRegex(@"[^\w\s\-\u0400-\u04FF\u0500-\u052F]")]
    private static partial Regex SlugUnsafeChars();

    [GeneratedRegex(@"\s+")]
    private static partial Regex SlugWhitespace();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex SlugMultipleDashes();
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
