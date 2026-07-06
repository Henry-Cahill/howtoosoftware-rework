namespace HowToSoftware.Web.Models;

public sealed class PaginationViewModel
{
    public required int CurrentPage { get; init; }
    public required int TotalPages { get; init; }
    public required bool HasPreviousPage { get; init; }
    public required bool HasNextPage { get; init; }

    /// <summary>
    /// Base URL for pagination links (e.g. "" for index, "/author/john", "/tag/csharp").
    /// The page query parameter will be appended as ?page=N.
    /// </summary>
    public string BaseUrl { get; init; } = "";
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
