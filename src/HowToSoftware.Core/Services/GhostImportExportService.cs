using System.Security.Cryptography;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Core.Utilities;

namespace HowToSoftware.Core.Services;

public class GhostImportExportService(
    IPostRepository postRepository,
    ITagRepository tagRepository,
    IUserRepository userRepository,
    ILexicalRenderer lexicalRenderer,
    IMobiledocRenderer mobiledocRenderer) : IGhostImportExportService
{
    private const string GhostDateFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    public async Task<GhostImportResult> ImportAsync(GhostExportRoot export, string importerId, CancellationToken ct = default)
    {
        var result = new GhostImportResult();

        if (export.Db is not { Count: > 0 })
        {
            result.Errors.Add("No database entries found in the import file.");
            return result;
        }

        var data = export.Db[0].Data;
        var now = DateTime.UtcNow;

        // ── 1. Import tags ──────────────────────────────────────────
        var tagIdMap = new Dictionary<string, string>(); // old → new

        foreach (var gt in data.Tags)
        {
            try
            {
                var existing = await tagRepository.GetBySlugAsync(gt.Slug, ct);
                if (existing is not null)
                {
                    tagIdMap[gt.Id] = existing.Id;
                    result.TagsSkipped++;
                    continue;
                }

                var newId = GenerateId();
                tagIdMap[gt.Id] = newId;

                var tag = new Tag
                {
                    Id = newId,
                    Name = gt.Name,
                    Slug = gt.Slug,
                    Description = gt.Description,
                    FeatureImage = gt.FeatureImage,
                    ParentId = gt.ParentId,
                    Visibility = gt.Visibility,
                    OgImage = gt.OgImage,
                    OgTitle = gt.OgTitle,
                    OgDescription = gt.OgDescription,
                    TwitterImage = gt.TwitterImage,
                    TwitterTitle = gt.TwitterTitle,
                    TwitterDescription = gt.TwitterDescription,
                    MetaTitle = gt.MetaTitle,
                    MetaDescription = gt.MetaDescription,
                    CodeinjectionHead = gt.CodeinjectionHead,
                    CodeinjectionFoot = gt.CodeinjectionFoot,
                    CanonicalUrl = gt.CanonicalUrl,
                    AccentColor = gt.AccentColor,
                    CreatedAt = ParseGhostDate(gt.CreatedAt) ?? now,
                    UpdatedAt = ParseGhostDate(gt.UpdatedAt),
                };

                await tagRepository.AddAsync(tag, ct);
                result.TagsImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Tag '{gt.Name}': {ex.Message}");
            }
        }

        // ── 2. Import users (authors) ───────────────────────────────
        var userIdMap = new Dictionary<string, string>(); // old → new

        foreach (var gu in data.Users)
        {
            try
            {
                var existing = gu.Email is not null
                    ? await userRepository.GetByEmailAsync(gu.Email, ct)
                    : await userRepository.GetBySlugAsync(gu.Slug, ct);

                if (existing is not null)
                {
                    userIdMap[gu.Id] = existing.Id;
                    result.UsersSkipped++;
                    continue;
                }

                var newId = GenerateId();
                userIdMap[gu.Id] = newId;

                var user = new User
                {
                    Id = newId,
                    UserName = gu.Email ?? gu.Slug,
                    Email = gu.Email,
                    NormalizedEmail = gu.Email?.ToUpperInvariant(),
                    NormalizedUserName = (gu.Email ?? gu.Slug).ToUpperInvariant(),
                    EmailConfirmed = true,
                    Name = gu.Name,
                    Slug = gu.Slug,
                    ProfileImage = gu.ProfileImage,
                    CoverImage = gu.CoverImage,
                    Bio = gu.Bio,
                    Website = gu.Website,
                    Location = gu.Location,
                    Facebook = gu.Facebook,
                    Twitter = gu.Twitter,
                    Accessibility = gu.Accessibility,
                    Status = gu.Status,
                    Locale = gu.Locale,
                    Visibility = gu.Visibility,
                    MetaTitle = gu.MetaTitle,
                    MetaDescription = gu.MetaDescription,
                    CreatedAt = ParseGhostDate(gu.CreatedAt) ?? now,
                    UpdatedAt = ParseGhostDate(gu.UpdatedAt),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                };

                await userRepository.AddAsync(user, ct);
                result.UsersImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"User '{gu.Name}': {ex.Message}");
            }
        }

        // Build junction-table lookup from import data
        var postTagsLookup = data.PostsTags
            .GroupBy(pt => pt.PostId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ToList());

        var postAuthorsLookup = data.PostsAuthors
            .GroupBy(pa => pa.PostId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.SortOrder).ToList());

        var postMetaLookup = data.PostsMeta
            .ToDictionary(pm => pm.PostId);

        // ── 3. Import posts ─────────────────────────────────────────

        foreach (var gp in data.Posts)
        {
            try
            {
                var existing = await postRepository.GetBySlugAsync(gp.Slug, ct);
                if (existing is not null)
                {
                    result.PostsSkipped++;
                    continue;
                }

                var postId = GenerateId();
                var html = RenderHtml(gp.Lexical, gp.Mobiledoc) ?? gp.Html;

                var post = new Post
                {
                    Id = postId,
                    Uuid = gp.Uuid ?? Guid.NewGuid().ToString("D"),
                    Title = gp.Title,
                    Slug = gp.Slug,
                    Mobiledoc = gp.Mobiledoc,
                    Lexical = gp.Lexical,
                    Html = html,
                    CommentId = gp.CommentId ?? postId,
                    Plaintext = StripHtml(html),
                    FeatureImage = gp.FeatureImage,
                    Featured = gp.Featured != 0,
                    Type = gp.Type,
                    Status = gp.Status,
                    Locale = gp.Locale,
                    Visibility = gp.Visibility,
                    EmailRecipientFilter = gp.EmailRecipientFilter,
                    CreatedAt = ParseGhostDate(gp.CreatedAt) ?? now,
                    UpdatedAt = ParseGhostDate(gp.UpdatedAt),
                    PublishedAt = ParseGhostDate(gp.PublishedAt),
                    PublishedBy = gp.PublishedBy is not null && userIdMap.TryGetValue(gp.PublishedBy, out var pub)
                        ? pub
                        : null,
                    CustomExcerpt = gp.CustomExcerpt,
                    CodeinjectionHead = gp.CodeinjectionHead,
                    CodeinjectionFoot = gp.CodeinjectionFoot,
                    CustomTemplate = gp.CustomTemplate,
                    CanonicalUrl = gp.CanonicalUrl,
                    NewsletterId = gp.NewsletterId,
                    ShowTitleAndFeatureImage = gp.ShowTitleAndFeatureImage != 0,
                };

                // Tags
                if (postTagsLookup.TryGetValue(gp.Id, out var ptList))
                {
                    for (var i = 0; i < ptList.Count; i++)
                    {
                        if (tagIdMap.TryGetValue(ptList[i].TagId, out var mappedTagId))
                        {
                            post.PostsTags.Add(new PostsTag
                            {
                                Id = GenerateId(),
                                PostId = postId,
                                TagId = mappedTagId,
                                SortOrder = i,
                            });
                        }
                    }
                }

                // Authors
                if (postAuthorsLookup.TryGetValue(gp.Id, out var paList))
                {
                    for (var i = 0; i < paList.Count; i++)
                    {
                        if (userIdMap.TryGetValue(paList[i].AuthorId, out var mappedAuthorId))
                        {
                            post.PostsAuthors.Add(new PostsAuthor
                            {
                                Id = GenerateId(),
                                PostId = postId,
                                AuthorId = mappedAuthorId,
                                SortOrder = i,
                            });
                        }
                    }
                }
                else
                {
                    // Fallback: assign the importer as author
                    post.PostsAuthors.Add(new PostsAuthor
                    {
                        Id = GenerateId(),
                        PostId = postId,
                        AuthorId = importerId,
                        SortOrder = 0,
                    });
                }

                // Post meta
                if (postMetaLookup.TryGetValue(gp.Id, out var pm))
                {
                    post.Meta = new PostMeta
                    {
                        Id = GenerateId(),
                        PostId = postId,
                        OgImage = pm.OgImage,
                        OgTitle = pm.OgTitle,
                        OgDescription = pm.OgDescription,
                        TwitterImage = pm.TwitterImage,
                        TwitterTitle = pm.TwitterTitle,
                        TwitterDescription = pm.TwitterDescription,
                        MetaTitle = pm.MetaTitle,
                        MetaDescription = pm.MetaDescription,
                        EmailSubject = pm.EmailSubject,
                        Frontmatter = pm.Frontmatter,
                        FeatureImageAlt = pm.FeatureImageAlt,
                        FeatureImageCaption = pm.FeatureImageCaption,
                        EmailOnly = pm.EmailOnly != 0,
                    };
                }

                await postRepository.AddAsync(post, ct);
                result.PostsImported++;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Post '{gp.Title}': {ex.Message}");
            }
        }

        return result;
    }

    public async Task<GhostExportRoot> ExportAsync(CancellationToken ct = default)
    {
        var posts = await postRepository.GetAllAsync(null, null, 1, int.MaxValue, ct);
        var tags = await tagRepository.GetAllAsync(ct);
        var users = await userRepository.GetAllStaffAsync(ct);

        var data = new GhostData();

        // ── Tags ────────────────────────────────────────────────────
        foreach (var t in tags)
        {
            data.Tags.Add(new GhostTag
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Description = t.Description,
                FeatureImage = t.FeatureImage,
                ParentId = t.ParentId,
                Visibility = t.Visibility,
                OgImage = t.OgImage,
                OgTitle = t.OgTitle,
                OgDescription = t.OgDescription,
                TwitterImage = t.TwitterImage,
                TwitterTitle = t.TwitterTitle,
                TwitterDescription = t.TwitterDescription,
                MetaTitle = t.MetaTitle,
                MetaDescription = t.MetaDescription,
                CodeinjectionHead = t.CodeinjectionHead,
                CodeinjectionFoot = t.CodeinjectionFoot,
                CanonicalUrl = t.CanonicalUrl,
                AccentColor = t.AccentColor,
                CreatedAt = FormatGhostDate(t.CreatedAt),
                UpdatedAt = t.UpdatedAt.HasValue ? FormatGhostDate(t.UpdatedAt.Value) : null,
            });
        }

        // ── Users ───────────────────────────────────────────────────
        foreach (var u in users)
        {
            data.Users.Add(new GhostUser
            {
                Id = u.Id,
                Name = u.Name,
                Slug = u.Slug,
                Email = u.Email,
                ProfileImage = u.ProfileImage,
                CoverImage = u.CoverImage,
                Bio = u.Bio,
                Website = u.Website,
                Location = u.Location,
                Facebook = u.Facebook,
                Twitter = u.Twitter,
                Accessibility = u.Accessibility,
                Status = u.Status,
                Locale = u.Locale,
                Visibility = u.Visibility,
                MetaTitle = u.MetaTitle,
                MetaDescription = u.MetaDescription,
                CreatedAt = FormatGhostDate(u.CreatedAt),
                UpdatedAt = u.UpdatedAt.HasValue ? FormatGhostDate(u.UpdatedAt.Value) : null,
            });
        }

        // ── Posts + junction tables ─────────────────────────────────
        foreach (var p in posts.Items)
        {
            data.Posts.Add(new GhostPost
            {
                Id = p.Id,
                Uuid = p.Uuid,
                Title = p.Title,
                Slug = p.Slug,
                Mobiledoc = p.Mobiledoc,
                Lexical = p.Lexical,
                Html = p.Html,
                CommentId = p.CommentId,
                Plaintext = p.Plaintext,
                FeatureImage = p.FeatureImage,
                Featured = p.Featured ? 1 : 0,
                Type = p.Type,
                Status = p.Status,
                Locale = p.Locale,
                Visibility = p.Visibility,
                EmailRecipientFilter = p.EmailRecipientFilter,
                CreatedAt = FormatGhostDate(p.CreatedAt),
                UpdatedAt = p.UpdatedAt.HasValue ? FormatGhostDate(p.UpdatedAt.Value) : null,
                PublishedAt = p.PublishedAt.HasValue ? FormatGhostDate(p.PublishedAt.Value) : null,
                PublishedBy = p.PublishedBy,
                CustomExcerpt = p.CustomExcerpt,
                CodeinjectionHead = p.CodeinjectionHead,
                CodeinjectionFoot = p.CodeinjectionFoot,
                CustomTemplate = p.CustomTemplate,
                CanonicalUrl = p.CanonicalUrl,
                NewsletterId = p.NewsletterId,
                ShowTitleAndFeatureImage = p.ShowTitleAndFeatureImage ? 1 : 0,
            });

            foreach (var pt in p.PostsTags)
            {
                data.PostsTags.Add(new GhostPostsTag
                {
                    Id = pt.Id,
                    PostId = pt.PostId,
                    TagId = pt.TagId,
                    SortOrder = pt.SortOrder,
                });
            }

            foreach (var pa in p.PostsAuthors)
            {
                data.PostsAuthors.Add(new GhostPostsAuthor
                {
                    Id = pa.Id,
                    PostId = pa.PostId,
                    AuthorId = pa.AuthorId,
                    SortOrder = pa.SortOrder,
                });
            }

            if (p.Meta is not null)
            {
                data.PostsMeta.Add(new GhostPostMeta
                {
                    Id = p.Meta.Id,
                    PostId = p.Meta.PostId,
                    OgImage = p.Meta.OgImage,
                    OgTitle = p.Meta.OgTitle,
                    OgDescription = p.Meta.OgDescription,
                    TwitterImage = p.Meta.TwitterImage,
                    TwitterTitle = p.Meta.TwitterTitle,
                    TwitterDescription = p.Meta.TwitterDescription,
                    MetaTitle = p.Meta.MetaTitle,
                    MetaDescription = p.Meta.MetaDescription,
                    EmailSubject = p.Meta.EmailSubject,
                    Frontmatter = p.Meta.Frontmatter,
                    FeatureImageAlt = p.Meta.FeatureImageAlt,
                    FeatureImageCaption = p.Meta.FeatureImageCaption,
                    EmailOnly = p.Meta.EmailOnly ? 1 : 0,
                });
            }
        }

        return new GhostExportRoot
        {
            Db =
            [
                new GhostDatabase
                {
                    Meta = new GhostExportMeta
                    {
                        ExportedOn = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                        Version = "5.0.0",
                    },
                    Data = data,
                },
            ],
        };
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
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }

    private static DateTime? ParseGhostDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var dt))
            return dt;
        return null;
    }

    private static string FormatGhostDate(DateTime dt) =>
        dt.ToUniversalTime().ToString(GhostDateFormat, System.Globalization.CultureInfo.InvariantCulture);

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
