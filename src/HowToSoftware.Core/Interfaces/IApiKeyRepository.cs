using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetBySecretAsync(string secret, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
