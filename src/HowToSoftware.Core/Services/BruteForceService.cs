using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Core.Services;

public class BruteForceService(IBruteForceRepository repository) : IBruteForceService
{
    public async Task<bool> TrackAsync(string key, int maxAttempts, TimeSpan window, CancellationToken ct = default)
    {
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var lifetimeMs = (long)window.TotalMilliseconds;

        var record = await repository.GetAsync(key, ct);

        if (record is null)
        {
            await repository.UpsertAsync(new Brute
            {
                Key = key,
                FirstRequest = nowMs,
                LastRequest = nowMs,
                Lifetime = lifetimeMs,
                Count = 1,
            }, ct);
            return false;
        }

        // Window expired → start a new window
        if (nowMs - record.FirstRequest > lifetimeMs)
        {
            record.FirstRequest = nowMs;
            record.LastRequest = nowMs;
            record.Lifetime = lifetimeMs;
            record.Count = 1;
            await repository.UpsertAsync(record, ct);
            return false;
        }

        // Within window → increment and check
        record.Count++;
        record.LastRequest = nowMs;
        await repository.UpsertAsync(record, ct);

        return record.Count > maxAttempts;
    }

    public async Task ResetAsync(string key, CancellationToken ct = default)
    {
        await repository.DeleteAsync(key, ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
