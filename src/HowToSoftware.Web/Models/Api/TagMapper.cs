using HowToSoftware.Core.Entities;

namespace HowToSoftware.Web.Models.Api;

public static class TagMapper
{
    public static TagResource ToResource(Tag tag, int? postCount = null)
    {
        return new TagResource
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
            Count = postCount.HasValue ? new TagCountResource { Posts = postCount.Value } : null,
        };
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
