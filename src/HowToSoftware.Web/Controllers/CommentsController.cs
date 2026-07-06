using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HowToSoftware.Core.Interfaces;
using HowToSoftware.Web.Controllers;

namespace HowToSoftware.Web.Controllers;

[ApiController]
[Route("api/comments")]
public class CommentsController(ICommentService commentService) : ControllerBase
{
    /// <summary>
    /// GET /api/comments/post/{postId}?page=1&amp;limit=20
    /// Returns top-level comments for a post with their replies.
    /// </summary>
    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetComments(
        string postId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;

        var result = await commentService.GetCommentsForPostAsync(postId, page, limit, ct);
        var memberId = GetMemberId();

        // Eagerly load replies for each top-level comment
        var commentResources = new List<CommentResource>();
        foreach (var c in result.Items)
        {
            var replies = await commentService.GetRepliesAsync(c.Id, ct);
            commentResources.Add(ToResource(c, memberId, replies.Select(r => ToResource(r, memberId)).ToList()));
        }

        return Ok(new
        {
            comments = commentResources,
            meta = new
            {
                pagination = new
                {
                    page = result.Page,
                    limit = result.PageSize,
                    pages = result.TotalPages,
                    total = result.TotalCount,
                }
            }
        });
    }

    /// <summary>
    /// GET /api/comments/post/{postId}/count
    /// </summary>
    [HttpGet("post/{postId}/count")]
    public async Task<IActionResult> GetCount(string postId, CancellationToken ct = default)
    {
        var count = await commentService.GetCommentCountAsync(postId, ct);
        return Ok(new { count });
    }

    /// <summary>
    /// POST /api/comments  — add a new comment (authenticated members only).
    /// </summary>
    [HttpPost]
    [Authorize(AuthenticationSchemes = MemberAuthController.MemberCookieScheme)]
    public async Task<IActionResult> AddComment(
        [FromBody] AddCommentDto dto, CancellationToken ct = default)
    {
        var memberId = GetMemberId();
        if (memberId is null)
            return Unauthorized(new { error = "Sign in to comment." });

        try
        {
            var comment = await commentService.AddCommentAsync(
                dto.PostId, memberId, dto.Html, dto.ParentId, ct);

            return Created($"/api/comments/{comment.Id}", new { comment = ToResource(comment, memberId) });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/comments/{id}  — edit own comment.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = MemberAuthController.MemberCookieScheme)]
    public async Task<IActionResult> EditComment(
        string id, [FromBody] EditCommentDto dto, CancellationToken ct = default)
    {
        var memberId = GetMemberId();
        if (memberId is null)
            return Unauthorized(new { error = "Sign in to edit comments." });

        try
        {
            var comment = await commentService.EditCommentAsync(id, memberId, dto.Html, ct);
            return Ok(new { comment = ToResource(comment, memberId) });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// DELETE /api/comments/{id}  — delete own comment.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = MemberAuthController.MemberCookieScheme)]
    public async Task<IActionResult> DeleteComment(string id, CancellationToken ct = default)
    {
        var memberId = GetMemberId();
        if (memberId is null)
            return Unauthorized(new { error = "Sign in to delete comments." });

        try
        {
            await commentService.DeleteCommentAsync(id, memberId, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// POST /api/comments/{id}/like  — like a comment.
    /// </summary>
    [HttpPost("{id}/like")]
    [Authorize(AuthenticationSchemes = MemberAuthController.MemberCookieScheme)]
    public async Task<IActionResult> LikeComment(string id, CancellationToken ct = default)
    {
        var memberId = GetMemberId();
        if (memberId is null)
            return Unauthorized(new { error = "Sign in to like comments." });

        try
        {
            await commentService.LikeCommentAsync(id, memberId, ct);
            return Ok(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/comments/{id}/like  — unlike a comment.
    /// </summary>
    [HttpDelete("{id}/like")]
    [Authorize(AuthenticationSchemes = MemberAuthController.MemberCookieScheme)]
    public async Task<IActionResult> UnlikeComment(string id, CancellationToken ct = default)
    {
        var memberId = GetMemberId();
        if (memberId is null)
            return Unauthorized(new { error = "Sign in to unlike comments." });

        try
        {
            await commentService.UnlikeCommentAsync(id, memberId, ct);
            return Ok(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/comments/{id}/report  — report a comment.
    /// </summary>
    [HttpPost("{id}/report")]
    [Authorize(AuthenticationSchemes = MemberAuthController.MemberCookieScheme)]
    public async Task<IActionResult> ReportComment(string id, CancellationToken ct = default)
    {
        var memberId = GetMemberId();
        if (memberId is null)
            return Unauthorized(new { error = "Sign in to report comments." });

        try
        {
            await commentService.ReportCommentAsync(id, memberId, ct);
            return Ok(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private string? GetMemberId()
    {
        var memberIdentity = User.Identities
            .FirstOrDefault(i => i.AuthenticationType == MemberAuthController.MemberCookieScheme);

        if (memberIdentity?.IsAuthenticated != true)
            return null;

        return memberIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private static CommentResource ToResource(
        Core.Entities.Comment c, string? currentMemberId, List<CommentResource>? replies = null)
    {
        return new CommentResource
        {
            Id = c.Id,
            PostId = c.PostId,
            ParentId = c.ParentId,
            Status = c.Status,
            Html = c.Html,
            CreatedAt = c.CreatedAt,
            EditedAt = c.EditedAt,
            Member = c.Member is not null
                ? new CommentMemberResource { Id = c.Member.Id, Name = c.Member.Name, Expertise = c.Member.Expertise }
                : null,
            LikeCount = c.Likes.Count,
            Liked = currentMemberId is not null && c.Likes.Any(l => l.MemberId == currentMemberId),
            Replies = replies ?? [],
        };
    }
}

// ── DTOs ────────────────────────────────────────────

public class AddCommentDto
{
    [Required]
    public string PostId { get; set; } = null!;

    [Required]
    [MaxLength(10000)]
    public string Html { get; set; } = null!;

    public string? ParentId { get; set; }
}

public class EditCommentDto
{
    [Required]
    [MaxLength(10000)]
    public string Html { get; set; } = null!;
}

// ── Resource models ─────────────────────────────────

public class CommentResource
{
    public string Id { get; set; } = null!;
    public string PostId { get; set; } = null!;
    public string? ParentId { get; set; }
    public string Status { get; set; } = null!;
    public string? Html { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public CommentMemberResource? Member { get; set; }
    public int LikeCount { get; set; }
    public bool Liked { get; set; }
    public List<CommentResource> Replies { get; set; } = [];
}

public class CommentMemberResource
{
    public string Id { get; set; } = null!;
    public string? Name { get; set; }
    public string? Expertise { get; set; }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
