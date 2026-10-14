using BusinessLayer.CQRS.Commands;
using BusinessLayer.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Api;

namespace WebUI.Controllers.Api.V1;

/// <summary>Member comments and one level of direct replies, submitted for moderation.</summary>
[ApiController]
[Route("api/v1/comments")]
[Produces("application/json")]
public sealed class CommentsApiController(ISender sender) : ControllerBase
{
    /// <summary>Creates a pending comment or a pending reply to an approved root comment.</summary>
    [HttpPost(Name = "ApiV1CreateComment")]
    [Authorize(Policy = AccessPolicies.ActiveMember)]
    [ApiCookieAntiforgery]
    [ProducesResponseType(typeof(ApiResponse<CreatedResourceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CommentWriteRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateCommentCommand(request.BlogId, request.Content, request.ParentCommentId), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreatedResourceDto>.Ok(new(id), "Comment awaits moderation.", StatusCodes.Status201Created));
    }
}
