using Microsoft.AspNetCore.Mvc;
using HowToSoftware.Core.Interfaces;

namespace HowToSoftware.Web.Controllers;

/// <summary>
/// ActivityPub Inbox/Outbox controllers and actor endpoints.
/// Implements https://www.w3.org/TR/activitypub/
/// </summary>
[ApiController]
[Route("activitypub")]
public class ActivityPubController(IActivityPubService apService) : ControllerBase
{
    private const string ActivityPubMediaType = "application/activity+json";

    /// <summary>
    /// GET /activitypub/users/{username} — Actor profile
    /// </summary>
    [HttpGet("users/{username}")]
    [Produces(ActivityPubMediaType)]
    public async Task<IActionResult> GetActor(string username, CancellationToken ct = default)
    {
        var actor = await apService.GetLocalActorAsync(ct);
        if (actor is null || actor.Username != username)
            return NotFound();

        return Ok(new
        {
            @context = new object[]
            {
                "https://www.w3.org/ns/activitystreams",
                "https://w3id.org/security/v1"
            },
            id = actor.ApId,
            type = "Person",
            preferredUsername = actor.Username,
            name = actor.Name,
            summary = actor.Bio,
            url = actor.Url,
            inbox = actor.ApInboxUrl,
            outbox = actor.ApOutboxUrl,
            following = actor.ApFollowingUrl,
            followers = actor.ApFollowersUrl,
            icon = actor.AvatarUrl is not null ? new { type = "Image", url = actor.AvatarUrl } : null,
            image = actor.BannerImageUrl is not null ? new { type = "Image", url = actor.BannerImageUrl } : null,
            publicKey = actor.ApPublicKey is not null ? new
            {
                id = $"{actor.ApId}#main-key",
                owner = actor.ApId,
                publicKeyPem = actor.ApPublicKey
            } : null
        });
    }

    /// <summary>
    /// POST /activitypub/users/{username}/inbox — Federated inbox
    /// Receives Follow, Undo, Like, Create, Delete, Update, Announce activities.
    /// </summary>
    [HttpPost("users/{username}/inbox")]
    public async Task<IActionResult> Inbox(string username, CancellationToken ct = default)
    {
        var actor = await apService.GetLocalActorAsync(ct);
        if (actor is null || actor.Username != username)
            return NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(body))
            return BadRequest();

        var signature = Request.Headers["Signature"].FirstOrDefault();
        var signatureInput = Request.Headers["Signature-Input"].FirstOrDefault();

        await apService.ProcessInboxAsync(body, signature, signatureInput, Request.Host.Value ?? string.Empty, ct);

        return Accepted();
    }

    /// <summary>
    /// POST /activitypub/inbox — Shared inbox
    /// </summary>
    [HttpPost("inbox")]
    public async Task<IActionResult> SharedInbox(CancellationToken ct = default)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        if (string.IsNullOrWhiteSpace(body))
            return BadRequest();

        var signature = Request.Headers["Signature"].FirstOrDefault();
        var signatureInput = Request.Headers["Signature-Input"].FirstOrDefault();

        await apService.ProcessInboxAsync(body, signature, signatureInput, Request.Host.Value ?? string.Empty, ct);

        return Accepted();
    }

    /// <summary>
    /// GET /activitypub/users/{username}/outbox — Outbox collection
    /// </summary>
    [HttpGet("users/{username}/outbox")]
    [Produces(ActivityPubMediaType)]
    public async Task<IActionResult> Outbox(
        string username,
        [FromQuery] int page = 0,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var collection = await apService.GetOutboxAsync(username, page, limit, ct);

        return Ok(new
        {
            @context = "https://www.w3.org/ns/activitystreams",
            id = collection.Id,
            type = collection.Type,
            totalItems = collection.TotalItems,
            first = collection.First,
            orderedItems = collection.OrderedItems
        });
    }

    /// <summary>
    /// GET /activitypub/users/{username}/followers — Followers collection
    /// </summary>
    [HttpGet("users/{username}/followers")]
    [Produces(ActivityPubMediaType)]
    public async Task<IActionResult> Followers(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var actor = await apService.GetLocalActorAsync(ct);
        if (actor is null || actor.Username != username)
            return NotFound();

        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var result = await apService.GetFollowersAsync(actor.Id, page, limit, ct);

        return Ok(new
        {
            @context = "https://www.w3.org/ns/activitystreams",
            id = actor.ApFollowersUrl,
            type = "OrderedCollection",
            totalItems = result.TotalCount,
            orderedItems = result.Items.Select(a => a.ApId)
        });
    }

    /// <summary>
    /// GET /activitypub/users/{username}/following — Following collection
    /// </summary>
    [HttpGet("users/{username}/following")]
    [Produces(ActivityPubMediaType)]
    public async Task<IActionResult> Following(
        string username,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var actor = await apService.GetLocalActorAsync(ct);
        if (actor is null || actor.Username != username)
            return NotFound();

        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var result = await apService.GetFollowingAsync(actor.Id, page, limit, ct);

        return Ok(new
        {
            @context = "https://www.w3.org/ns/activitystreams",
            id = actor.ApFollowingUrl,
            type = "OrderedCollection",
            totalItems = result.TotalCount,
            orderedItems = result.Items.Select(a => a.ApId)
        });
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
