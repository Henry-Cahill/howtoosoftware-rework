using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class ApiKeyRepository(AppDbContext db) : IApiKeyRepository
{
    public async Task<ApiKey?> GetBySecretAsync(string secret, CancellationToken ct = default)
    {
        return await db.ApiKeys
            .Include(k => k.Integration)
            .FirstOrDefaultAsync(k => k.Secret == secret && k.Type == "content", ct);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
