namespace HowToSoftware.Core.Interfaces;

public interface IActivityPubHttpClient
{
    Task<string?> FetchActorAsync(string apId, CancellationToken ct = default);
    Task<bool> DeliverAsync(string inboxUrl, string activity, string actorKeyId, string privateKeyPem, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
