using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Services;

namespace HowToSoftware.Core.Tests;

public class NewsletterServiceTests
{
    private readonly FakePostRepository _postRepo = new();
    private readonly FakeNewsletterRepository _newsletterRepo = new();
    private readonly FakeMemberRepository _memberRepo = new();
    private readonly FakeEmailRepository _emailRepo = new();
    private readonly FakeEmailService _emailService = new();
    private readonly FakeSettingsService _settingsService = new();
    private readonly NewsletterService _sut;

    private const string SiteUrl = "https://howtoosoftware.com";

    public NewsletterServiceTests()
    {
        _sut = new NewsletterService(
            _postRepo, _newsletterRepo, _memberRepo,
            _emailRepo, _emailService, _settingsService);
    }

    // ── SendPostAsNewsletterAsync ────────────────────────────────

    [Fact]
    public async Task SendPostAsNewsletterAsync_CreatesEmailBatchAndRecipients()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.NotNull(email);
        Assert.Equal("submitted", email.Status);
        Assert.Equal(2, email.EmailCount);
        Assert.Equal(2, email.DeliveredCount);
        Assert.Equal(0, email.FailedCount);
        Assert.True(email.TrackOpens);
        Assert.Equal(post.Id, email.PostId);
        Assert.Equal(newsletter.Id, email.NewsletterId);

        // Verify batch was created
        Assert.Single(_emailRepo.Batches);
        Assert.Equal("submitted", _emailRepo.Batches[0].Status);

        // Verify recipients were created
        Assert.Equal(2, _emailRepo.Recipients.Count);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_SetsSubjectFromMetaIfAvailable()
    {
        var (post, newsletter, _) = SetupStandardScenario();
        post.Meta = new PostMeta
        {
            Id = "meta1",
            PostId = post.Id,
            EmailSubject = "Custom Subject Line",
        };

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.Equal("Custom Subject Line", email.Subject);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_FallsBackToTitleForSubject()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.Equal(post.Title, email.Subject);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_ThrowsWhenPostNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
            {
                PostId = "nonexistent",
                NewsletterId = "nl1",
                SiteUrl = SiteUrl,
            }));
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_ThrowsWhenNoHtml()
    {
        var post = CreatePost(html: null);
        _postRepo.Posts.Add(post);
        var newsletter = CreateNewsletter();
        _newsletterRepo.Newsletters.Add(newsletter);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
            {
                PostId = post.Id,
                NewsletterId = newsletter.Id,
                SiteUrl = SiteUrl,
            }));
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_ThrowsWhenNewsletterNotFound()
    {
        var post = CreatePost();
        _postRepo.Posts.Add(post);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
            {
                PostId = post.Id,
                NewsletterId = "nonexistent",
                SiteUrl = SiteUrl,
            }));
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_ThrowsWhenNewsletterInactive()
    {
        var post = CreatePost();
        _postRepo.Posts.Add(post);
        var newsletter = CreateNewsletter(status: "archived");
        _newsletterRepo.Newsletters.Add(newsletter);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
            {
                PostId = post.Id,
                NewsletterId = newsletter.Id,
                SiteUrl = SiteUrl,
            }));
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_ExcludesSuppressedEmails()
    {
        var (post, newsletter, members) = SetupStandardScenario();

        // Suppress the first member's email
        _emailRepo.SuppressedEmails.Add(members[0].Email);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.Equal(1, email.EmailCount);
        Assert.Single(_emailRepo.Recipients);
        Assert.Equal(members[1].Email, _emailRepo.Recipients[0].MemberEmail);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_ThrowsWhenNoEligibleRecipients()
    {
        var post = CreatePost();
        _postRepo.Posts.Add(post);
        var newsletter = CreateNewsletter();
        _newsletterRepo.Newsletters.Add(newsletter);
        // No members subscribed

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
            {
                PostId = post.Id,
                NewsletterId = newsletter.Id,
                SiteUrl = SiteUrl,
            }));
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_FiltersRecipientsByStatus()
    {
        var (post, newsletter, members) = SetupStandardScenario();
        members[0].Status = "free";
        members[1].Status = "paid";

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            RecipientFilter = "paid",
            SiteUrl = SiteUrl,
        });

        Assert.Equal(1, email.EmailCount);
        Assert.Single(_emailRepo.Recipients);
        Assert.Equal(members[1].Email, _emailRepo.Recipients[0].MemberEmail);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_WrapsLinksForClickTracking()
    {
        var post = CreatePost(html: "<p>Visit <a href=\"https://example.com\">here</a></p>");
        _postRepo.Posts.Add(post);
        var (newsletter, members) = SetupNewsletterWithMembers(1);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        // A redirect record should have been created
        Assert.Single(_emailRepo.Redirects);
        Assert.Equal("https://example.com", _emailRepo.Redirects[0].To);
        Assert.True(email.TrackClicks);

        // The email HTML should contain the tracking URL
        Assert.Contains("/api/email/click/", email.Html!);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_SkipsMailtoAndAnchorLinks()
    {
        var post = CreatePost(html: "<p><a href=\"mailto:test@test.com\">Email</a> <a href=\"#section\">Jump</a> <a href=\"tel:+1234567890\">Call</a></p>");
        _postRepo.Posts.Add(post);
        var (newsletter, _) = SetupNewsletterWithMembers(1);

        await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        // No redirect records should be created for mailto/anchor/tel links
        Assert.Empty(_emailRepo.Redirects);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_DeduplicatesRedirectsForSameUrl()
    {
        var post = CreatePost(html: "<a href=\"https://example.com\">One</a> <a href=\"https://example.com\">Two</a>");
        _postRepo.Posts.Add(post);
        var (newsletter, _) = SetupNewsletterWithMembers(1);

        await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        // Same URL should produce only one Redirect record
        Assert.Single(_emailRepo.Redirects);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_SendsBatchWithSubstitutions()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        // Verify the email service received a batch request with substitutions
        Assert.Single(_emailService.BatchRequests);
        var batchReq = _emailService.BatchRequests[0];
        Assert.Equal(2, batchReq.Recipients.Count);

        // Each recipient should have tracking pixel and unsubscribe URL substitutions
        foreach (var (_, vars) in batchReq.Recipients)
        {
            Assert.Contains("tracking_pixel", vars.Keys);
            Assert.Contains("unsubscribe_url", vars.Keys);
            Assert.Contains("member_id", vars.Keys);
            Assert.Contains("/api/email/open/", vars["tracking_pixel"]);
        }
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_HandlesPartialFailure()
    {
        var (post, newsletter, members) = SetupStandardScenario();

        // Make the email service fail for the first member
        _emailService.FailForEmails.Add(members[0].Email);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.Equal("submitted", email.Status);
        Assert.Equal(1, email.DeliveredCount);
        Assert.Equal(1, email.FailedCount);

        var failedRecipient = _emailRepo.Recipients.First(r => r.MemberEmail == members[0].Email);
        Assert.NotNull(failedRecipient.FailedAt);
        Assert.Single(failedRecipient.Failures);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_FullFailureSetsFailed()
    {
        var (post, newsletter, members) = SetupStandardScenario();

        // Fail all
        foreach (var m in members)
            _emailService.FailForEmails.Add(m.Email);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.Equal("failed", email.Status);
        Assert.Equal(0, email.DeliveredCount);
        Assert.Equal(2, email.FailedCount);
        Assert.Equal("failed", _emailRepo.Batches[0].Status);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_UsesSenderNameFromNewsletter()
    {
        var (post, newsletter, _) = SetupStandardScenario();
        newsletter.SenderName = "Custom Sender";
        newsletter.SenderEmail = "custom@example.com";

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.Equal("Custom Sender <custom@example.com>", email.From);
    }

    // ── Template Model Building ──────────────────────────────────

    [Fact]
    public async Task SendPostAsNewsletterAsync_PopulatesSiteSettingsInTemplateModel()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        var model = _emailService.LastTemplateModel;
        Assert.NotNull(model);
        Assert.Equal("newsletter", _emailService.LastTemplateName);
        Assert.Equal("HowToSoftware", model.SiteTitle);
        Assert.Equal("https://howtoosoftware.com/content/images/icon.png", model.SiteIconUrl);
        Assert.Equal("#ff6600", model.AccentColor);
        Assert.Equal(SiteUrl, model.SiteUrl);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_PopulatesPostFieldsInTemplateModel()
    {
        var post = CreatePost(html: "<p>Hello world</p>", title: "My Great Post");
        post.FeatureImage = "https://example.com/image.jpg";
        post.CustomExcerpt = "A great post excerpt";
        post.PublishedAt = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        _postRepo.Posts.Add(post);
        var (newsletter, _) = SetupNewsletterWithMembers(1);

        await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        var model = _emailService.LastTemplateModel!;
        Assert.Equal("My Great Post", model.PostTitle);
        Assert.Equal($"{SiteUrl}/{post.Slug}/", model.PostUrl);
        Assert.Equal("https://example.com/image.jpg", model.FeatureImage);
        Assert.Contains("Hello world", model.HtmlBody);
        Assert.Equal("A great post excerpt", model.Excerpt);
        Assert.Equal(new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc), model.PublishedAt);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_MapsNewsletterDesignProperties()
    {
        var post = CreatePost();
        _postRepo.Posts.Add(post);
        var (newsletter, _) = SetupNewsletterWithMembers(1);

        // Customize newsletter design
        newsletter.BackgroundColor = "dark";
        newsletter.TitleFontCategory = "serif";
        newsletter.TitleAlignment = "left";
        newsletter.TitleFontWeight = "normal";
        newsletter.PostTitleColor = "#ff0000";
        newsletter.BodyFontCategory = "serif";
        newsletter.HeaderBackgroundColor = "#333333";
        newsletter.DividerColor = "#555555";
        newsletter.ButtonCorners = "pill";
        newsletter.ButtonStyle = "outline";
        newsletter.ButtonColor = "#00ff00";
        newsletter.LinkStyle = "none";
        newsletter.LinkColor = "#0000ff";
        newsletter.ImageCorners = "rounded";
        newsletter.HeaderImage = "https://example.com/header.jpg";
        newsletter.ShowBadge = false;
        newsletter.ShowHeaderIcon = false;
        newsletter.ShowHeaderTitle = false;
        newsletter.ShowHeaderName = false;
        newsletter.ShowFeatureImage = false;
        newsletter.ShowExcerpt = true;
        newsletter.ShowPostTitleSection = false;
        newsletter.ShowCommentCta = false;
        newsletter.FeedbackEnabled = true;
        newsletter.FooterContent = "<p>Custom footer</p>";

        await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        var model = _emailService.LastTemplateModel!;
        Assert.Equal("dark", model.BackgroundColor);
        Assert.Equal("serif", model.TitleFontCategory);
        Assert.Equal("left", model.TitleAlignment);
        Assert.Equal("normal", model.TitleFontWeight);
        Assert.Equal("#ff0000", model.PostTitleColor);
        Assert.Equal("serif", model.BodyFontCategory);
        Assert.Equal("#333333", model.HeaderBackgroundColor);
        Assert.Equal("#555555", model.DividerColor);
        Assert.Equal("pill", model.ButtonCorners);
        Assert.Equal("outline", model.ButtonStyle);
        Assert.Equal("#00ff00", model.ButtonColor);
        Assert.Equal("none", model.LinkStyle);
        Assert.Equal("#0000ff", model.LinkColor);
        Assert.Equal("rounded", model.ImageCorners);
        Assert.Equal("https://example.com/header.jpg", model.HeaderImage);
        Assert.False(model.ShowBadge);
        Assert.False(model.ShowHeaderIcon);
        Assert.False(model.ShowHeaderTitle);
        Assert.False(model.ShowHeaderName);
        Assert.False(model.ShowFeatureImage);
        Assert.True(model.ShowExcerpt);
        Assert.False(model.ShowPostTitleSection);
        Assert.False(model.ShowCommentCta);
        Assert.True(model.FeedbackEnabled);
        Assert.Equal("<p>Custom footer</p>", model.FooterContent);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_SetsFooterLinks()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        var model = _emailService.LastTemplateModel!;
        Assert.Equal("%%unsubscribe_url%%", model.UnsubscribeUrl);
        Assert.Equal($"{SiteUrl}/#/portal/account", model.ManageSubscriptionUrl);
        Assert.Equal($"{SiteUrl}/{post.Slug}/#comments", model.CommentUrl);
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_IncludesFeedbackUrlsInSubstitutions()
    {
        var (post, newsletter, _) = SetupStandardScenario();
        newsletter.FeedbackEnabled = true;

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        var batchReq = _emailService.BatchRequests[0];
        foreach (var (_, vars) in batchReq.Recipients)
        {
            Assert.Contains("feedback_positive_url", vars.Keys);
            Assert.Contains("feedback_negative_url", vars.Keys);
            Assert.Contains($"/api/email/feedback/{email.Id}/", vars["feedback_positive_url"]);
            Assert.EndsWith("/1", vars["feedback_positive_url"]);
            Assert.EndsWith("/0", vars["feedback_negative_url"]);
        }
    }

    // ── RecordOpenAsync ─────────────────────────────────────────

    [Fact]
    public async Task RecordOpenAsync_SetsOpenedAtOnFirstOpen()
    {
        var (emailEntity, recipient) = SetupEmailWithRecipient();

        await _sut.RecordOpenAsync(recipient.Id);

        Assert.NotNull(recipient.OpenedAt);
        Assert.Equal(1, emailEntity.OpenedCount);
    }

    [Fact]
    public async Task RecordOpenAsync_DoesNotUpdateOnSubsequentOpens()
    {
        var (emailEntity, recipient) = SetupEmailWithRecipient();
        var firstOpenTime = DateTime.UtcNow.AddMinutes(-5);
        recipient.OpenedAt = firstOpenTime;

        await _sut.RecordOpenAsync(recipient.Id);

        Assert.Equal(firstOpenTime, recipient.OpenedAt);
        // OpenedCount should not change since the recipient was already opened
        Assert.Equal(0, emailEntity.OpenedCount);
    }

    [Fact]
    public async Task RecordOpenAsync_NoOpForUnknownRecipient()
    {
        // Should not throw
        await _sut.RecordOpenAsync("unknown_id");
    }

    // ── RecordClickAsync ────────────────────────────────────────

    [Fact]
    public async Task RecordClickAsync_CreatesClickEventAndReturnsDestination()
    {
        var redirect = new Redirect
        {
            Id = "redirect1",
            From = "/r/abc123",
            To = "https://example.com/target",
            CreatedAt = DateTime.UtcNow,
        };
        _emailRepo.Redirects.Add(redirect);

        var destination = await _sut.RecordClickAsync("redirect1", "member1");

        Assert.Equal("https://example.com/target", destination);
        Assert.Single(_emailRepo.ClickEvents);
        Assert.Equal("member1", _emailRepo.ClickEvents[0].MemberId);
        Assert.Equal("redirect1", _emailRepo.ClickEvents[0].RedirectId);
    }

    [Fact]
    public async Task RecordClickAsync_ThrowsForUnknownRedirect()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RecordClickAsync("nonexistent", "member1"));
    }

    // ── RecordFeedbackAsync ─────────────────────────────────────

    [Fact]
    public async Task RecordFeedbackAsync_CreatesNewFeedbackRecord()
    {
        var email = new Email
        {
            Id = "email1",
            PostId = "post1",
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "submitted",
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
        };
        _emailRepo.Emails.Add(email);

        await _sut.RecordFeedbackAsync("email1", "member1", 1);

        Assert.Single(_emailRepo.Feedbacks);
        var feedback = _emailRepo.Feedbacks[0];
        Assert.Equal("post1", feedback.PostId);
        Assert.Equal("member1", feedback.MemberId);
        Assert.Equal(1, feedback.Score);
    }

    [Fact]
    public async Task RecordFeedbackAsync_UpdatesExistingFeedback()
    {
        var email = new Email
        {
            Id = "email1",
            PostId = "post1",
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "submitted",
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
        };
        _emailRepo.Emails.Add(email);

        _emailRepo.Feedbacks.Add(new MembersFeedback
        {
            Id = "fb1",
            MemberId = "member1",
            PostId = "post1",
            Score = 1,
            CreatedAt = DateTime.UtcNow,
        });

        await _sut.RecordFeedbackAsync("email1", "member1", 0);

        // Should update existing, not add new
        Assert.Single(_emailRepo.Feedbacks);
        Assert.Equal(0, _emailRepo.Feedbacks[0].Score);
        Assert.NotNull(_emailRepo.Feedbacks[0].UpdatedAt);
    }

    [Fact]
    public async Task RecordFeedbackAsync_ThrowsForUnknownEmail()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RecordFeedbackAsync("nonexistent", "member1", 1));
    }

    // ── SubscribeAsync ──────────────────────────────────────────

    [Fact]
    public async Task SubscribeAsync_CreatesSubscription()
    {
        await _sut.SubscribeAsync("member1", "newsletter1");

        Assert.Single(_emailRepo.Subscriptions);
        Assert.Equal("member1", _emailRepo.Subscriptions[0].MemberId);
        Assert.Equal("newsletter1", _emailRepo.Subscriptions[0].NewsletterId);
    }

    [Fact]
    public async Task SubscribeAsync_NoOpIfAlreadySubscribed()
    {
        _emailRepo.Subscriptions.Add(new MembersNewsletter
        {
            Id = "existing",
            MemberId = "member1",
            NewsletterId = "newsletter1",
        });

        await _sut.SubscribeAsync("member1", "newsletter1");

        Assert.Single(_emailRepo.Subscriptions); // Still just one
    }

    // ── UnsubscribeAsync ────────────────────────────────────────

    [Fact]
    public async Task UnsubscribeAsync_RemovesSubscription()
    {
        _emailRepo.Subscriptions.Add(new MembersNewsletter
        {
            Id = "sub1",
            MemberId = "member1",
            NewsletterId = "newsletter1",
        });

        await _sut.UnsubscribeAsync("member1", "newsletter1");

        Assert.Empty(_emailRepo.Subscriptions);
    }

    [Fact]
    public async Task UnsubscribeAsync_NoOpIfNotSubscribed()
    {
        await _sut.UnsubscribeAsync("member1", "newsletter1");
        Assert.Empty(_emailRepo.Subscriptions);
    }

    // ── GetMemberSubscriptionsAsync ─────────────────────────────

    [Fact]
    public async Task GetMemberSubscriptionsAsync_ReturnsNewsletterIds()
    {
        _emailRepo.Subscriptions.Add(new MembersNewsletter { Id = "s1", MemberId = "m1", NewsletterId = "nl1" });
        _emailRepo.Subscriptions.Add(new MembersNewsletter { Id = "s2", MemberId = "m1", NewsletterId = "nl2" });
        _emailRepo.Subscriptions.Add(new MembersNewsletter { Id = "s3", MemberId = "m2", NewsletterId = "nl1" });

        var subs = await _sut.GetMemberSubscriptionsAsync("m1");

        Assert.Equal(2, subs.Count);
        Assert.Contains("nl1", subs);
        Assert.Contains("nl2", subs);
    }

    // ── A/B Subject Line Testing ─────────────────────────────────

    [Fact]
    public async Task SendPostAsNewsletterAsync_AbTesting_AssignsCohortsAndSubjects()
    {
        // 10 subscribers; 10% split → 1 each in A/B, 8 in holdout.
        var (newsletter, _) = SetupNewsletterWithMembers(10);
        var post = CreatePost();
        post.Meta = new PostMeta
        {
            Id = "meta1",
            PostId = post.Id,
            EmailSubject = "Subject A",
            EmailSubjectB = "Subject B",
        };
        _postRepo.Posts.Add(post);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
            AbSplitPercent = 10,
            AbWaitMinutes = 120,
        });

        Assert.Equal("Subject A", email.Subject);
        Assert.Equal("Subject B", email.SubjectB);
        Assert.Equal("testing", email.AbTestPhase);
        Assert.Equal(10, email.AbTestSplitPercent);
        Assert.Equal(120, email.AbTestWaitMinutes);
        Assert.NotNull(email.AbTestStartedAt);

        var byVariant = _emailRepo.Recipients
            .Where(r => r.EmailId == email.Id)
            .GroupBy(r => r.AbVariant ?? "(null)")
            .ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(1, byVariant["a"]);
        Assert.Equal(1, byVariant["b"]);
        Assert.Equal(8, byVariant["holdout"]);

        // Holdout recipients must NOT be processed yet.
        var holdouts = _emailRepo.Recipients.Where(r => r.AbVariant == "holdout");
        Assert.All(holdouts, r => Assert.Null(r.ProcessedAt));
        Assert.All(holdouts, r => Assert.Null(r.DeliveredAt));

        // Two cohort batches were created (one per variant).
        Assert.Equal(2, _emailRepo.Batches.Count);

        // Email service was called twice — once per cohort — with distinct subjects.
        Assert.Equal(2, _emailService.BatchRequests.Count);
        Assert.Contains(_emailService.BatchRequests, b => b.Subject == "Subject A");
        Assert.Contains(_emailService.BatchRequests, b => b.Subject == "Subject B");
    }

    [Fact]
    public async Task SendPostAsNewsletterAsync_AbTesting_FallsBackWhenTooFewSubscribers()
    {
        // Only 3 subscribers — below A/B viability threshold; should send as a normal newsletter.
        var (newsletter, _) = SetupNewsletterWithMembers(3);
        var post = CreatePost();
        post.Meta = new PostMeta
        {
            Id = "meta1",
            PostId = post.Id,
            EmailSubject = "Subject A",
            EmailSubjectB = "Subject B",
        };
        _postRepo.Posts.Add(post);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
        });

        Assert.Null(email.AbTestPhase);
        Assert.Null(email.SubjectB);
        Assert.Equal("submitted", email.Status);
        Assert.All(_emailRepo.Recipients, r => Assert.Null(r.AbVariant));
    }

    [Fact]
    public async Task SendAbTestWinnerAsync_PicksVariantWithMoreOpensAndSendsHoldout()
    {
        var (newsletter, _) = SetupNewsletterWithMembers(10);
        var post = CreatePost();
        post.Meta = new PostMeta
        {
            Id = "meta1",
            PostId = post.Id,
            EmailSubject = "Subject A",
            EmailSubjectB = "Subject B",
        };
        _postRepo.Posts.Add(post);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
            AbSplitPercent = 10,
        });

        // Simulate the B cohort opening; A does not.
        foreach (var r in _emailRepo.Recipients.Where(r => r.AbVariant == "b"))
            r.OpenedAt = DateTime.UtcNow;

        _emailService.BatchRequests.Clear();

        var result = await _sut.SendAbTestWinnerAsync(email.Id, SiteUrl);

        Assert.Equal("completed", result.AbTestPhase);
        Assert.Equal("b", result.AbTestWinnerVariant);
        Assert.Equal("Subject B", result.Subject); // Winner's subject promoted to primary
        Assert.Equal(0, result.AbTestOpensA);
        Assert.Equal(1, result.AbTestOpensB);

        // Holdout (8 recipients) was sent with the winning subject.
        Assert.Single(_emailService.BatchRequests);
        Assert.Equal("Subject B", _emailService.BatchRequests[0].Subject);
        Assert.Equal(8, _emailService.BatchRequests[0].Recipients.Count);

        // All holdout recipients are now processed and assigned to a batch.
        Assert.All(
            _emailRepo.Recipients.Where(r => r.AbVariant == "holdout"),
            r => Assert.NotNull(r.ProcessedAt));
    }

    [Fact]
    public async Task SendAbTestWinnerAsync_TieBreaksToVariantA()
    {
        var (newsletter, _) = SetupNewsletterWithMembers(10);
        var post = CreatePost();
        post.Meta = new PostMeta
        {
            Id = "meta1",
            PostId = post.Id,
            EmailSubject = "Subject A",
            EmailSubjectB = "Subject B",
        };
        _postRepo.Posts.Add(post);

        var email = await _sut.SendPostAsNewsletterAsync(new SendNewsletterRequest
        {
            PostId = post.Id,
            NewsletterId = newsletter.Id,
            SiteUrl = SiteUrl,
            AbSplitPercent = 10,
        });

        // Neither cohort opened — tie.
        var result = await _sut.SendAbTestWinnerAsync(email.Id, SiteUrl);

        Assert.Equal("a", result.AbTestWinnerVariant);
        Assert.Equal("Subject A", result.Subject);
    }

    [Fact]
    public async Task SendAbTestWinnerAsync_NoOpWhenNotInTestingPhase()
    {
        var email = new Email
        {
            Id = "email-x",
            PostId = "p1",
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "submitted",
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            AbTestPhase = null,
        };
        _emailRepo.Emails.Add(email);

        var result = await _sut.SendAbTestWinnerAsync(email.Id, SiteUrl);

        Assert.Null(result.AbTestPhase);
        Assert.Null(result.AbTestWinnerVariant);
        Assert.Empty(_emailService.BatchRequests);
    }

    // ── ProcessPendingEmailAsync ─────────────────────────────────

    [Fact]
    public async Task ProcessPendingEmailAsync_SegmentsAndSendsToAllSubscribers()
    {
        var (post, newsletter, members) = SetupStandardScenario();

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Equal("submitted", email.Status);
        Assert.Equal(2, email.EmailCount);
        Assert.Equal(2, email.DeliveredCount);
        Assert.Equal(0, email.FailedCount);

        Assert.Single(_emailRepo.Batches);
        Assert.Equal("submitted", _emailRepo.Batches[0].Status);
        Assert.Equal(2, _emailRepo.Recipients.Count);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_SkipsNonPendingEmail()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        var email = CreatePendingEmail(post, newsletter);
        email.Status = "submitted"; // Already processed
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Empty(_emailRepo.Batches);
        Assert.Empty(_emailRepo.Recipients);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_ThrowsWhenEmailNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessPendingEmailAsync("nonexistent", SiteUrl));
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_ThrowsWhenPostNotFound()
    {
        var newsletter = CreateNewsletter();
        _newsletterRepo.Newsletters.Add(newsletter);

        var email = new Email
        {
            Id = "email_orphan",
            PostId = "missing_post",
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "pending",
            NewsletterId = newsletter.Id,
            Subject = "Test",
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
        };
        _emailRepo.Emails.Add(email);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.ProcessPendingEmailAsync(email.Id, SiteUrl));
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_ExcludesSuppressedEmails()
    {
        var (post, newsletter, members) = SetupStandardScenario();
        _emailRepo.SuppressedEmails.Add(members[0].Email);

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Equal(1, email.EmailCount);
        Assert.Single(_emailRepo.Recipients);
        Assert.Equal(members[1].Email, _emailRepo.Recipients[0].MemberEmail);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_AppliesRecipientFilter()
    {
        var (post, newsletter, members) = SetupStandardScenario();
        members[0].Status = "free";
        members[1].Status = "paid";

        var email = CreatePendingEmail(post, newsletter, recipientFilter: "paid");
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Equal(1, email.EmailCount);
        Assert.Single(_emailRepo.Recipients);
        Assert.Equal(members[1].Email, _emailRepo.Recipients[0].MemberEmail);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_HandlesNoSubscribers()
    {
        var post = CreatePost();
        _postRepo.Posts.Add(post);
        var newsletter = CreateNewsletter();
        _newsletterRepo.Newsletters.Add(newsletter);
        // No members subscribed

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Equal("submitted", email.Status);
        Assert.Equal(0, email.EmailCount);
        Assert.Empty(_emailRepo.Batches);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_MarksAsSubmittingImmediately()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        // Final status after processing should be submitted (not submitting)
        Assert.Equal("submitted", email.Status);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_SetsFromAndReplyTo()
    {
        var (post, newsletter, _) = SetupStandardScenario();
        newsletter.SenderName = "Custom Name";
        newsletter.SenderEmail = "custom@example.com";

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Equal("Custom Name <custom@example.com>", email.From);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_HandlesPartialFailure()
    {
        var (post, newsletter, members) = SetupStandardScenario();
        _emailService.FailForEmails.Add(members[0].Email);

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Equal("submitted", email.Status);
        Assert.Equal(1, email.DeliveredCount);
        Assert.Equal(1, email.FailedCount);

        var failedRecipient = _emailRepo.Recipients.First(r => r.MemberEmail == members[0].Email);
        Assert.NotNull(failedRecipient.FailedAt);
        Assert.Single(failedRecipient.Failures);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_FullFailureSetsFailed()
    {
        var (post, newsletter, members) = SetupStandardScenario();
        foreach (var m in members)
            _emailService.FailForEmails.Add(m.Email);

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Equal("failed", email.Status);
        Assert.Equal(0, email.DeliveredCount);
        Assert.Equal(2, email.FailedCount);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_WrapsLinksForClickTracking()
    {
        var post = CreatePost(html: "<p>Visit <a href=\"https://example.com\">here</a></p>");
        _postRepo.Posts.Add(post);
        var (newsletter, _) = SetupNewsletterWithMembers(1);

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Single(_emailRepo.Redirects);
        Assert.Equal("https://example.com", _emailRepo.Redirects[0].To);
        Assert.Contains("/api/email/click/", email.Html!);
    }

    [Fact]
    public async Task ProcessPendingEmailAsync_SendsBatchWithSubstitutions()
    {
        var (post, newsletter, _) = SetupStandardScenario();

        var email = CreatePendingEmail(post, newsletter);
        _emailRepo.Emails.Add(email);

        await _sut.ProcessPendingEmailAsync(email.Id, SiteUrl);

        Assert.Single(_emailService.BatchRequests);
        var batchReq = _emailService.BatchRequests[0];
        Assert.Equal(2, batchReq.Recipients.Count);

        foreach (var (_, vars) in batchReq.Recipients)
        {
            Assert.Contains("tracking_pixel", vars.Keys);
            Assert.Contains("unsubscribe_url", vars.Keys);
            Assert.Contains("member_id", vars.Keys);
        }
    }

    // ── Setup Helpers ───────────────────────────────────────────

    private static int _counter;

    private static Post CreatePost(string? html = "<p>Test content</p>", string? title = null)
    {
        var id = $"post_{++_counter:D22}";
        return new Post
        {
            Id = id,
            Uuid = Guid.NewGuid().ToString("D"),
            Title = title ?? "Test Post",
            Slug = "test-post",
            Html = html,
            Plaintext = "Test content",
            Lexical = "{}",
            Status = "published",
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static Newsletter CreateNewsletter(string? status = "active")
    {
        var id = $"nl_{++_counter:D23}";
        return new Newsletter
        {
            Id = id,
            Uuid = Guid.NewGuid().ToString("D"),
            Name = "Default Newsletter",
            Slug = "default",
            Status = status!,
            SenderName = "HowToSoftware",
            SenderEmail = "noreply@howtoosoftware.com",
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static Member CreateMember(string email)
    {
        var id = $"member_{++_counter:D19}";
        return new Member
        {
            Id = id,
            Uuid = Guid.NewGuid().ToString("D"),
            TransientId = Guid.NewGuid().ToString("D"),
            Email = email,
            Status = "free",
            Name = email.Split('@')[0],
            CreatedAt = DateTime.UtcNow,
        };
    }

    private (Post post, Newsletter newsletter, List<Member> members) SetupStandardScenario()
    {
        var post = CreatePost();
        _postRepo.Posts.Add(post);

        var (newsletter, members) = SetupNewsletterWithMembers(2);
        return (post, newsletter, members);
    }

    private (Newsletter newsletter, List<Member> members) SetupNewsletterWithMembers(int memberCount)
    {
        var newsletter = CreateNewsletter();
        _newsletterRepo.Newsletters.Add(newsletter);

        var members = new List<Member>();
        for (var i = 0; i < memberCount; i++)
        {
            var member = CreateMember($"member{i}@example.com");
            members.Add(member);
            _memberRepo.Members.Add(member);
            _memberRepo.NewsletterSubscribers.TryAdd(newsletter.Id, []);
            _memberRepo.NewsletterSubscribers[newsletter.Id].Add(member);
        }

        return (newsletter, members);
    }

    private (Email email, EmailRecipient recipient) SetupEmailWithRecipient()
    {
        var emailEntity = new Email
        {
            Id = "email1",
            PostId = "post1",
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "submitted",
            CreatedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
        };
        _emailRepo.Emails.Add(emailEntity);

        var recipient = new EmailRecipient
        {
            Id = "recipient1",
            EmailId = "email1",
            BatchId = "batch1",
            MemberId = "member1",
            MemberUuid = Guid.NewGuid().ToString("D"),
            MemberEmail = "test@example.com",
        };
        _emailRepo.Recipients.Add(recipient);

        return (emailEntity, recipient);
    }

    private static Email CreatePendingEmail(Post post, Newsletter newsletter, string recipientFilter = "all")
    {
        var id = $"email_{++_counter:D19}";
        return new Email
        {
            Id = id,
            PostId = post.Id,
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "pending",
            RecipientFilter = recipientFilter,
            Subject = post.Title,
            Html = post.Html,
            Plaintext = post.Plaintext,
            Source = post.Lexical,
            SourceType = "lexical",
            TrackOpens = true,
            TrackClicks = true,
            FeedbackEnabled = true,
            NewsletterId = newsletter.Id,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };
    }

    // ── Fakes ───────────────────────────────────────────────────

    private sealed class FakePostRepository : IPostRepository
    {
        public List<Post> Posts { get; } = [];

        public Task<Post?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Id == id));
        public Task<Post?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Posts.FirstOrDefault(p => p.Slug == slug));
        public Task<Post?> GetByUuidAsync(string uuid, CancellationToken ct = default)
            => Task.FromResult<Post?>(null);
        public Task<Post?> GetByIdWithRevisionsAsync(string id, CancellationToken ct = default)
            => GetByIdAsync(id, ct);
        public Task AddAsync(Post post, CancellationToken ct = default)
        { Posts.Add(post); return Task.CompletedTask; }
        public Task UpdateAsync(Post post, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
        { Posts.RemoveAll(p => p.Id == id); return Task.CompletedTask; }

        public Task<PagedResult<Post>> GetPublishedPostsAsync(int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByTagAsync(string tagSlug, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByAuthorAsync(string authorSlug, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetPublishedPostsByNewsletterAsync(string newsletterId, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<PagedResult<Post>> GetAllAsync(string? status, string? type, int page, int pageSize, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetFeaturedPostsAsync(int count, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetRelatedPostsAsync(string postId, int count, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<int> GetCountAsync(string? status, string? type, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task DeleteManyAsync(IReadOnlyList<string> ids, CancellationToken ct = default)
        { Posts.RemoveAll(p => ids.Contains(p.Id)); return Task.CompletedTask; }
        public Task SetFeaturedAsync(IReadOnlyList<string> ids, bool featured, CancellationToken ct = default)
        { foreach (var p in Posts.Where(p => ids.Contains(p.Id))) p.Featured = featured; return Task.CompletedTask; }
        public Task<List<Post>> GetAllPagesAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Post>> GetPublishedPagesAsync(CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task UpdateSortOrderAsync(IReadOnlyList<(string Id, string? ParentId, int SortOrder)> updates, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeNewsletterRepository : INewsletterRepository
    {
        public List<Newsletter> Newsletters { get; } = [];

        public Task<Newsletter?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Newsletters.FirstOrDefault(n => n.Id == id));
        public Task<Newsletter?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(Newsletters.FirstOrDefault(n => n.Slug == slug));
        public Task<List<Newsletter>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(Newsletters.ToList());
        public Task<List<Newsletter>> GetActiveAsync(CancellationToken ct = default)
            => Task.FromResult(Newsletters.Where(n => n.Status == "active").ToList());
        public Task AddAsync(Newsletter newsletter, CancellationToken ct = default)
        { Newsletters.Add(newsletter); return Task.CompletedTask; }
        public Task UpdateAsync(Newsletter newsletter, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
        { Newsletters.RemoveAll(n => n.Id == id); return Task.CompletedTask; }
        public Task<int> GetSubscriberCountAsync(string newsletterId, CancellationToken ct = default)
            => Task.FromResult(0);
        public Task<NewsletterAnalytics> GetAnalyticsAsync(string newsletterId, int sendsLimit = 10, CancellationToken ct = default)
            => Task.FromResult(new NewsletterAnalytics(0, 0, 0, 0, 0, 0, []));
        public Task<IReadOnlyList<NewsletterGrowthPoint>> GetGrowthAsync(string newsletterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NewsletterGrowthPoint>>([]);
    }

    private sealed class FakeMemberRepository : IMemberRepository
    {
        public List<Member> Members { get; } = [];
        public Dictionary<string, List<Member>> NewsletterSubscribers { get; } = [];

        public Task<Member?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Id == id));
        public Task<Member?> GetByEmailAsync(string email, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Email == email));
        public Task<Member?> GetByUuidAsync(string uuid, CancellationToken ct = default)
            => Task.FromResult(Members.FirstOrDefault(m => m.Uuid == uuid));
        public Task<PagedResult<Member>> GetAllAsync(string? status, string? search, string? labelId, int page, int pageSize, MemberEngagementFilter? engagement = null, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Member>> GetAllForExportAsync(string? status, string? labelId, MemberEngagementFilter? engagement = null, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Member>> GetByLabelAsync(string labelId, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<List<Member>> GetNewsletterSubscribersAsync(string newsletterId, CancellationToken ct = default)
            => Task.FromResult(NewsletterSubscribers.TryGetValue(newsletterId, out var list) ? list.ToList() : []);
        public Task AddAsync(Member member, CancellationToken ct = default)
        { Members.Add(member); return Task.CompletedTask; }
        public Task UpdateAsync(Member member, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task DeleteAsync(string id, CancellationToken ct = default)
        { Members.RemoveAll(m => m.Id == id); return Task.CompletedTask; }
        public Task<int> GetCountAsync(string? status, CancellationToken ct = default)
            => Task.FromResult(Members.Count);
        public Task AddLabelToMemberAsync(string memberId, string labelId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task RemoveLabelFromMemberAsync(string memberId, string labelId, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<bool> LinkStripeCustomerAsync(string memberId, string stripeCustomerId, string? name, string? email, CancellationToken ct = default)
            => Task.FromResult(true);
        public Task UpdateNoteAsync(string memberId, string? note, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AddCreatedEventAsync(MembersCreatedEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AddStatusEventAsync(MembersStatusEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AddSubscribeEventAsync(MembersSubscribeEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEmailRepository : IEmailRepository
    {
        public List<Email> Emails { get; } = [];
        public List<EmailBatch> Batches { get; } = [];
        public List<EmailRecipient> Recipients { get; } = [];
        public List<Redirect> Redirects { get; } = [];
        public List<MembersClickEvent> ClickEvents { get; } = [];
        public HashSet<string> SuppressedEmails { get; } = [];
        public List<MembersNewsletter> Subscriptions { get; } = [];
        public List<MembersFeedback> Feedbacks { get; } = [];

        public Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
            => Task.FromResult<ITransactionScope>(new NoOpTransactionScope());

        public Task<Email?> GetByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Emails.FirstOrDefault(e => e.Id == id));
        public Task<List<Email>> GetPendingEmailsAsync(CancellationToken ct = default)
            => Task.FromResult(Emails.Where(e => e.Status == "pending").OrderBy(e => e.CreatedAt).ToList());
        public Task<List<Email>> GetAbTestsAwaitingWinnerAsync(DateTime now, CancellationToken ct = default)
            => Task.FromResult(Emails
                .Where(e => e.AbTestPhase == "testing" && e.AbTestStartedAt != null
                    && e.AbTestStartedAt.Value.AddMinutes(e.AbTestWaitMinutes) <= now)
                .OrderBy(e => e.AbTestStartedAt)
                .ToList());
        public Task<List<EmailRecipient>> GetHoldoutRecipientsAsync(string emailId, CancellationToken ct = default)
            => Task.FromResult(Recipients
                .Where(r => r.EmailId == emailId && r.AbVariant == "holdout" && r.ProcessedAt == null)
                .ToList());
        public Task<Dictionary<string, int>> GetAbVariantOpenCountsAsync(string emailId, CancellationToken ct = default)
            => Task.FromResult(Recipients
                .Where(r => r.EmailId == emailId && r.OpenedAt != null && (r.AbVariant == "a" || r.AbVariant == "b"))
                .GroupBy(r => r.AbVariant!)
                .ToDictionary(g => g.Key, g => g.Count()));
        public Task AddEmailAsync(Email email, CancellationToken ct = default)
        { Emails.Add(email); return Task.CompletedTask; }
        public Task UpdateEmailAsync(Email email, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AddBatchAsync(EmailBatch batch, CancellationToken ct = default)
        { Batches.Add(batch); return Task.CompletedTask; }
        public Task UpdateBatchAsync(EmailBatch batch, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AddRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default)
        { Recipients.AddRange(recipients); return Task.CompletedTask; }
        public Task<EmailRecipient?> GetRecipientByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Recipients.FirstOrDefault(r => r.Id == id));
        public Task UpdateRecipientAsync(EmailRecipient recipient, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task UpdateRecipientsAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task AddRedirectAsync(Redirect redirect, CancellationToken ct = default)
        { Redirects.Add(redirect); return Task.CompletedTask; }
        public Task<Redirect?> GetRedirectByIdAsync(string id, CancellationToken ct = default)
            => Task.FromResult(Redirects.FirstOrDefault(r => r.Id == id));

        public Task AddClickEventAsync(MembersClickEvent clickEvent, CancellationToken ct = default)
        { ClickEvents.Add(clickEvent); return Task.CompletedTask; }

        public Task<List<string>> GetSuppressedEmailsAsync(CancellationToken ct = default)
            => Task.FromResult(SuppressedEmails.ToList());

        public Task<bool> IsEmailSuppressedAsync(string email, CancellationToken ct = default)
            => Task.FromResult(SuppressedEmails.Contains(email));
        public Task AddSuppressionAsync(Suppression suppression, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task RemoveSuppressionAsync(string email, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task AddSpamComplaintEventAsync(EmailSpamComplaintEvent evt, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<MembersNewsletter?> GetSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default)
            => Task.FromResult(Subscriptions.FirstOrDefault(s => s.MemberId == memberId && s.NewsletterId == newsletterId));
        public Task<List<MembersNewsletter>> GetMemberSubscriptionsAsync(string memberId, CancellationToken ct = default)
            => Task.FromResult(Subscriptions.Where(s => s.MemberId == memberId).ToList());
        public Task AddSubscriptionAsync(MembersNewsletter subscription, CancellationToken ct = default)
        { Subscriptions.Add(subscription); return Task.CompletedTask; }
        public Task RemoveSubscriptionAsync(string memberId, string newsletterId, CancellationToken ct = default)
        { Subscriptions.RemoveAll(s => s.MemberId == memberId && s.NewsletterId == newsletterId); return Task.CompletedTask; }

        public Task<MembersFeedback?> GetFeedbackAsync(string memberId, string postId, CancellationToken ct = default)
            => Task.FromResult(Feedbacks.FirstOrDefault(f => f.MemberId == memberId && f.PostId == postId));
        public Task AddFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default)
        { Feedbacks.Add(feedback); return Task.CompletedTask; }
        public Task UpdateFeedbackAsync(MembersFeedback feedback, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeEmailService : IEmailService
    {
        public List<EmailBatchRequest> BatchRequests { get; } = [];
        public HashSet<string> FailForEmails { get; } = [];
        public EmailTemplateModel? LastTemplateModel { get; private set; }
        public string? LastTemplateName { get; private set; }

        public Task<string> RenderTemplateAsync(string templateName, EmailTemplateModel model, CancellationToken ct = default)
        {
            LastTemplateName = templateName;
            LastTemplateModel = model;
            return Task.FromResult(model.HtmlBody ?? string.Empty);
        }

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken ct = default)
            => Task.FromResult(new EmailSendResult { RecipientEmail = message.To, Success = true });

        public Task<List<EmailSendResult>> SendBatchAsync(EmailBatchRequest request, CancellationToken ct = default)
        {
            BatchRequests.Add(request);
            var results = request.Recipients.Keys.Select(email => new EmailSendResult
            {
                RecipientEmail = email,
                Success = !FailForEmails.Contains(email),
                ErrorMessage = FailForEmails.Contains(email) ? "Delivery failed" : null,
                ErrorCode = FailForEmails.Contains(email) ? 550 : null,
            }).ToList();
            return Task.FromResult(results);
        }
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public Task<string?> GetStringAsync(string key, CancellationToken ct = default)
            => Task.FromResult(key switch
            {
                "title" => (string?)"HowToSoftware",
                "icon" => "https://howtoosoftware.com/content/images/icon.png",
                "accent_color" => "#ff6600",
                "members_support_address" => "support@howtoosoftware.com",
                _ => null,
            });

        public Task<bool?> GetBoolAsync(string key, CancellationToken ct = default) => Task.FromResult<bool?>(null);
        public Task<int?> GetIntAsync(string key, CancellationToken ct = default) => Task.FromResult<int?>(null);
        public Task<T?> GetJsonAsync<T>(string key, CancellationToken ct = default) where T : class => Task.FromResult<T?>(null);
        public Task<Setting?> GetAsync(string key, CancellationToken ct = default) => Task.FromResult<Setting?>(null);
        public Task<List<Setting>> GetAllAsync(CancellationToken ct = default) => Task.FromResult<List<Setting>>([]);
        public Task<List<Setting>> GetByGroupAsync(string group, CancellationToken ct = default) => Task.FromResult<List<Setting>>([]);
        public Task SetAsync(string key, string? value, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetBoolAsync(string key, bool value, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetIntAsync(string key, int value, CancellationToken ct = default) => Task.CompletedTask;
        public Task SetJsonAsync<T>(string key, T value, CancellationToken ct = default) where T : class => Task.CompletedTask;
        public Task DeleteAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public void InvalidateCache() { }
        public void InvalidateCache(string key) { }
    }

    private sealed class NoOpTransactionScope : ITransactionScope
    {
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
