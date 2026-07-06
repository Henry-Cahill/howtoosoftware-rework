using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using HowToSoftware.Core.Entities;
using HowToSoftware.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace HowToSoftware.Infrastructure.Services;

public class ActivityPubService(
    IActivityPubRepository repo,
    IActivityPubHttpClient httpClient,
    ISettingsRepository settingsRepo,
    ILogger<ActivityPubService> logger) : IActivityPubService
{
    // --- Account discovery ---

    public async Task<ApAccount?> GetLocalActorAsync(CancellationToken ct = default)
    {
        var user = await repo.GetLocalUserAsync(ct);
        return user?.Account;
    }

    public async Task<WebFingerResult?> HandleWebFingerAsync(string resource, CancellationToken ct = default)
    {
        // resource = "acct:username@domain"
        if (!resource.StartsWith("acct:", StringComparison.OrdinalIgnoreCase))
            return null;

        var acct = resource[5..];
        var parts = acct.Split('@', 2);
        if (parts.Length != 2) return null;

        var (username, domain) = (parts[0], parts[1]);

        var account = await repo.GetAccountByUsernameAndDomainAsync(username, domain, ct);
        if (account is null) return null;

        // Verify this is a local user
        var user = await repo.GetUserByAccountIdAsync(account.Id, ct);
        if (user is null) return null;

        return new WebFingerResult
        {
            Subject = resource,
            Links =
            [
                new WebFingerLink
                {
                    Rel = "self",
                    Type = "application/activity+json",
                    Href = account.ApId
                }
            ]
        };
    }

    // --- Follow / Unfollow ---

    public async Task HandleFollowAsync(string actorApId, string objectApId, CancellationToken ct = default)
    {
        var follower = await ResolveOrFetchAccountAsync(actorApId, ct);
        if (follower is null)
        {
            logger.LogWarning("Cannot resolve follower actor: {ApId}", actorApId);
            return;
        }

        var following = await repo.GetAccountByApIdAsync(objectApId, ct);
        if (following is null)
        {
            logger.LogWarning("Follow target not found: {ApId}", objectApId);
            return;
        }

        // Check blocks
        if (await repo.IsBlockedAsync(following.Id, follower.Id, ct))
        {
            logger.LogInformation("Follow rejected: {Follower} is blocked by {Following}", actorApId, objectApId);
            return;
        }

        if (await repo.IsDomainBlockedAsync(following.Id, follower.Domain, ct))
        {
            logger.LogInformation("Follow rejected: domain {Domain} is blocked", follower.Domain);
            return;
        }

        // Check existing follow
        var existing = await repo.GetFollowAsync(follower.Id, following.Id, ct);
        if (existing is not null) return;

        await repo.AddFollowAsync(new ApFollow
        {
            FollowerId = follower.Id,
            FollowingId = following.Id
        }, ct);

        // Send Accept activity
        await SendAcceptAsync(following, follower, actorApId, ct);
    }

    public async Task HandleUndoFollowAsync(string actorApId, string objectApId, CancellationToken ct = default)
    {
        var follower = await repo.GetAccountByApIdAsync(actorApId, ct);
        if (follower is null) return;

        var following = await repo.GetAccountByApIdAsync(objectApId, ct);
        if (following is null) return;

        await repo.RemoveFollowAsync(follower.Id, following.Id, ct);
    }

    public Task<PagedResult<ApAccount>> GetFollowersAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
        => repo.GetFollowersAsync(accountId, page, pageSize, ct);

    public Task<PagedResult<ApAccount>> GetFollowingAsync(int accountId, int page, int pageSize, CancellationToken ct = default)
        => repo.GetFollowingAsync(accountId, page, pageSize, ct);

    // --- Inbox processing ---

    public async Task ProcessInboxAsync(string body, string? signature, string? signatureInput, string host, CancellationToken ct = default)
    {
        JsonNode? activity;
        try
        {
            activity = JsonNode.Parse(body);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid JSON in inbox");
            return;
        }

        if (activity is null) return;

        var type = activity["type"]?.GetValue<string>();
        var actorId = activity["actor"]?.GetValue<string>();

        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(actorId))
        {
            logger.LogWarning("Inbox activity missing type or actor");
            return;
        }

        switch (type)
        {
            case "Follow":
                var followObject = activity["object"]?.GetValue<string>();
                if (followObject is not null)
                    await HandleFollowAsync(actorId, followObject, ct);
                break;

            case "Undo":
                await HandleUndoActivityAsync(activity, actorId, ct);
                break;

            case "Like":
                await HandleLikeAsync(activity, actorId, ct);
                break;

            case "Announce":
                await HandleAnnounceAsync(activity, actorId, ct);
                break;

            case "Create":
                await HandleCreateAsync(activity, actorId, ct);
                break;

            case "Delete":
                await HandleDeleteAsync(activity, actorId, ct);
                break;

            case "Update":
                await HandleUpdateAsync(activity, actorId, ct);
                break;

            default:
                logger.LogInformation("Unhandled activity type: {Type}", type);
                break;
        }
    }

    // --- Outbox / publish to fediverse ---

    public async Task<ApPost> PublishPostAsync(string ghostPostUuid, string title, string content, string url, string? imageUrl, CancellationToken ct = default)
    {
        var localActor = await GetLocalActorAsync(ct);
        if (localActor is null)
            throw new InvalidOperationException("Local actor not configured. Run ActivityPub setup first.");

        var siteUrl = await GetSiteUrlAsync(ct);
        var apId = $"{siteUrl}/activitypub/post/{ghostPostUuid}";

        var post = new ApPost
        {
            Uuid = ghostPostUuid,
            Type = 0, // Article
            Audience = 0, // Public
            AuthorId = localActor.Id,
            Title = title,
            Content = content,
            Url = url,
            ImageUrl = imageUrl,
            ApId = apId,
            PublishedAt = DateTime.UtcNow
        };

        post = await repo.AddPostAsync(post, ct);

        await repo.AddPostMappingAsync(new ApGhostApPostMapping
        {
            GhostUuid = ghostPostUuid,
            ApId = apId
        }, ct);

        // Add outbox entry
        await repo.AddOutboxEntryAsync(new ApOutbox
        {
            AccountId = localActor.Id,
            PostId = post.Id,
            AuthorId = localActor.Id,
            PostType = 0,
            OutboxType = 0, // Create
            PublishedAt = DateTime.UtcNow
        }, ct);

        // Deliver Create activity to followers
        await DeliverToFollowersAsync(localActor, BuildCreateActivity(localActor, post, siteUrl), ct);

        return post;
    }

    public async Task DeletePostAsync(string ghostPostUuid, CancellationToken ct = default)
    {
        var mapping = await repo.GetPostMappingByGhostUuidAsync(ghostPostUuid, ct);
        if (mapping is null) return;

        var post = await repo.GetPostByApIdAsync(mapping.ApId, ct);
        if (post is null) return;

        post.DeletedAt = DateTime.UtcNow;
        await repo.UpdatePostAsync(post, ct);

        var localActor = await GetLocalActorAsync(ct);
        if (localActor is null) return;

        var siteUrl = await GetSiteUrlAsync(ct);
        var deleteActivity = BuildDeleteActivity(localActor, post, siteUrl);
        await DeliverToFollowersAsync(localActor, deleteActivity, ct);
    }

    // --- Outbox collection ---

    public async Task<ActivityPubCollection> GetOutboxAsync(string username, int page, int pageSize, CancellationToken ct = default)
    {
        var localActor = await GetLocalActorAsync(ct);
        if (localActor is null || localActor.Username != username)
            return new ActivityPubCollection { TotalItems = 0 };

        var siteUrl = await GetSiteUrlAsync(ct);
        var result = await repo.GetOutboxEntriesAsync(localActor.Id, page, pageSize, ct);

        var items = result.Items.Select(entry => (object)new
        {
            Type = "Create",
            actor = localActor.ApId,
            published = entry.PublishedAt?.ToString("o"),
            @object = BuildNoteObject(localActor, entry.Post, siteUrl)
        }).ToList();

        return new ActivityPubCollection
        {
            Id = $"{siteUrl}/activitypub/users/{username}/outbox",
            Type = page > 0 ? "OrderedCollectionPage" : "OrderedCollection",
            TotalItems = result.TotalCount,
            OrderedItems = page > 0 ? items : null,
            First = $"{siteUrl}/activitypub/users/{username}/outbox?page=1"
        };
    }

    // --- Remote account resolution ---

    public async Task<ApAccount?> ResolveRemoteAccountAsync(string apId, CancellationToken ct = default)
    {
        return await ResolveOrFetchAccountAsync(apId, ct);
    }

    // --- Private helpers ---

    private async Task<ApAccount?> ResolveOrFetchAccountAsync(string apId, CancellationToken ct)
    {
        var existing = await repo.GetAccountByApIdAsync(apId, ct);
        if (existing is not null) return existing;

        var json = await httpClient.FetchActorAsync(apId, ct);
        if (json is null) return null;

        return await ParseAndStoreRemoteAccount(json, ct);
    }

    private async Task<ApAccount?> ParseAndStoreRemoteAccount(string json, CancellationToken ct)
    {
        JsonNode? actor;
        try { actor = JsonNode.Parse(json); }
        catch { return null; }

        if (actor is null) return null;

        var id = actor["id"]?.GetValue<string>();
        var inbox = actor["inbox"]?.GetValue<string>();
        var preferredUsername = actor["preferredUsername"]?.GetValue<string>();

        if (id is null || inbox is null || preferredUsername is null) return null;

        var domain = new Uri(id).Host;

        var account = new ApAccount
        {
            Username = preferredUsername,
            Name = actor["name"]?.GetValue<string>(),
            Bio = actor["summary"]?.GetValue<string>(),
            ApId = id,
            ApInboxUrl = inbox,
            ApSharedInboxUrl = actor["endpoints"]?["sharedInbox"]?.GetValue<string>(),
            ApOutboxUrl = actor["outbox"]?.GetValue<string>(),
            ApFollowingUrl = actor["following"]?.GetValue<string>(),
            ApFollowersUrl = actor["followers"]?.GetValue<string>(),
            Domain = domain,
            Url = actor["url"]?.GetValue<string>(),
            AvatarUrl = actor["icon"]?["url"]?.GetValue<string>(),
            BannerImageUrl = actor["image"]?["url"]?.GetValue<string>(),
            ApPublicKey = actor["publicKey"]?["publicKeyPem"]?.GetValue<string>()
        };

        return await repo.AddAccountAsync(account, ct);
    }

    private async Task SendAcceptAsync(ApAccount localAccount, ApAccount remoteAccount, string followActivityId, CancellationToken ct)
    {
        if (localAccount.ApPrivateKey is null)
        {
            logger.LogWarning("Cannot send Accept: local actor missing private key");
            return;
        }

        var siteUrl = await GetSiteUrlAsync(ct);
        var acceptActivity = JsonSerializer.Serialize(new
        {
            @context = "https://www.w3.org/ns/activitystreams",
            type = "Accept",
            actor = localAccount.ApId,
            @object = new
            {
                type = "Follow",
                actor = remoteAccount.ApId,
                @object = localAccount.ApId
            }
        });

        var targetInbox = remoteAccount.ApInboxUrl;
        var keyId = $"{localAccount.ApId}#main-key";

        await httpClient.DeliverAsync(targetInbox, acceptActivity, keyId, localAccount.ApPrivateKey, ct);
    }

    private async Task DeliverToFollowersAsync(ApAccount actor, string activity, CancellationToken ct)
    {
        if (actor.ApPrivateKey is null)
        {
            logger.LogWarning("Cannot deliver: actor {ApId} missing private key", actor.ApId);
            return;
        }

        var keyId = $"{actor.ApId}#main-key";
        var page = 1;
        const int batchSize = 50;
        var deliveredInboxes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (true)
        {
            var followers = await repo.GetFollowersAsync(actor.Id, page, batchSize, ct);
            if (followers.Items.Count == 0) break;

            foreach (var follower in followers.Items)
            {
                // Prefer shared inbox for efficiency
                var inbox = follower.ApSharedInboxUrl ?? follower.ApInboxUrl;
                if (!deliveredInboxes.Add(inbox)) continue;

                // Check backoff
                var backoff = await repo.GetDeliveryBackoffAsync(follower.Id, ct);
                if (backoff is not null && backoff.BackoffUntil > DateTime.UtcNow) continue;

                var success = await httpClient.DeliverAsync(inbox, activity, keyId, actor.ApPrivateKey, ct);

                if (!success)
                {
                    await repo.UpsertDeliveryBackoffAsync(new ApAccountDeliveryBackoff
                    {
                        AccountId = follower.Id,
                        LastFailureAt = DateTime.UtcNow,
                        LastFailureReason = "Delivery failed",
                        BackoffUntil = DateTime.UtcNow.AddMinutes(5),
                        BackoffSeconds = 300
                    }, ct);
                }
                else
                {
                    await repo.RemoveDeliveryBackoffAsync(follower.Id, ct);
                }
            }

            if (!followers.HasNextPage) break;
            page++;
        }
    }

    private static string BuildCreateActivity(ApAccount actor, ApPost post, string siteUrl)
    {
        return JsonSerializer.Serialize(new
        {
            @context = "https://www.w3.org/ns/activitystreams",
            id = $"{siteUrl}/activitypub/activity/create/{post.Uuid}",
            type = "Create",
            actor = actor.ApId,
            published = post.PublishedAt.ToString("o"),
            to = new[] { "https://www.w3.org/ns/activitystreams#Public" },
            cc = new[] { actor.ApFollowersUrl },
            @object = BuildNoteObject(actor, post, siteUrl)
        });
    }

    private static string BuildDeleteActivity(ApAccount actor, ApPost post, string siteUrl)
    {
        return JsonSerializer.Serialize(new
        {
            @context = "https://www.w3.org/ns/activitystreams",
            id = $"{siteUrl}/activitypub/activity/delete/{post.Uuid}",
            type = "Delete",
            actor = actor.ApId,
            @object = post.ApId,
            to = new[] { "https://www.w3.org/ns/activitystreams#Public" }
        });
    }

    private static object BuildNoteObject(ApAccount actor, ApPost post, string siteUrl)
    {
        return new
        {
            id = post.ApId,
            type = post.Type == 0 ? "Article" : "Note",
            attributedTo = actor.ApId,
            content = post.Content,
            name = post.Title,
            url = post.Url,
            published = post.PublishedAt.ToString("o"),
            to = new[] { "https://www.w3.org/ns/activitystreams#Public" },
            cc = new[] { actor.ApFollowersUrl },
            summary = post.Summary
        };
    }

    private async Task HandleUndoActivityAsync(JsonNode activity, string actorId, CancellationToken ct)
    {
        var inner = activity["object"];
        if (inner is null) return;

        var innerType = inner["type"]?.GetValue<string>();
        switch (innerType)
        {
            case "Follow":
                var followObject = inner["object"]?.GetValue<string>();
                if (followObject is not null)
                    await HandleUndoFollowAsync(actorId, followObject, ct);
                break;

            case "Like":
                await HandleUndoLikeAsync(inner, actorId, ct);
                break;

            case "Announce":
                await HandleUndoAnnounceAsync(inner, actorId, ct);
                break;
        }
    }

    private async Task HandleLikeAsync(JsonNode activity, string actorId, CancellationToken ct)
    {
        var objectId = activity["object"]?.GetValue<string>();
        if (objectId is null) return;

        var account = await ResolveOrFetchAccountAsync(actorId, ct);
        if (account is null) return;

        var post = await repo.GetPostByApIdAsync(objectId, ct);
        if (post is null) return;

        var existing = await repo.GetLikeAsync(account.Id, post.Id, ct);
        if (existing is not null) return;

        await repo.AddLikeAsync(new ApLike { AccountId = account.Id, PostId = post.Id }, ct);

        post.LikeCount++;
        await repo.UpdatePostAsync(post, ct);
    }

    private async Task HandleUndoLikeAsync(JsonNode inner, string actorId, CancellationToken ct)
    {
        var objectId = inner["object"]?.GetValue<string>();
        if (objectId is null) return;

        var account = await repo.GetAccountByApIdAsync(actorId, ct);
        if (account is null) return;

        var post = await repo.GetPostByApIdAsync(objectId, ct);
        if (post is null) return;

        await repo.RemoveLikeAsync(account.Id, post.Id, ct);

        if (post.LikeCount > 0) post.LikeCount--;
        await repo.UpdatePostAsync(post, ct);
    }

    private async Task HandleAnnounceAsync(JsonNode activity, string actorId, CancellationToken ct)
    {
        var objectId = activity["object"]?.GetValue<string>();
        if (objectId is null) return;

        var account = await ResolveOrFetchAccountAsync(actorId, ct);
        if (account is null) return;

        var post = await repo.GetPostByApIdAsync(objectId, ct);
        if (post is null) return;

        post.RepostCount++;
        await repo.UpdatePostAsync(post, ct);
    }

    private async Task HandleUndoAnnounceAsync(JsonNode inner, string actorId, CancellationToken ct)
    {
        var objectId = inner["object"]?.GetValue<string>();
        if (objectId is null) return;

        var account = await repo.GetAccountByApIdAsync(actorId, ct);
        if (account is null) return;

        var post = await repo.GetPostByApIdAsync(objectId, ct);
        if (post is null) return;

        if (post.RepostCount > 0) post.RepostCount--;
        await repo.UpdatePostAsync(post, ct);
    }

    private async Task HandleCreateAsync(JsonNode activity, string actorId, CancellationToken ct)
    {
        var obj = activity["object"];
        if (obj is null) return;

        var account = await ResolveOrFetchAccountAsync(actorId, ct);
        if (account is null) return;

        var apId = obj["id"]?.GetValue<string>();
        if (apId is null) return;

        // Check if post already exists
        var existing = await repo.GetPostByApIdAsync(apId, ct);
        if (existing is not null) return;

        var inReplyTo = obj["inReplyTo"]?.GetValue<string>();
        int? inReplyToId = null;
        if (inReplyTo is not null)
        {
            var replyPost = await repo.GetPostByApIdAsync(inReplyTo, ct);
            inReplyToId = replyPost?.Id;
        }

        var objectType = obj["type"]?.GetValue<string>();

        await repo.AddPostAsync(new ApPost
        {
            Type = objectType == "Article" ? (byte)0 : (byte)1,
            Audience = 0,
            AuthorId = account.Id,
            Title = obj["name"]?.GetValue<string>(),
            Content = obj["content"]?.GetValue<string>(),
            Summary = obj["summary"]?.GetValue<string>(),
            Url = obj["url"]?.GetValue<string>() ?? apId,
            ApId = apId,
            PublishedAt = DateTime.UtcNow,
            InReplyToId = inReplyToId
        }, ct);
    }

    private async Task HandleDeleteAsync(JsonNode activity, string actorId, CancellationToken ct)
    {
        var objectId = activity["object"]?.GetValue<string>();
        if (objectId is null)
            objectId = activity["object"]?["id"]?.GetValue<string>();
        if (objectId is null) return;

        var account = await repo.GetAccountByApIdAsync(actorId, ct);
        if (account is null) return;

        var post = await repo.GetPostByApIdAsync(objectId, ct);
        if (post is null || post.AuthorId != account.Id) return;

        post.DeletedAt = DateTime.UtcNow;
        await repo.UpdatePostAsync(post, ct);
    }

    private async Task HandleUpdateAsync(JsonNode activity, string actorId, CancellationToken ct)
    {
        var obj = activity["object"];
        if (obj is null) return;

        var apId = obj["id"]?.GetValue<string>();
        if (apId is null) return;

        var account = await repo.GetAccountByApIdAsync(actorId, ct);
        if (account is null) return;

        var post = await repo.GetPostByApIdAsync(apId, ct);
        if (post is null || post.AuthorId != account.Id) return;

        post.Title = obj["name"]?.GetValue<string>() ?? post.Title;
        post.Content = obj["content"]?.GetValue<string>() ?? post.Content;
        post.Summary = obj["summary"]?.GetValue<string>() ?? post.Summary;
        await repo.UpdatePostAsync(post, ct);
    }

    private async Task<string> GetSiteUrlAsync(CancellationToken ct)
    {
        var setting = await settingsRepo.GetByKeyAsync("url", ct);
        return setting?.Value?.TrimEnd('/') ?? "https://howtoosoftware.com";
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
