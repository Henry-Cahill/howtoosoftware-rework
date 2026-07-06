using HowToSoftware.Core.Interfaces;
using HowToSoftware.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HowToSoftware.Infrastructure.Tests;

public class MailgunEmailServiceTests : IDisposable
{
    private readonly MailgunEmailService _sut;

    public MailgunEmailServiceTests()
    {
        var settings = Options.Create(new MailSettings
        {
            MailgunDomain = "mg.test.com",
            MailgunApiKey = "key-test123",
            SmtpHost = "smtp.mailgun.org",
            SmtpPort = 587,
            DefaultFrom = "noreply@test.com",
            BatchSize = 2,
            BatchDelayMs = 100,
            MaxRetries = 1,
            TimeoutSeconds = 10,
        });

        _sut = new MailgunEmailService(settings, NullLogger<MailgunEmailService>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    // ── Template Rendering ─────────────────────────────────────

    [Fact]
    public async Task RenderTemplateAsync_Newsletter_ReturnsHtmlWithPostTitle()
    {
        var model = new EmailTemplateModel
        {
            SiteTitle = "Test Site",
            SiteUrl = "https://test.com",
            PostTitle = "My Great Post",
            PostUrl = "https://test.com/my-great-post",
            HtmlBody = "<p>Hello world</p>",
            ShowHeaderTitle = true,
            ShowFeatureImage = false,
            ShowBadge = true,
            BackgroundColor = "light",
            TitleAlignment = "center",
            TitleFontCategory = "sans_serif",
            BodyFontCategory = "sans_serif",
        };

        var html = await _sut.RenderTemplateAsync("Newsletter", model);

        Assert.Contains("My Great Post", html);
        Assert.Contains("<p>Hello world</p>", html);
        Assert.Contains("Test Site", html);
        Assert.Contains("https://test.com", html);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithFeatureImage_IncludesImage()
    {
        var model = new EmailTemplateModel
        {
            PostTitle = "Image Post",
            PostUrl = "https://test.com/image-post",
            FeatureImage = "https://test.com/content/images/2026/03/hero.webp",
            HtmlBody = "<p>Content</p>",
            ShowFeatureImage = true,
            ShowHeaderTitle = false,
            ShowBadge = false,
        };

        var html = await _sut.RenderTemplateAsync("Newsletter", model);

        Assert.Contains("hero.webp", html);
        Assert.Contains("feature-image", html);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithoutFeatureImage_OmitsImageBlock()
    {
        var model = new EmailTemplateModel
        {
            PostTitle = "No Image",
            PostUrl = "https://test.com/no-image",
            HtmlBody = "<p>Content</p>",
            ShowFeatureImage = false,
            ShowHeaderTitle = false,
            ShowBadge = false,
        };

        var html = await _sut.RenderTemplateAsync("Newsletter", model);

        // The CSS class name appears in styles, but no <img> with that class should be rendered
        Assert.DoesNotContain("<img class=\"feature-image\"", html);
    }

    [Fact]
    public async Task RenderTemplateAsync_DarkBackground_UsesDarkColors()
    {
        var model = new EmailTemplateModel
        {
            PostTitle = "Dark Mode",
            PostUrl = "https://test.com/dark",
            HtmlBody = "<p>Content</p>",
            BackgroundColor = "dark",
            ShowHeaderTitle = false,
            ShowBadge = false,
        };

        var html = await _sut.RenderTemplateAsync("Newsletter", model);

        Assert.Contains("#15212A", html);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithUnsubscribeUrl_IncludesLink()
    {
        var model = new EmailTemplateModel
        {
            PostTitle = "Unsub Test",
            PostUrl = "https://test.com/unsub",
            HtmlBody = "<p>Content</p>",
            UnsubscribeUrl = "https://test.com/unsubscribe/%%uuid%%",
            ShowHeaderTitle = false,
            ShowBadge = false,
        };

        var html = await _sut.RenderTemplateAsync("Newsletter", model);

        Assert.Contains("Unsubscribe", html);
        Assert.Contains("%%uuid%%", html);
    }

    [Fact]
    public async Task RenderTemplateAsync_WithExcerpt_IncludesExcerpt()
    {
        var model = new EmailTemplateModel
        {
            PostTitle = "Excerpt Test",
            PostUrl = "https://test.com/excerpt",
            HtmlBody = "<p>Content</p>",
            ShowExcerpt = true,
            Excerpt = "This is a short summary",
            ShowHeaderTitle = false,
            ShowBadge = false,
        };

        var html = await _sut.RenderTemplateAsync("Newsletter", model);

        Assert.Contains("This is a short summary", html);
    }

    // ── SendAsync (SMTP failure — no real server) ──────────────

    [Fact]
    public async Task SendAsync_NoSmtpServer_ReturnsFailureResult()
    {
        var message = new EmailMessage
        {
            From = "sender@test.com",
            To = "recipient@test.com",
            Subject = "Test",
            Html = "<p>Hello</p>",
        };

        var result = await _sut.SendAsync(message);

        Assert.False(result.Success);
        Assert.Equal("recipient@test.com", result.RecipientEmail);
        Assert.NotNull(result.ErrorMessage);
    }

    // ── SendBatchAsync (SMTP failure — no real server) ─────────

    [Fact]
    public async Task SendBatchAsync_NoSmtpServer_ReturnsFailureForAllRecipients()
    {
        var request = new EmailBatchRequest
        {
            From = "sender@test.com",
            Subject = "Batch Test",
            Html = "<p>Hello %%name%%</p>",
            Recipients = new Dictionary<string, Dictionary<string, string>>
            {
                ["alice@test.com"] = new() { ["name"] = "Alice" },
                ["bob@test.com"] = new() { ["name"] = "Bob" },
                ["carol@test.com"] = new() { ["name"] = "Carol" },
            },
        };

        var results = await _sut.SendBatchAsync(request);

        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.False(r.Success));
        Assert.Contains(results, r => r.RecipientEmail == "alice@test.com");
        Assert.Contains(results, r => r.RecipientEmail == "bob@test.com");
        Assert.Contains(results, r => r.RecipientEmail == "carol@test.com");
    }

    // ── MailSettings defaults ──────────────────────────────────

    [Fact]
    public void MailSettings_Defaults_AreReasonable()
    {
        var settings = new MailSettings();

        Assert.Equal("smtp.mailgun.org", settings.SmtpHost);
        Assert.Equal(587, settings.SmtpPort);
        Assert.Equal(500, settings.BatchSize);
        Assert.Equal(1000, settings.BatchDelayMs);
        Assert.Equal(2, settings.MaxRetries);
        Assert.Equal(30, settings.TimeoutSeconds);
        Assert.Equal(string.Empty, settings.MailgunDomain);
        Assert.Equal(string.Empty, settings.MailgunApiKey);
        Assert.Equal(string.Empty, settings.DefaultFrom);
    }

    // ── EmailTemplateModel defaults ────────────────────────────

    [Fact]
    public void EmailTemplateModel_Defaults_MatchNewsletterDefaults()
    {
        var model = new EmailTemplateModel();

        Assert.True(model.ShowBadge);
        Assert.True(model.ShowHeaderIcon);
        Assert.True(model.ShowHeaderTitle);
        Assert.True(model.ShowFeatureImage);
        Assert.False(model.ShowExcerpt);
        Assert.Equal("light", model.BackgroundColor);
        Assert.Equal("sans_serif", model.TitleFontCategory);
        Assert.Equal("center", model.TitleAlignment);
        Assert.Equal("sans_serif", model.BodyFontCategory);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
