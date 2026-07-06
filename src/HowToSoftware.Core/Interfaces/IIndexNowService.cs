namespace HowToSoftware.Core.Interfaces;

/// <summary>
/// Enqueues URLs for submission to search engines via the IndexNow protocol.
/// Submissions are dispatched asynchronously in the background.
/// </summary>
public interface IIndexNowService
{
    /// <summary>
    /// Enqueues a URL for IndexNow submission to search engines.
    /// </summary>
    /// <param name="url">The absolute URL of the page that was published or updated.</param>
    void Enqueue(string url);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
