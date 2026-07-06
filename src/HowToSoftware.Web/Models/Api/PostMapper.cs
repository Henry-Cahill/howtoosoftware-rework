using HowToSoftware.Core.Entities;

namespace HowToSoftware.Web.Models.Api;

/// <summary>
/// Maps Post entities to Ghost-compatible API resources,
/// respecting include and fields selections.
/// </summary>
public static class PostMapper
{
    public static PostResource ToResource(Post post, HashSet<string> includes, HashSet<string>? fields)
    {
        bool includeTags = includes.Contains("tags");
        bool includeAuthors = includes.Contains("authors");
        bool allFields = fields is null || fields.Count == 0;

        var tags = includeTags
            ? post.PostsTags.OrderBy(pt => pt.SortOrder).Select(pt => MapTag(pt.Tag)).ToList()
            : null;

        var authors = includeAuthors
            ? post.PostsAuthors.OrderBy(pa => pa.SortOrder).Select(pa => MapAuthor(pa.Author)).ToList()
            : null;

        var primaryTag = includeTags ? tags?.FirstOrDefault() : null;
        var primaryAuthor = includeAuthors ? authors?.FirstOrDefault() : null;

        var excerpt = post.CustomExcerpt ?? TruncatePlaintext(post.Plaintext, 500);
        var readingTime = EstimateReadingTime(post.Plaintext);

        return new PostResource
        {
            Id = post.Id,
            Uuid = Field(allFields, fields, "uuid") ? post.Uuid : null,
            Title = Field(allFields, fields, "title") ? post.Title : null,
            Slug = Field(allFields, fields, "slug") ? post.Slug : null,
            Html = Field(allFields, fields, "html") ? post.Html : null,
            CommentId = Field(allFields, fields, "comment_id") ? post.CommentId : null,
            FeatureImage = Field(allFields, fields, "feature_image") ? post.FeatureImage : null,
            Featured = Field(allFields, fields, "featured") ? post.Featured : null,
            Status = Field(allFields, fields, "status") ? post.Status : null,
            Visibility = Field(allFields, fields, "visibility") ? post.Visibility : null,
            CreatedAt = Field(allFields, fields, "created_at") ? post.CreatedAt : null,
            UpdatedAt = Field(allFields, fields, "updated_at") ? post.UpdatedAt : null,
            PublishedAt = Field(allFields, fields, "published_at") ? post.PublishedAt : null,
            CustomExcerpt = Field(allFields, fields, "custom_excerpt") ? post.CustomExcerpt : null,
            CodeinjectionHead = Field(allFields, fields, "codeinjection_head") ? post.CodeinjectionHead : null,
            CodeinjectionFoot = Field(allFields, fields, "codeinjection_foot") ? post.CodeinjectionFoot : null,
            CustomTemplate = Field(allFields, fields, "custom_template") ? post.CustomTemplate : null,
            CanonicalUrl = Field(allFields, fields, "canonical_url") ? post.CanonicalUrl : null,
            Excerpt = Field(allFields, fields, "excerpt") ? excerpt : null,
            ReadingTime = Field(allFields, fields, "reading_time") ? readingTime : null,
            OgImage = Field(allFields, fields, "og_image") ? post.Meta?.OgImage : null,
            OgTitle = Field(allFields, fields, "og_title") ? post.Meta?.OgTitle : null,
            OgDescription = Field(allFields, fields, "og_description") ? post.Meta?.OgDescription : null,
            TwitterImage = Field(allFields, fields, "twitter_image") ? post.Meta?.TwitterImage : null,
            TwitterTitle = Field(allFields, fields, "twitter_title") ? post.Meta?.TwitterTitle : null,
            TwitterDescription = Field(allFields, fields, "twitter_description") ? post.Meta?.TwitterDescription : null,
            MetaTitle = Field(allFields, fields, "meta_title") ? post.Meta?.MetaTitle : null,
            MetaDescription = Field(allFields, fields, "meta_description") ? post.Meta?.MetaDescription : null,
            EmailSubject = Field(allFields, fields, "email_subject") ? post.Meta?.EmailSubject : null,
            Frontmatter = Field(allFields, fields, "frontmatter") ? post.Meta?.Frontmatter : null,
            FeatureImageAlt = Field(allFields, fields, "feature_image_alt") ? post.Meta?.FeatureImageAlt : null,
            FeatureImageCaption = Field(allFields, fields, "feature_image_caption") ? post.Meta?.FeatureImageCaption : null,
            Tags = tags,
            Authors = authors,
            PrimaryTag = primaryTag,
            PrimaryAuthor = primaryAuthor,
        };
    }

    private static bool Field(bool allFields, HashSet<string>? fields, string name)
        => allFields || fields!.Contains(name);

    private static TagResource MapTag(Tag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        Slug = tag.Slug,
        Description = tag.Description,
        FeatureImage = tag.FeatureImage,
        Visibility = tag.Visibility,
        OgImage = tag.OgImage,
        OgTitle = tag.OgTitle,
        OgDescription = tag.OgDescription,
        TwitterImage = tag.TwitterImage,
        TwitterTitle = tag.TwitterTitle,
        TwitterDescription = tag.TwitterDescription,
        MetaTitle = tag.MetaTitle,
        MetaDescription = tag.MetaDescription,
        CodeinjectionHead = tag.CodeinjectionHead,
        CodeinjectionFoot = tag.CodeinjectionFoot,
        CanonicalUrl = tag.CanonicalUrl,
        AccentColor = tag.AccentColor,
        Url = $"/tag/{tag.Slug}/",
    };

    private static AuthorResource MapAuthor(User user) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Slug = user.Slug,
        ProfileImage = user.ProfileImage,
        CoverImage = user.CoverImage,
        Bio = user.Bio,
        Website = user.Website,
        Location = user.Location,
        Facebook = user.Facebook,
        Twitter = user.Twitter,
        MetaTitle = user.MetaTitle,
        MetaDescription = user.MetaDescription,
        Url = $"/author/{user.Slug}/",
    };

    private static string? TruncatePlaintext(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length <= maxLength) return text;
        // Truncate at last word boundary
        var truncated = text[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        return lastSpace > 0 ? truncated[..lastSpace] + "..." : truncated + "...";
    }

    private static int EstimateReadingTime(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext)) return 0;
        var wordCount = plaintext.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        return Math.Max(1, (int)Math.Round(wordCount / 275.0));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
