using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Pages;

public class RssModel(IPostRepository postRepository, ISettingsService settings) : PageModel
{
    private const int FeedSize = 15;

    public async Task<IActionResult> OnGetAsync()
    {
        var siteTitle = await settings.GetStringAsync("title") ?? "howtosoftware";
        var siteDescription = await settings.GetStringAsync("description") ?? "";

        var siteUrl = $"{Request.Scheme}://{Request.Host}";
        var feedUrl = $"{siteUrl}/rss/";

        var result = await postRepository.GetPublishedPostsAsync(1, FeedSize);

        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
            Async = true,
        });

        await writer.WriteStartDocumentAsync();

        writer.WriteStartElement("rss");
        writer.WriteAttributeString("version", "2.0");
        writer.WriteAttributeString("xmlns", "dc", null, "http://purl.org/dc/elements/1.1/");
        writer.WriteAttributeString("xmlns", "content", null, "http://purl.org/rss/1.0/modules/content/");
        writer.WriteAttributeString("xmlns", "atom", null, "http://www.w3.org/2005/Atom");
        writer.WriteAttributeString("xmlns", "media", null, "http://search.yahoo.com/mrss/");

        writer.WriteStartElement("channel");

        writer.WriteElementString("title", siteTitle);
        writer.WriteElementString("description", siteDescription);
        writer.WriteElementString("link", siteUrl + "/");
        writer.WriteElementString("generator", "HowToSoftware .NET");
        if (result.Items.Count > 0 && result.Items[0].PublishedAt.HasValue)
        {
            writer.WriteElementString("lastBuildDate",
                result.Items[0].PublishedAt!.Value.ToUniversalTime().ToString("R"));
        }

        // atom:link self reference
        writer.WriteStartElement("atom", "link", "http://www.w3.org/2005/Atom");
        writer.WriteAttributeString("href", feedUrl);
        writer.WriteAttributeString("rel", "self");
        writer.WriteAttributeString("type", "application/rss+xml");
        writer.WriteEndElement();

        foreach (var post in result.Items)
        {
            writer.WriteStartElement("item");

            writer.WriteElementString("title", post.Title);
            writer.WriteElementString("link", $"{siteUrl}/{post.Slug}/");
            writer.WriteElementString("guid", $"{siteUrl}/{post.Slug}/");

            // Primary author
            var primaryAuthor = post.PostsAuthors
                .OrderBy(pa => pa.SortOrder)
                .Select(pa => pa.Author)
                .FirstOrDefault();
            if (primaryAuthor is not null)
            {
                writer.WriteElementString("dc", "creator", "http://purl.org/dc/elements/1.1/",
                    primaryAuthor.Name);
            }

            if (post.PublishedAt.HasValue)
            {
                writer.WriteElementString("pubDate",
                    post.PublishedAt.Value.ToUniversalTime().ToString("R"));
            }

            // Tags as categories
            foreach (var pt in post.PostsTags.OrderBy(pt => pt.SortOrder))
            {
                if (pt.Tag.Visibility == "public")
                    writer.WriteElementString("category", pt.Tag.Name);
            }

            // Feature image as media:content
            if (!string.IsNullOrEmpty(post.FeatureImage))
            {
                var imageUrl = post.FeatureImage.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? post.FeatureImage
                    : $"{siteUrl}{post.FeatureImage}";

                writer.WriteStartElement("media", "content", "http://search.yahoo.com/mrss/");
                writer.WriteAttributeString("url", imageUrl);
                writer.WriteAttributeString("medium", "image");
                writer.WriteEndElement();
            }

            // Description (excerpt)
            var excerpt = post.CustomExcerpt ?? TruncatePlaintext(post.Plaintext, 500);
            if (!string.IsNullOrEmpty(excerpt))
            {
                writer.WriteStartElement("description");
                writer.WriteCData(excerpt);
                writer.WriteEndElement();
            }

            // Full content
            if (!string.IsNullOrEmpty(post.Html))
            {
                writer.WriteStartElement("content", "encoded",
                    "http://purl.org/rss/1.0/modules/content/");
                writer.WriteCData(post.Html);
                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // item
        }

        writer.WriteEndElement(); // channel
        writer.WriteEndElement(); // rss
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();

        return Content(sb.ToString(), "application/rss+xml; charset=utf-8");
    }

    private static string? TruncatePlaintext(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (text.Length <= maxLength) return text;
        return string.Concat(text.AsSpan(0, maxLength).TrimEnd(), "\u2026");
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
