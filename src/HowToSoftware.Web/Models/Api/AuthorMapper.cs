using HowToSoftware.Core.Entities;

namespace HowToSoftware.Web.Models.Api;

public static class AuthorMapper
{
    public static AuthorResource ToResource(User user, int? postCount = null)
    {
        return new AuthorResource
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
            Count = postCount.HasValue ? new AuthorCountResource { Posts = postCount.Value } : null,
        };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
