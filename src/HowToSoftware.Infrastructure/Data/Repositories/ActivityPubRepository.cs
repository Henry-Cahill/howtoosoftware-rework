using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Infrastructure.Data.Repositories;

public class ActivityPubRepository(AppDbContext db) : IActivityPubRepository
{
    // --- Accounts ---

    public async Task<ApAccount?> GetAccountByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.ApAccounts.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<ApAccount?> GetAccountByApIdAsync(string apId, CancellationToken ct = default)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apId));
        return await db.ApAccounts.FirstOrDefaultAsync(a => a.ApIdHash == hash, ct);
    }

    public async Task<ApAccount?> GetAccountByUsernameAndDomainAsync(string username, string domain, CancellationToken ct = default)
    {
        return await db.ApAccounts
            .FirstOrDefaultAsync(a => a.Username == username && a.Domain == domain, ct);
    }

    public async Task<ApAccount> AddAccountAsync(ApAccount account, CancellationToken ct = default)
    {
        db.ApAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account;
    }

    public async Task UpdateAccountAsync(ApAccount account, CancellationToken ct = default)
    {
        account.UpdatedAt = DateTime.UtcNow;
        db.ApAccounts.Update(account);
        await db.SaveChangesAsync(ct);
    }

    // --- Local user ---

    public async Task<ApUser?> GetLocalUserAsync(CancellationToken ct = default)
    {
        return await db.ApUsers
            .Include(u => u.Account)
            .Include(u => u.Site)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ApUser?> GetUserByAccountIdAsync(int accountId, CancellationToken ct = default)
    {
        return await db.ApUsers
            .Include(u => u.Account)
            .Include(u => u.Site)
            .FirstOrDefaultAsync(u => u.AccountId == accountId, ct);
    }

    // --- Follows ---

    public async Task<ApFollow?> GetFollowAsync(int followerId, int followingId, CancellationToken ct = default)
    {
        return await db.ApFollows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, ct);
    }

    public async Task<ApFollow> AddFollowAsync(ApFollow follow, CancellationToken ct = default)
    {
        db.ApFollows.Add(follow);
        await db.SaveChangesAsync(ct);
        return follow;
    }

    public async Task RemoveFollowAsync(int followerId, int followingId, CancellationToken ct = default)
    {
        var follow = await db.ApFollows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId, ct);
        if (follow is not null)
        {
            db.ApFollows.Remove(follow);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<PagedResult<ApAccount>> GetFollowersAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ApFollows
            .Where(f => f.FollowingId == accountId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Follower);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ApAccount>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResult<ApAccount>> GetFollowingAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ApFollows
            .Where(f => f.FollowerId == accountId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Following);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ApAccount>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<int> GetFollowerCountAsync(int accountId, CancellationToken ct = default)
    {
        return await db.ApFollows.CountAsync(f => f.FollowingId == accountId, ct);
    }

    public async Task<int> GetFollowingCountAsync(int accountId, CancellationToken ct = default)
    {
        return await db.ApFollows.CountAsync(f => f.FollowerId == accountId, ct);
    }

    // --- Posts ---

    public async Task<ApPost?> GetPostByIdAsync(int id, CancellationToken ct = default)
    {
        return await db.ApPosts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<ApPost?> GetPostByApIdAsync(string apId, CancellationToken ct = default)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(apId));
        return await db.ApPosts
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.ApIdHash == hash, ct);
    }

    public async Task<ApPost> AddPostAsync(ApPost post, CancellationToken ct = default)
    {
        db.ApPosts.Add(post);
        await db.SaveChangesAsync(ct);
        return post;
    }

    public async Task UpdatePostAsync(ApPost post, CancellationToken ct = default)
    {
        post.UpdatedAt = DateTime.UtcNow;
        db.ApPosts.Update(post);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<ApPost>> GetOutboxPostsAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ApPosts
            .Include(p => p.Author)
            .Where(p => p.AuthorId == accountId && p.DeletedAt == null)
            .OrderByDescending(p => p.PublishedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ApPost>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // --- Outbox entries ---

    public async Task<ApOutbox> AddOutboxEntryAsync(ApOutbox entry, CancellationToken ct = default)
    {
        db.ApOutboxes.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task<PagedResult<ApOutbox>> GetOutboxEntriesAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ApOutboxes
            .Include(o => o.Post).ThenInclude(p => p.Author)
            .Where(o => o.AccountId == accountId)
            .OrderByDescending(o => o.PublishedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ApOutbox>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    // --- Likes ---

    public async Task<ApLike?> GetLikeAsync(int accountId, int postId, CancellationToken ct = default)
    {
        return await db.ApLikes
            .FirstOrDefaultAsync(l => l.AccountId == accountId && l.PostId == postId, ct);
    }

    public async Task<ApLike> AddLikeAsync(ApLike like, CancellationToken ct = default)
    {
        db.ApLikes.Add(like);
        await db.SaveChangesAsync(ct);
        return like;
    }

    public async Task RemoveLikeAsync(int accountId, int postId, CancellationToken ct = default)
    {
        var like = await db.ApLikes
            .FirstOrDefaultAsync(l => l.AccountId == accountId && l.PostId == postId, ct);
        if (like is not null)
        {
            db.ApLikes.Remove(like);
            await db.SaveChangesAsync(ct);
        }
    }

    // --- Blocks ---

    public async Task<bool> IsBlockedAsync(int blockerId, int blockedId, CancellationToken ct = default)
    {
        return await db.ApBlocks
            .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, ct);
    }

    public async Task<bool> IsDomainBlockedAsync(int blockerId, string domain, CancellationToken ct = default)
    {
        return await db.ApDomainBlocks
            .AnyAsync(b => b.BlockerId == blockerId && b.Domain == domain, ct);
    }

    public async Task<ApBlock> AddBlockAsync(ApBlock block, CancellationToken ct = default)
    {
        db.ApBlocks.Add(block);
        await db.SaveChangesAsync(ct);
        return block;
    }

    public async Task RemoveBlockAsync(int blockerId, int blockedId, CancellationToken ct = default)
    {
        var block = await db.ApBlocks
            .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId, ct);
        if (block is not null)
        {
            db.ApBlocks.Remove(block);
            await db.SaveChangesAsync(ct);
        }
    }

    // --- Ghost ↔ AP post mapping ---

    public async Task<ApGhostApPostMapping?> GetPostMappingByGhostUuidAsync(string ghostUuid, CancellationToken ct = default)
    {
        return await db.ApGhostApPostMappings
            .FirstOrDefaultAsync(m => m.GhostUuid == ghostUuid, ct);
    }

    public async Task<ApGhostApPostMapping> AddPostMappingAsync(ApGhostApPostMapping mapping, CancellationToken ct = default)
    {
        db.ApGhostApPostMappings.Add(mapping);
        await db.SaveChangesAsync(ct);
        return mapping;
    }

    // --- Delivery backoffs ---

    public async Task<ApAccountDeliveryBackoff?> GetDeliveryBackoffAsync(int accountId, CancellationToken ct = default)
    {
        return await db.ApAccountDeliveryBackoffs
            .FirstOrDefaultAsync(b => b.AccountId == accountId, ct);
    }

    public async Task UpsertDeliveryBackoffAsync(ApAccountDeliveryBackoff backoff, CancellationToken ct = default)
    {
        var existing = await db.ApAccountDeliveryBackoffs
            .FirstOrDefaultAsync(b => b.AccountId == backoff.AccountId, ct);

        if (existing is not null)
        {
            existing.LastFailureAt = backoff.LastFailureAt;
            existing.LastFailureReason = backoff.LastFailureReason;
            existing.BackoffUntil = backoff.BackoffUntil;
            existing.BackoffSeconds = backoff.BackoffSeconds;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.ApAccountDeliveryBackoffs.Add(backoff);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveDeliveryBackoffAsync(int accountId, CancellationToken ct = default)
    {
        var backoff = await db.ApAccountDeliveryBackoffs
            .FirstOrDefaultAsync(b => b.AccountId == accountId, ct);
        if (backoff is not null)
        {
            db.ApAccountDeliveryBackoffs.Remove(backoff);
            await db.SaveChangesAsync(ct);
        }
    }

    // --- Notifications ---

    public async Task<PagedResult<ApNotification>> GetNotificationsAsync(int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ApNotifications
            .Include(n => n.Account)
            .Include(n => n.Post)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.Id);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<ApNotification>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task MarkNotificationsReadAsync(int userId, CancellationToken ct = default)
    {
        await db.ApNotifications
            .Where(n => n.UserId == userId && !n.Read)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.Read, true), ct);
    }

    // --- Site ---

    public async Task<ApSite?> GetSiteByHostAsync(string host, CancellationToken ct = default)
    {
        return await db.ApSites.FirstOrDefaultAsync(s => s.Host == host, ct);
    }

    public async Task<ApSite> AddSiteAsync(ApSite site, CancellationToken ct = default)
    {
        db.ApSites.Add(site);
        await db.SaveChangesAsync(ct);
        return site;
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
