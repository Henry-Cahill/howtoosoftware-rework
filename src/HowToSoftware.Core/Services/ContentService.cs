using System.Security.Cryptography;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Core.Services;

public class ContentService(
    IPostRepository postRepository,
    ISlugGenerator slugGenerator,
    ILexicalRenderer lexicalRenderer,
    IMobiledocRenderer mobiledocRenderer,
    IWebhookDispatchService webhookDispatch,
    IIndexNowService indexNow) : IContentService
{
    public async Task<Post> CreateAsync(ContentCreateRequest request, string authorId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var postId = GenerateId();

        var slug = request.Slug is not null
            ? request.Slug
            : await slugGenerator.GenerateUniqueSlugAsync(
                request.Title,
                async s => await postRepository.GetBySlugAsync(s, ct) is not null,
                ct);

        var post = new Post
        {
            Id = postId,
            Uuid = Guid.NewGuid().ToString("D"),
            Title = request.Title,
            Slug = slug,
            Type = request.Type is "post" or "page" ? request.Type : "post",
            Status = "draft",
            Lexical = request.Lexical,
            Mobiledoc = request.Mobiledoc,
            Html = RenderHtml(request.Lexical, request.Mobiledoc),
            Plaintext = StripHtml(RenderHtml(request.Lexical, request.Mobiledoc)),
            Featured = request.Featured,
            FeatureImage = request.FeatureImage,
            CustomExcerpt = request.CustomExcerpt,
            Visibility = request.Visibility,
            Locale = request.Locale,
            CodeinjectionHead = request.CodeinjectionHead,
            CodeinjectionFoot = request.CodeinjectionFoot,
            CustomTemplate = request.CustomTemplate,
            CanonicalUrl = request.CanonicalUrl,
            ParentId = request.ParentId,
            EmailRecipientFilter = "all",
            ShowTitleAndFeatureImage = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Primary author
        post.PostsAuthors.Add(new PostsAuthor
        {
            Id = GenerateId(),
            PostId = postId,
            AuthorId = authorId,
            SortOrder = 0,
        });

        // Tags
        if (request.TagIds is { Count: > 0 })
        {
            for (var i = 0; i < request.TagIds.Count; i++)
            {
                post.PostsTags.Add(new PostsTag
                {
                    Id = GenerateId(),
                    PostId = postId,
                    TagId = request.TagIds[i],
                    SortOrder = i,
                });
            }
        }

        // Post meta
        if (request.Meta is not null)
        {
            post.Meta = CreateMeta(postId, request.Meta);
        }

        // Initial revision
        post.Revisions.Add(CreateRevision(post, authorId, "initial"));

        await postRepository.AddAsync(post, ct);

        var addedEvent = post.Type == "page" ? "page.added" : "post.added";
        webhookDispatch.Enqueue(addedEvent, new { id = post.Id, title = post.Title, slug = post.Slug, type = post.Type, status = post.Status });

        return post;
    }

    public async Task<Post> UpdateAsync(string id, ContentUpdateRequest request, string editorId, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        var now = DateTime.UtcNow;
        var contentChanged = false;

        // Title
        if (request.Title is not null && request.Title != post.Title)
        {
            post.Title = request.Title;
            contentChanged = true;
        }

        // Slug
        if (request.Slug is not null && request.Slug != post.Slug)
        {
            post.Slug = await slugGenerator.GenerateUniqueSlugAsync(
                request.Slug,
                async s => s != post.Slug && await postRepository.GetBySlugAsync(s, ct) is not null,
                ct);
        }

        // Content
        if (request.Lexical is not null && request.Lexical != post.Lexical)
        {
            post.Lexical = request.Lexical;
            post.Mobiledoc = null;
            post.Html = RenderHtml(request.Lexical, null);
            post.Plaintext = StripHtml(post.Html);
            contentChanged = true;
        }
        else if (request.Mobiledoc is not null && request.Mobiledoc != post.Mobiledoc)
        {
            post.Mobiledoc = request.Mobiledoc;
            post.Lexical = null;
            post.Html = RenderHtml(null, request.Mobiledoc);
            post.Plaintext = StripHtml(post.Html);
            contentChanged = true;
        }

        // Scalar fields
        if (request.Featured.HasValue) post.Featured = request.Featured.Value;
        if (request.FeatureImage is not null) post.FeatureImage = request.FeatureImage;
        if (request.CustomExcerpt is not null) post.CustomExcerpt = request.CustomExcerpt;
        if (request.Visibility is not null) post.Visibility = request.Visibility;
        if (request.Locale is not null) post.Locale = request.Locale;
        if (request.CodeinjectionHead is not null) post.CodeinjectionHead = request.CodeinjectionHead;
        if (request.CodeinjectionFoot is not null) post.CodeinjectionFoot = request.CodeinjectionFoot;
        if (request.CustomTemplate is not null) post.CustomTemplate = request.CustomTemplate;
        if (request.CanonicalUrl is not null) post.CanonicalUrl = request.CanonicalUrl;
        if (request.ParentId is not null) post.ParentId = request.ParentId;
        if (request.ClearParent) post.ParentId = null;

        // Tags — replace the full set
        if (request.TagIds is not null)
        {
            post.PostsTags.Clear();
            for (var i = 0; i < request.TagIds.Count; i++)
            {
                post.PostsTags.Add(new PostsTag
                {
                    Id = GenerateId(),
                    PostId = post.Id,
                    TagId = request.TagIds[i],
                    SortOrder = i,
                });
            }
        }

        // Meta
        if (request.Meta is not null)
        {
            if (post.Meta is not null)
            {
                ApplyMeta(post.Meta, request.Meta);
            }
            else
            {
                post.Meta = CreateMeta(post.Id, request.Meta);
            }
        }

        // Revision on content/title change
        if (contentChanged)
        {
            post.Revisions.Add(CreateRevision(post, editorId, request.RevisionReason ?? "edited"));
        }

        post.UpdatedAt = now;
        await postRepository.UpdateAsync(post, ct);

        var editedEvent = post.Type == "page" ? "page.edited" : "post.edited";
        webhookDispatch.Enqueue(editedEvent, new { id = post.Id, title = post.Title, slug = post.Slug, type = post.Type, status = post.Status });

        // Notify search engines via IndexNow when a published post is updated
        if (post.Status == "published")
        {
            indexNow.Enqueue($"/{post.Slug}/");
        }

        return post;
    }

    public async Task<Post> PublishAsync(string id, string publisherId, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        if (post.Status == "published")
            return post;

        var now = DateTime.UtcNow;
        var wasDraft = post.Status is "draft" or "scheduled";

        post.Status = "published";
        post.PublishedAt ??= now;
        post.PublishedBy = publisherId;
        post.UpdatedAt = now;

        if (wasDraft)
        {
            post.Revisions.Add(CreateRevision(post, publisherId, "published"));
        }

        await postRepository.UpdateAsync(post, ct);

        var publishedEvent = post.Type == "page" ? "page.published" : "post.published";
        webhookDispatch.Enqueue(publishedEvent, new { id = post.Id, title = post.Title, slug = post.Slug, type = post.Type });

        // Notify search engines via IndexNow
        indexNow.Enqueue($"/{post.Slug}/");

        return post;
    }

    public async Task<Post> UnpublishAsync(string id, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        if (post.Status == "draft")
            return post;

        post.Status = "draft";
        post.UpdatedAt = DateTime.UtcNow;

        await postRepository.UpdateAsync(post, ct);

        var unpublishedEvent = post.Type == "page" ? "page.unpublished" : "post.unpublished";
        webhookDispatch.Enqueue(unpublishedEvent, new { id = post.Id, title = post.Title, slug = post.Slug, type = post.Type });

        return post;
    }

    public async Task<Post> ScheduleAsync(string id, DateTime scheduledAt, string publisherId, CancellationToken ct = default)
    {
        if (scheduledAt <= DateTime.UtcNow)
            throw new ArgumentException("Scheduled date must be in the future.", nameof(scheduledAt));

        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        post.Status = "scheduled";
        post.PublishedAt = scheduledAt;
        post.PublishedBy = publisherId;
        post.UpdatedAt = DateTime.UtcNow;

        post.Revisions.Add(CreateRevision(post, publisherId, "scheduled"));

        await postRepository.UpdateAsync(post, ct);

        webhookDispatch.Enqueue("post.scheduled", new { id = post.Id, title = post.Title, slug = post.Slug, scheduled_at = scheduledAt });

        return post;
    }

    public async Task<Email> SendAsEmailAsync(string postId, string newsletterId, string recipientFilter, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(postId, ct)
            ?? throw new InvalidOperationException($"Post '{postId}' not found.");

        if (string.IsNullOrWhiteSpace(post.Html))
            throw new InvalidOperationException("Cannot send a post with no rendered content as email.");

        var now = DateTime.UtcNow;

        var email = new Email
        {
            Id = GenerateId(),
            PostId = post.Id,
            Uuid = Guid.NewGuid().ToString("D"),
            Status = "pending",
            RecipientFilter = recipientFilter,
            Subject = post.Meta?.EmailSubject ?? post.Title,
            SubjectB = post.Meta?.EmailSubjectB,
            Html = post.Html,
            Plaintext = post.Plaintext,
            Source = post.Lexical ?? post.Mobiledoc,
            SourceType = post.Lexical is not null ? "lexical" : "mobiledoc",
            TrackOpens = true,
            TrackClicks = true,
            FeedbackEnabled = true,
            NewsletterId = newsletterId,
            SubmittedAt = now,
            CreatedAt = now,
        };

        post.NewsletterId = newsletterId;
        post.EmailRecipientFilter = recipientFilter;
        post.Emails.Add(email);
        post.UpdatedAt = now;

        await postRepository.UpdateAsync(post, ct);
        return email;
    }

    public async Task<Post> DuplicateAsync(string id, string authorId, CancellationToken ct = default)
    {
        var source = await postRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        var request = new ContentCreateRequest
        {
            Title = source.Title + " (Copy)",
            Type = source.Type,
            Lexical = source.Lexical,
            Mobiledoc = source.Mobiledoc,
            Featured = source.Featured,
            FeatureImage = source.FeatureImage,
            CustomExcerpt = source.CustomExcerpt,
            Visibility = source.Visibility,
            Locale = source.Locale,
            CodeinjectionHead = source.CodeinjectionHead,
            CodeinjectionFoot = source.CodeinjectionFoot,
            CustomTemplate = source.CustomTemplate,
            CanonicalUrl = null, // intentionally omit — duplicate should not share canonical
            TagIds = source.PostsTags.OrderBy(pt => pt.SortOrder).Select(pt => pt.TagId).ToList(),
            Meta = source.Meta is not null
                ? new PostMetaRequest
                {
                    OgImage = source.Meta.OgImage,
                    OgTitle = source.Meta.OgTitle,
                    OgDescription = source.Meta.OgDescription,
                    TwitterImage = source.Meta.TwitterImage,
                    TwitterTitle = source.Meta.TwitterTitle,
                    TwitterDescription = source.Meta.TwitterDescription,
                    MetaTitle = source.Meta.MetaTitle,
                    MetaDescription = source.Meta.MetaDescription,
                    Frontmatter = source.Meta.Frontmatter,
                    FeatureImageAlt = source.Meta.FeatureImageAlt,
                    FeatureImageCaption = source.Meta.FeatureImageCaption,
                }
                : null,
        };

        return await CreateAsync(request, authorId, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdAsync(id, ct)
            ?? throw new InvalidOperationException($"Post '{id}' not found.");

        await postRepository.DeleteAsync(id, ct);

        var deletedEvent = post.Type == "page" ? "page.deleted" : "post.deleted";
        webhookDispatch.Enqueue(deletedEvent, new { id = post.Id, title = post.Title, slug = post.Slug, type = post.Type });
    }

    public async Task<List<PostRevision>> GetRevisionsAsync(string postId, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdWithRevisionsAsync(postId, ct)
            ?? throw new InvalidOperationException($"Post '{postId}' not found.");

        return [.. post.Revisions.OrderByDescending(r => r.CreatedAt)];
    }

    public async Task<Post> RestoreRevisionAsync(string postId, string revisionId, string editorId, CancellationToken ct = default)
    {
        var post = await postRepository.GetByIdWithRevisionsAsync(postId, ct)
            ?? throw new InvalidOperationException($"Post '{postId}' not found.");

        var revision = post.Revisions.FirstOrDefault(r => r.Id == revisionId)
            ?? throw new InvalidOperationException($"Revision '{revisionId}' not found.");

        if (revision.Title is not null)
            post.Title = revision.Title;
        if (revision.Lexical is not null)
        {
            post.Lexical = revision.Lexical;
            post.Mobiledoc = null;
            post.Html = RenderHtml(revision.Lexical, null);
            post.Plaintext = StripHtml(post.Html);
        }
        if (revision.FeatureImage is not null)
            post.FeatureImage = revision.FeatureImage;
        if (revision.CustomExcerpt is not null)
            post.CustomExcerpt = revision.CustomExcerpt;

        post.UpdatedAt = DateTime.UtcNow;
        post.Revisions.Add(CreateRevision(post, editorId, "restored"));

        await postRepository.UpdateAsync(post, ct);
        return post;
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private string? RenderHtml(string? lexical, string? mobiledoc)
    {
        if (lexical is not null)
            return lexicalRenderer.Render(lexical);
        if (mobiledoc is not null)
            return mobiledocRenderer.Render(mobiledoc);
        return null;
    }

    private static string? StripHtml(string? html)
    {
        if (html is null) return null;

        // Simple tag stripping — sufficient for plaintext excerpts
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static PostRevision CreateRevision(Post post, string authorId, string reason)
    {
        var now = DateTime.UtcNow;
        return new PostRevision
        {
            Id = GenerateId(),
            PostId = post.Id,
            Lexical = post.Lexical,
            Title = post.Title,
            PostStatus = post.Status,
            Reason = reason,
            FeatureImage = post.FeatureImage,
            CustomExcerpt = post.CustomExcerpt,
            AuthorId = authorId,
            CreatedAt = now,
            CreatedAtTs = new DateTimeOffset(now).ToUnixTimeMilliseconds(),
        };
    }

    private static PostMeta CreateMeta(string postId, PostMetaRequest req) => new()
    {
        Id = GenerateId(),
        PostId = postId,
        OgImage = req.OgImage,
        OgTitle = req.OgTitle,
        OgDescription = req.OgDescription,
        TwitterImage = req.TwitterImage,
        TwitterTitle = req.TwitterTitle,
        TwitterDescription = req.TwitterDescription,
        MetaTitle = req.MetaTitle,
        MetaDescription = req.MetaDescription,
        EmailSubject = req.EmailSubject,
        EmailSubjectB = req.EmailSubjectB,
        Frontmatter = req.Frontmatter,
        FeatureImageAlt = req.FeatureImageAlt,
        FeatureImageCaption = req.FeatureImageCaption,
        EmailOnly = req.EmailOnly,
    };

    private static void ApplyMeta(PostMeta meta, PostMetaRequest req)
    {
        meta.OgImage = req.OgImage;
        meta.OgTitle = req.OgTitle;
        meta.OgDescription = req.OgDescription;
        meta.TwitterImage = req.TwitterImage;
        meta.TwitterTitle = req.TwitterTitle;
        meta.TwitterDescription = req.TwitterDescription;
        meta.MetaTitle = req.MetaTitle;
        meta.MetaDescription = req.MetaDescription;
        meta.EmailSubject = req.EmailSubject;
        meta.EmailSubjectB = req.EmailSubjectB;
        meta.Frontmatter = req.Frontmatter;
        meta.FeatureImageAlt = req.FeatureImageAlt;
        meta.FeatureImageCaption = req.FeatureImageCaption;
        meta.EmailOnly = req.EmailOnly;
    }

    /// <summary>
    /// Generates a 24-character lowercase hex ID (matches Ghost's ObjectId format).
    /// </summary>
    private static string GenerateId()
    {
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
