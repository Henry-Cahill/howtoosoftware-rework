using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace HowToSoftware.Web.Tests;

public class EmailTrackingControllerTests
{
    private readonly FakeNewsletterService _newsletterService = new();
    private readonly EmailTrackingController _sut;

    public EmailTrackingControllerTests()
    {
        _sut = new EmailTrackingController(_newsletterService);
    }

    [Fact]
    public async Task TrackOpen_ReturnsTransparentGif()
    {
        var result = await _sut.TrackOpen("recipient-1", CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/gif", fileResult.ContentType);
        Assert.True(fileResult.FileContents.Length > 0);
        // GIF89a magic bytes
        Assert.Equal((byte)'G', fileResult.FileContents[0]);
        Assert.Equal((byte)'I', fileResult.FileContents[1]);
        Assert.Equal((byte)'F', fileResult.FileContents[2]);
    }

    [Fact]
    public async Task TrackOpen_CallsRecordOpen()
    {
        await _sut.TrackOpen("recipient-42", CancellationToken.None);

        Assert.Single(_newsletterService.RecordedOpens);
        Assert.Equal("recipient-42", _newsletterService.RecordedOpens[0]);
    }

    [Fact]
    public async Task TrackOpen_ReturnsGifEvenIfRecipientUnknown()
    {
        // RecordOpenAsync is fire-and-forget for unknown IDs; pixel should still return
        var result = await _sut.TrackOpen("nonexistent", CancellationToken.None);

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/gif", fileResult.ContentType);
    }

    [Fact]
    public async Task TrackClick_RedirectsToDestinationUrl()
    {
        _newsletterService.RedirectDestinations["redirect-1"] = "https://example.com/article";

        var result = await _sut.TrackClick("redirect-1", "member-1", CancellationToken.None);

        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://example.com/article", redirectResult.Url);
    }

    [Fact]
    public async Task TrackClick_RecordsClickEvent()
    {
        _newsletterService.RedirectDestinations["redirect-1"] = "https://example.com/article";

        await _sut.TrackClick("redirect-1", "member-42", CancellationToken.None);

        Assert.Single(_newsletterService.RecordedClicks);
        Assert.Equal(("redirect-1", "member-42"), _newsletterService.RecordedClicks[0]);
    }

    [Fact]
    public async Task TrackClick_ReturnsNotFoundForUnknownRedirect()
    {
        // No redirect registered → InvalidOperationException → 404
        var result = await _sut.TrackClick("nonexistent", "member-1", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RecordFeedback_ReturnsHtmlThankYouPage()
    {
        var result = await _sut.RecordFeedback("email-1", "member-1", 1, CancellationToken.None);

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Equal("text/html", contentResult.ContentType);
        Assert.Contains("Thanks for your feedback", contentResult.Content);
    }

    [Fact]
    public async Task RecordFeedback_RecordsFeedbackEvent()
    {
        await _sut.RecordFeedback("email-1", "member-42", 0, CancellationToken.None);

        Assert.Single(_newsletterService.RecordedFeedbacks);
        Assert.Equal(("email-1", "member-42", 0), _newsletterService.RecordedFeedbacks[0]);
    }

    [Fact]
    public async Task RecordFeedback_ReturnsBadRequestForInvalidScore()
    {
        var result = await _sut.RecordFeedback("email-1", "member-1", 5, CancellationToken.None);

        Assert.IsType<BadRequestResult>(result);
        Assert.Empty(_newsletterService.RecordedFeedbacks);
    }

    [Fact]
    public async Task RecordFeedback_ReturnsNotFoundForUnknownEmail()
    {
        _newsletterService.FailFeedbackForEmails.Add("unknown-email");

        var result = await _sut.RecordFeedback("unknown-email", "member-1", 1, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    // ── Fake ────────────────────────────────────────────────────

    private class FakeNewsletterService : INewsletterService
    {
        public List<string> RecordedOpens { get; } = [];
        public List<(string RedirectId, string MemberId)> RecordedClicks { get; } = [];
        public List<(string EmailId, string MemberId, int Score)> RecordedFeedbacks { get; } = [];
        public Dictionary<string, string> RedirectDestinations { get; } = new();
        public HashSet<string> FailFeedbackForEmails { get; } = [];

        public Task RecordOpenAsync(string emailRecipientId, CancellationToken ct = default)
        {
            RecordedOpens.Add(emailRecipientId);
            return Task.CompletedTask;
        }

        public Task<string> RecordClickAsync(string redirectId, string memberId, CancellationToken ct = default)
        {
            RecordedClicks.Add((redirectId, memberId));
            if (RedirectDestinations.TryGetValue(redirectId, out var url))
                return Task.FromResult(url);
            throw new InvalidOperationException($"Redirect '{redirectId}' not found.");
        }

        public Task RecordFeedbackAsync(string emailId, string memberId, int score, CancellationToken ct = default)
        {
            if (FailFeedbackForEmails.Contains(emailId))
                throw new InvalidOperationException($"Email '{emailId}' not found.");
            RecordedFeedbacks.Add((emailId, memberId, score));
            return Task.CompletedTask;
        }

        public Task<Email> SendPostAsNewsletterAsync(SendNewsletterRequest request, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task ProcessPendingEmailAsync(string emailId, string siteUrl, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<Email> SendAbTestWinnerAsync(string emailId, string siteUrl, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task SubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UnsubscribeAsync(string memberId, string newsletterId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<string>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default)
            => throw new NotImplementedException();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
