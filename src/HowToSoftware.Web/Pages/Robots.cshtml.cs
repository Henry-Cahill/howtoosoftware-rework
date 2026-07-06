using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HowToSoftware.Web.Pages;

public class RobotsModel : PageModel
{
    public IActionResult OnGet()
    {
        var siteUrl = $"{Request.Scheme}://{Request.Host}";

        var content = $"""
            User-agent: *
            Allow: /

            Sitemap: {siteUrl}/sitemap.xml
            """;

        return Content(content, "text/plain; charset=utf-8");
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
