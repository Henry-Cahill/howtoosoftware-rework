using HowToSoftware.Core.Entities;
using HowToSoftware.Web.Controllers;
using HowToSoftware.Web.Tests.Fakes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HowToSoftware.Web.Tests;

public class CommentsControllerTests
{
    private readonly FakeCommentService _commentService = new();
    private readonly CommentsController _sut;

    public CommentsControllerTests()
    {
        _sut = new CommentsController(_commentService);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    private void SetMemberAuth(string memberId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, memberId),
        };
        var identity = new ClaimsIdentity(claims, MemberAuthController.MemberCookieScheme);
        _sut.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task GetComments_ReturnsOk()
    {
        var result = await _sut.GetComments("post-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetCount_ReturnsCount()
    {
        _commentService.Comments.Add(new Comment
        {
            Id = "c1", PostId = "post-1", Status = "published",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });

        var result = await _sut.GetCount("post-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task AddComment_Authenticated_ReturnsCreated()
    {
        SetMemberAuth("member-1");
        var dto = new AddCommentDto { PostId = "post-1", Html = "<p>Hello</p>" };

        var result = await _sut.AddComment(dto);

        var created = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(created.Value);
    }

    [Fact]
    public async Task AddComment_Unauthenticated_ReturnsUnauthorized()
    {
        var dto = new AddCommentDto { PostId = "post-1", Html = "<p>Hello</p>" };

        var result = await _sut.AddComment(dto);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task EditComment_OwnComment_ReturnsOk()
    {
        SetMemberAuth("member-1");
        _commentService.Comments.Add(new Comment
        {
            Id = "c1", PostId = "post-1", MemberId = "member-1",
            Html = "<p>Original</p>", Status = "published",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });

        var result = await _sut.EditComment("c1", new EditCommentDto { Html = "<p>Edited</p>" });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task EditComment_OtherMember_ReturnsForbid()
    {
        SetMemberAuth("member-2");
        _commentService.Comments.Add(new Comment
        {
            Id = "c1", PostId = "post-1", MemberId = "member-1",
            Html = "<p>Original</p>", Status = "published",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });

        var result = await _sut.EditComment("c1", new EditCommentDto { Html = "<p>Hacked</p>" });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteComment_OwnComment_ReturnsNoContent()
    {
        SetMemberAuth("member-1");
        _commentService.Comments.Add(new Comment
        {
            Id = "c1", PostId = "post-1", MemberId = "member-1",
            Status = "published", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });

        var result = await _sut.DeleteComment("c1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task LikeComment_Authenticated_ReturnsOk()
    {
        SetMemberAuth("member-1");

        var result = await _sut.LikeComment("c1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UnlikeComment_Authenticated_ReturnsOk()
    {
        SetMemberAuth("member-1");

        var result = await _sut.UnlikeComment("c1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task ReportComment_Authenticated_ReturnsOk()
    {
        SetMemberAuth("member-1");

        var result = await _sut.ReportComment("c1");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task LikeComment_Unauthenticated_ReturnsUnauthorized()
    {
        var result = await _sut.LikeComment("c1");

        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}

// =============================================================
// © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
// Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
// =============================================================
