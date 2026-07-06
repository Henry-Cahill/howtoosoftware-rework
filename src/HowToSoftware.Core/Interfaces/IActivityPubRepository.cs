using HowToSoftware.Core.Entities;

namespace HowToSoftware.Core.Interfaces;

public interface IActivityPubRepository
{
    // Accounts
    Task<ApAccount?> GetAccountByIdAsync(int id, CancellationToken ct = default);
    Task<ApAccount?> GetAccountByApIdAsync(string apId, CancellationToken ct = default);
    Task<ApAccount?> GetAccountByUsernameAndDomainAsync(string username, string domain, CancellationToken ct = default);
    Task<ApAccount> AddAccountAsync(ApAccount account, CancellationToken ct = default);
    Task UpdateAccountAsync(ApAccount account, CancellationToken ct = default);

    // Local user
    Task<ApUser?> GetLocalUserAsync(CancellationToken ct = default);
    Task<ApUser?> GetUserByAccountIdAsync(int accountId, CancellationToken ct = default);

    // Follows
    Task<ApFollow?> GetFollowAsync(int followerId, int followingId, CancellationToken ct = default);
    Task<ApFollow> AddFollowAsync(ApFollow follow, CancellationToken ct = default);
    Task RemoveFollowAsync(int followerId, int followingId, CancellationToken ct = default);
    Task<PagedResult<ApAccount>> GetFollowersAsync(int accountId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<ApAccount>> GetFollowingAsync(int accountId, int page, int pageSize, CancellationToken ct = default);
    Task<int> GetFollowerCountAsync(int accountId, CancellationToken ct = default);
    Task<int> GetFollowingCountAsync(int accountId, CancellationToken ct = default);

    // Posts
    Task<ApPost?> GetPostByIdAsync(int id, CancellationToken ct = default);
    Task<ApPost?> GetPostByApIdAsync(string apId, CancellationToken ct = default);
    Task<ApPost> AddPostAsync(ApPost post, CancellationToken ct = default);
    Task UpdatePostAsync(ApPost post, CancellationToken ct = default);
    Task<PagedResult<ApPost>> GetOutboxPostsAsync(int accountId, int page, int pageSize, CancellationToken ct = default);

    // Outbox entries
    Task<ApOutbox> AddOutboxEntryAsync(ApOutbox entry, CancellationToken ct = default);
    Task<PagedResult<ApOutbox>> GetOutboxEntriesAsync(int accountId, int page, int pageSize, CancellationToken ct = default);

    // Likes
    Task<ApLike?> GetLikeAsync(int accountId, int postId, CancellationToken ct = default);
    Task<ApLike> AddLikeAsync(ApLike like, CancellationToken ct = default);
    Task RemoveLikeAsync(int accountId, int postId, CancellationToken ct = default);

    // Blocks
    Task<bool> IsBlockedAsync(int blockerId, int blockedId, CancellationToken ct = default);
    Task<bool> IsDomainBlockedAsync(int blockerId, string domain, CancellationToken ct = default);
    Task<ApBlock> AddBlockAsync(ApBlock block, CancellationToken ct = default);
    Task RemoveBlockAsync(int blockerId, int blockedId, CancellationToken ct = default);

    // Ghost ↔ AP post mapping
    Task<ApGhostApPostMapping?> GetPostMappingByGhostUuidAsync(string ghostUuid, CancellationToken ct = default);
    Task<ApGhostApPostMapping> AddPostMappingAsync(ApGhostApPostMapping mapping, CancellationToken ct = default);

    // Delivery backoffs
    Task<ApAccountDeliveryBackoff?> GetDeliveryBackoffAsync(int accountId, CancellationToken ct = default);
    Task UpsertDeliveryBackoffAsync(ApAccountDeliveryBackoff backoff, CancellationToken ct = default);
    Task RemoveDeliveryBackoffAsync(int accountId, CancellationToken ct = default);

    // Notifications
    Task<PagedResult<ApNotification>> GetNotificationsAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task MarkNotificationsReadAsync(int userId, CancellationToken ct = default);

    // Site
    Task<ApSite?> GetSiteByHostAsync(string host, CancellationToken ct = default);
    Task<ApSite> AddSiteAsync(ApSite site, CancellationToken ct = default);
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
