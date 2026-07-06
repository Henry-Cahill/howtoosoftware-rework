using System.Text.Json;
using HowToSoftware.Web.Models.Api;

namespace HowToSoftware.Web.Tests;

/// <summary>
/// Verifies that all Ghost-compatible API envelope types serialize to the
/// exact JSON shape expected by Ghost Content API consumers.
/// </summary>
public class GhostEnvelopeTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // ASP.NET Core default: preserve property names (we use [JsonPropertyName] attributes)
        WriteIndented = false,
    };

    // ── Posts envelope ──

    [Fact]
    public void PostsEnvelope_Serializes_With_Posts_Array_And_Meta_Pagination()
    {
        var envelope = new GhostEnvelope<PostResource>
        {
            Posts =
            [
                new PostResource { Id = "abc123", Title = "Hello World", Slug = "hello-world" },
                new PostResource { Id = "def456", Title = "Second Post", Slug = "second-post" },
            ],
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination
                {
                    Page = 1, Limit = 15, Pages = 3, Total = 42,
                    Next = 2, Prev = null,
                }
            }
        };

        var json = JsonSerializer.Serialize(envelope, Options);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Top-level keys: "posts" and "meta"
        Assert.True(root.TryGetProperty("posts", out var posts));
        Assert.True(root.TryGetProperty("meta", out var meta));
        Assert.Equal(JsonValueKind.Array, posts.ValueKind);
        Assert.Equal(2, posts.GetArrayLength());

        // First post has expected fields
        var first = posts[0];
        Assert.Equal("abc123", first.GetProperty("id").GetString());
        Assert.Equal("Hello World", first.GetProperty("title").GetString());
        Assert.Equal("hello-world", first.GetProperty("slug").GetString());

        // meta.pagination
        var pagination = meta.GetProperty("pagination");
        Assert.Equal(1, pagination.GetProperty("page").GetInt32());
        Assert.Equal(15, pagination.GetProperty("limit").GetInt32());
        Assert.Equal(3, pagination.GetProperty("pages").GetInt32());
        Assert.Equal(42, pagination.GetProperty("total").GetInt32());
        Assert.Equal(2, pagination.GetProperty("next").GetInt32());
        Assert.Equal(JsonValueKind.Null, pagination.GetProperty("prev").ValueKind);
    }

    [Fact]
    public void PostsEnvelope_SinglePost_Wraps_In_Array()
    {
        var envelope = new GhostEnvelope<PostResource>
        {
            Posts = [new PostResource { Id = "single1" }],
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination
                {
                    Page = 1, Limit = 1, Pages = 1, Total = 1,
                    Next = null, Prev = null,
                }
            }
        };

        var json = JsonSerializer.Serialize(envelope, Options);
        using var doc = JsonDocument.Parse(json);

        var posts = doc.RootElement.GetProperty("posts");
        Assert.Equal(JsonValueKind.Array, posts.ValueKind);
        Assert.Equal(1, posts.GetArrayLength());
        Assert.Equal("single1", posts[0].GetProperty("id").GetString());
    }

    // ── Pages envelope ──

    [Fact]
    public void PagesEnvelope_Serializes_With_Pages_Key()
    {
        var envelope = new GhostPagesEnvelope<PostResource>
        {
            Pages = [new PostResource { Id = "page1", Title = "About" }],
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination
                {
                    Page = 1, Limit = 15, Pages = 1, Total = 1,
                    Next = null, Prev = null,
                }
            }
        };

        var json = JsonSerializer.Serialize(envelope, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("pages", out var pages));
        Assert.Equal(JsonValueKind.Array, pages.ValueKind);
        Assert.Equal("page1", pages[0].GetProperty("id").GetString());

        // Must NOT have "posts" key
        Assert.False(doc.RootElement.TryGetProperty("posts", out _));
    }

    // ── Tags envelope ──

    [Fact]
    public void TagsEnvelope_Serializes_With_Tags_Key()
    {
        var envelope = new GhostTagsEnvelope
        {
            Tags =
            [
                new TagResource { Id = "tag1", Name = "C#", Slug = "csharp", Url = "/tag/csharp/" },
            ],
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination
                {
                    Page = 1, Limit = 15, Pages = 1, Total = 1,
                    Next = null, Prev = null,
                }
            }
        };

        var json = JsonSerializer.Serialize(envelope, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("tags", out var tags));
        Assert.Equal(JsonValueKind.Array, tags.ValueKind);
        Assert.Equal("tag1", tags[0].GetProperty("id").GetString());
        Assert.Equal("C#", tags[0].GetProperty("name").GetString());
    }

    [Fact]
    public void TagsEnvelope_IncludesCount_When_Set()
    {
        var envelope = new GhostTagsEnvelope
        {
            Tags =
            [
                new TagResource
                {
                    Id = "tag1", Name = "C#", Slug = "csharp",
                    Count = new TagCountResource { Posts = 7 },
                },
            ],
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination { Page = 1, Limit = 15, Pages = 1, Total = 1 }
            }
        };

        var json = JsonSerializer.Serialize(envelope, Options);
        using var doc = JsonDocument.Parse(json);

        var count = doc.RootElement.GetProperty("tags")[0].GetProperty("count");
        Assert.Equal(7, count.GetProperty("posts").GetInt32());
    }

    // ── Authors envelope ──

    [Fact]
    public void AuthorsEnvelope_Serializes_With_Authors_Key()
    {
        var envelope = new GhostAuthorsEnvelope
        {
            Authors =
            [
                new AuthorResource { Id = "user1", Name = "Jane", Slug = "jane", Url = "/author/jane/" },
            ],
            Meta = new GhostMeta
            {
                Pagination = new GhostPagination
                {
                    Page = 1, Limit = 15, Pages = 1, Total = 1,
                    Next = null, Prev = null,
                }
            }
        };

        var json = JsonSerializer.Serialize(envelope, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("authors", out var authors));
        Assert.Equal(JsonValueKind.Array, authors.ValueKind);
        Assert.Equal("user1", authors[0].GetProperty("id").GetString());
    }

    // ── Settings envelope ──

    [Fact]
    public void SettingsEnvelope_Serializes_With_Settings_Object_And_Empty_Meta()
    {
        var envelope = new GhostSettingsEnvelope
        {
            Settings = new SettingsResource
            {
                Title = "My Blog",
                Description = "A test blog",
                Url = "https://example.com",
            },
            Meta = new { },
        };

        var json = JsonSerializer.Serialize(envelope, Options);
        using var doc = JsonDocument.Parse(json);

        // "settings" is an object (not array)
        Assert.True(doc.RootElement.TryGetProperty("settings", out var settings));
        Assert.Equal(JsonValueKind.Object, settings.ValueKind);
        Assert.Equal("My Blog", settings.GetProperty("title").GetString());

        // "meta" is an empty object
        Assert.True(doc.RootElement.TryGetProperty("meta", out var meta));
        Assert.Equal(JsonValueKind.Object, meta.ValueKind);
    }

    // ── Error response ──

    [Fact]
    public void ErrorResponse_Serializes_With_Errors_Array()
    {
        var error = new GhostErrorResponse
        {
            Errors = [new GhostError { Message = "Post not found.", Type = "NotFoundError" }],
        };

        var json = JsonSerializer.Serialize(error, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("errors", out var errors));
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        Assert.Equal(1, errors.GetArrayLength());
        Assert.Equal("Post not found.", errors[0].GetProperty("message").GetString());
        Assert.Equal("NotFoundError", errors[0].GetProperty("type").GetString());
    }

    // ── Pagination edge cases ──

    [Fact]
    public void Pagination_LastPage_Has_Null_Next()
    {
        var pagination = new GhostPagination
        {
            Page = 3, Limit = 15, Pages = 3, Total = 42,
            Next = null, Prev = 2,
        };

        var json = JsonSerializer.Serialize(pagination, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("next").ValueKind);
        Assert.Equal(2, doc.RootElement.GetProperty("prev").GetInt32());
    }

    [Fact]
    public void Pagination_FirstPage_Has_Null_Prev()
    {
        var pagination = new GhostPagination
        {
            Page = 1, Limit = 15, Pages = 3, Total = 42,
            Next = 2, Prev = null,
        };

        var json = JsonSerializer.Serialize(pagination, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(2, doc.RootElement.GetProperty("next").GetInt32());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("prev").ValueKind);
    }

    [Fact]
    public void Pagination_SinglePage_Has_Null_Next_And_Prev()
    {
        var pagination = new GhostPagination
        {
            Page = 1, Limit = 15, Pages = 1, Total = 5,
            Next = null, Prev = null,
        };

        var json = JsonSerializer.Serialize(pagination, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("next").ValueKind);
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("prev").ValueKind);
    }

    // ── PostResource null omission ──

    [Fact]
    public void PostResource_Omits_Null_Optional_Fields()
    {
        var post = new PostResource { Id = "p1" };

        var json = JsonSerializer.Serialize(post, Options);
        using var doc = JsonDocument.Parse(json);

        // "id" always present
        Assert.Equal("p1", doc.RootElement.GetProperty("id").GetString());

        // Null optional fields should be omitted
        Assert.False(doc.RootElement.TryGetProperty("title", out _));
        Assert.False(doc.RootElement.TryGetProperty("slug", out _));
        Assert.False(doc.RootElement.TryGetProperty("html", out _));
        Assert.False(doc.RootElement.TryGetProperty("tags", out _));
        Assert.False(doc.RootElement.TryGetProperty("authors", out _));
        Assert.False(doc.RootElement.TryGetProperty("meta_title", out _));
    }

    [Fact]
    public void PostResource_Includes_Tags_And_Authors_When_Set()
    {
        var post = new PostResource
        {
            Id = "p1",
            Tags = [new TagResource { Id = "t1", Name = "Tech" }],
            Authors = [new AuthorResource { Id = "a1", Name = "Jane" }],
            PrimaryTag = new TagResource { Id = "t1", Name = "Tech" },
            PrimaryAuthor = new AuthorResource { Id = "a1", Name = "Jane" },
        };

        var json = JsonSerializer.Serialize(post, Options);
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("tags", out var tags));
        Assert.Equal(1, tags.GetArrayLength());
        Assert.True(doc.RootElement.TryGetProperty("authors", out var authors));
        Assert.Equal(1, authors.GetArrayLength());
        Assert.True(doc.RootElement.TryGetProperty("primary_tag", out _));
        Assert.True(doc.RootElement.TryGetProperty("primary_author", out _));
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
