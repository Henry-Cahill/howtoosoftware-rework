namespace HowToSoftware.Core.Interfaces;

public interface IBruteForceService
{
    /// <summary>
    /// Records an attempt and returns true if the key has exceeded
    /// <paramref name="maxAttempts"/> within the sliding <paramref name="window"/>.
    /// </summary>
    Task<bool> TrackAsync(string key, int maxAttempts, TimeSpan window, CancellationToken ct = default);

    /// <summary>
    /// Resets the counter for a key (e.g., after successful authentication).
    /// </summary>
    Task ResetAsync(string key, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
