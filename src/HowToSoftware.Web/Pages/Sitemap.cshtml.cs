using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Pages;

public class SitemapModel(
    IPostRepository postRepository,
    ITagRepository tagRepository,
    IUserRepository userRepository) : PageModel
{
    private const string SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public async Task<IActionResult> OnGetAsync()
    {
        var siteUrl = $"{Request.Scheme}://{Request.Host}";

        // Fetch all published posts and pages (large page size to get all)
        var posts = await postRepository.GetAllAsync("published", "post", 1, 10000);
        var pages = await postRepository.GetAllAsync("published", "page", 1, 10000);
        var tags = await tagRepository.GetPublicTagsAsync();
        var authors = await userRepository.GetActiveAuthorsAsync();

        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
            Async = true,
        });

        await writer.WriteStartDocumentAsync();
        writer.WriteStartElement("urlset", SitemapNs);

        // Homepage
        WriteUrl(writer, $"{siteUrl}/", DateTime.UtcNow, "daily", "1.0");

        // Posts
        foreach (var post in posts.Items)
        {
            var lastmod = post.UpdatedAt ?? post.PublishedAt ?? post.CreatedAt;
            WriteUrl(writer, $"{siteUrl}/{post.Slug}/", lastmod, "weekly", "0.8");
        }

        // Pages
        foreach (var page in pages.Items)
        {
            var lastmod = page.UpdatedAt ?? page.PublishedAt ?? page.CreatedAt;
            WriteUrl(writer, $"{siteUrl}/{page.Slug}/", lastmod, "monthly", "0.8");
        }

        // Tags
        foreach (var tag in tags)
        {
            WriteUrl(writer, $"{siteUrl}/tag/{tag.Slug}/", tag.UpdatedAt ?? tag.CreatedAt, "weekly", "0.5");
        }

        // Authors
        foreach (var author in authors)
        {
            WriteUrl(writer, $"{siteUrl}/author/{author.Slug}/", author.UpdatedAt ?? author.CreatedAt, "weekly", "0.5");
        }

        writer.WriteEndElement(); // urlset
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();

        return Content(sb.ToString(), "application/xml; charset=utf-8");
    }

    private static void WriteUrl(XmlWriter writer, string loc, DateTime? lastmod, string changefreq, string priority)
    {
        writer.WriteStartElement("url");
        writer.WriteElementString("loc", loc);
        if (lastmod.HasValue)
        {
            writer.WriteElementString("lastmod", lastmod.Value.ToUniversalTime().ToString("yyyy-MM-dd"));
        }
        writer.WriteElementString("changefreq", changefreq);
        writer.WriteElementString("priority", priority);
        writer.WriteEndElement();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
