using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Tests;

public class PostMapperTests
{
    private static Post CreateTestPost() => new()
    {
        Id = "post-1",
        Uuid = "uuid-1",
        Title = "Test Post",
        Slug = "test-post",
        Html = "<p>Hello world</p>",
        Plaintext = "Hello world this is a test post",
        FeatureImage = "/content/images/test.jpg",
        Featured = true,
        Type = "post",
        Status = "published",
        Visibility = "public",
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        UpdatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        PublishedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        CustomExcerpt = "A test excerpt",
        PostsTags =
        [
            new PostsTag
            {
                Id = "pt-1", PostId = "post-1", TagId = "tag-1", SortOrder = 0,
                Tag = new Tag { Id = "tag-1", Name = "News", Slug = "news", Visibility = "public", CreatedAt = DateTime.UtcNow }
            },
            new PostsTag
            {
                Id = "pt-2", PostId = "post-1", TagId = "tag-2", SortOrder = 1,
                Tag = new Tag { Id = "tag-2", Name = "Tech", Slug = "tech", Visibility = "public", CreatedAt = DateTime.UtcNow }
            },
        ],
        PostsAuthors =
        [
            new PostsAuthor
            {
                Id = "pa-1", PostId = "post-1", AuthorId = "user-1", SortOrder = 0,
                Author = new User { Id = "user-1", Name = "John Doe", Slug = "john-doe", Email = "john@test.com", PasswordHash = "hash", CreatedAt = DateTime.UtcNow }
            }
        ],
        Meta = new PostMeta
        {
            Id = "meta-1", PostId = "post-1",
            OgTitle = "OG Title", MetaTitle = "Meta Title"
        },
    };

    [Fact]
    public void ToResource_AllFieldsNoIncludes_ReturnsAllFieldsNoRelations()
    {
        var post = CreateTestPost();
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = PostMapper.ToResource(post, includes, null);

        Assert.Equal("post-1", result.Id);
        Assert.Equal("Test Post", result.Title);
        Assert.Equal("test-post", result.Slug);
        Assert.Equal("<p>Hello world</p>", result.Html);
        Assert.True(result.Featured);
        Assert.Equal("published", result.Status);
        Assert.Equal("A test excerpt", result.Excerpt);
        Assert.Null(result.Tags);
        Assert.Null(result.Authors);
        Assert.Null(result.PrimaryTag);
        Assert.Null(result.PrimaryAuthor);
    }

    [Fact]
    public void ToResource_IncludeTags_ReturnsTagsAndPrimaryTag()
    {
        var post = CreateTestPost();
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tags" };

        var result = PostMapper.ToResource(post, includes, null);

        Assert.NotNull(result.Tags);
        Assert.Equal(2, result.Tags.Count);
        Assert.Equal("News", result.Tags[0].Name);
        Assert.Equal("Tech", result.Tags[1].Name);
        Assert.NotNull(result.PrimaryTag);
        Assert.Equal("News", result.PrimaryTag.Name);
        Assert.Equal("/tag/news/", result.PrimaryTag.Url);
    }

    [Fact]
    public void ToResource_IncludeAuthors_ReturnsAuthorsAndPrimaryAuthor()
    {
        var post = CreateTestPost();
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "authors" };

        var result = PostMapper.ToResource(post, includes, null);

        Assert.NotNull(result.Authors);
        Assert.Single(result.Authors);
        Assert.Equal("John Doe", result.Authors[0].Name);
        Assert.NotNull(result.PrimaryAuthor);
        Assert.Equal("/author/john-doe/", result.PrimaryAuthor.Url);
    }

    [Fact]
    public void ToResource_IncludeBoth_ReturnsBothRelations()
    {
        var post = CreateTestPost();
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "tags", "authors" };

        var result = PostMapper.ToResource(post, includes, null);

        Assert.NotNull(result.Tags);
        Assert.NotNull(result.Authors);
        Assert.NotNull(result.PrimaryTag);
        Assert.NotNull(result.PrimaryAuthor);
    }

    [Fact]
    public void ToResource_WithFieldsSelection_OnlyRequestedFieldsPopulated()
    {
        var post = CreateTestPost();
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "id", "title", "slug" };

        var result = PostMapper.ToResource(post, includes, fields);

        Assert.Equal("post-1", result.Id);
        Assert.Equal("Test Post", result.Title);
        Assert.Equal("test-post", result.Slug);
        // Fields not requested should be null
        Assert.Null(result.Html);
        Assert.Null(result.Uuid);
        Assert.Null(result.Status);
        Assert.Null(result.FeatureImage);
    }

    [Fact]
    public void ToResource_MetaFieldsMapped_WhenAllFields()
    {
        var post = CreateTestPost();
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = PostMapper.ToResource(post, includes, null);

        Assert.Equal("OG Title", result.OgTitle);
        Assert.Equal("Meta Title", result.MetaTitle);
    }

    [Fact]
    public void ToResource_ReadingTimCalculated()
    {
        var post = CreateTestPost();
        post.Plaintext = string.Join(" ", Enumerable.Repeat("word", 550)); // ~2 min read
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = PostMapper.ToResource(post, includes, null);

        Assert.Equal(2, result.ReadingTime);
    }

    [Fact]
    public void ToResource_ExcerptFallsBackToPlaintext_WhenNoCustomExcerpt()
    {
        var post = CreateTestPost();
        post.CustomExcerpt = null;
        post.Plaintext = "This is the plaintext content";
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = PostMapper.ToResource(post, includes, null);

        Assert.Equal("This is the plaintext content", result.Excerpt);
    }

    [Fact]
    public void ToResource_ExcerptTruncated_WhenPlaintextIsLong()
    {
        var post = CreateTestPost();
        post.CustomExcerpt = null;
        post.Plaintext = new string('a', 300) + " " + new string('b', 300);
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = PostMapper.ToResource(post, includes, null);

        Assert.NotNull(result.Excerpt);
        Assert.True(result.Excerpt!.Length <= 510); // 500 + "..."
        Assert.EndsWith("...", result.Excerpt);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
