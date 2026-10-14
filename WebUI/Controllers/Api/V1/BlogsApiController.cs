using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Api;

namespace WebUI.Controllers.Api.V1;

/// <summary>Published content queries and policy protected author commands.</summary>
[ApiController]
[Route("api/v1/blogs")]
public sealed class BlogsApiController(ISender sender) : ControllerBase
{
    /// <summary>Lists published blogs with optional category and literal text filters.</summary>
    [HttpGet(Name = "ApiV1Blogs")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<BlogListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20,
        [FromQuery] int? categoryId = null, [FromQuery] string? search = null, CancellationToken cancellationToken = default)
        => Ok(ApiResponse<PaginatedList<BlogListDto>>.Ok(await sender.Send(new GetPublicBlogsQuery(pageIndex, pageSize, categoryId, Search: search), cancellationToken)));

    /// <summary>Returns a published blog and approved comments; drafts and deleted records are hidden.</summary>
    [HttpGet("{slug}", Name = "ApiV1BlogBySlug")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PublicBlogPostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, [FromQuery] int commentsPageIndex = 1,
        [FromQuery] int commentsPageSize = 10, CancellationToken cancellationToken = default)
    {
        var blog = await sender.Send(new GetPublicBlogBySlugQuery(slug), cancellationToken);
        if (blog is null) return Problem(statusCode: StatusCodes.Status404NotFound, title: "Blog not found.");
        var comments = await sender.Send(new GetPublicCommentsQuery(blog.Id, commentsPageIndex, commentsPageSize), cancellationToken);
        return Ok(ApiResponse<PublicBlogPostDto>.Ok(new(blog, comments)));
    }

    /// <summary>Creates a draft owned by the authenticated author. Publication is a separate review action.</summary>
    [HttpPost(Name = "ApiV1CreateBlog")]
    [Authorize(Policy = AccessPolicies.BlogAuthor)]
    [Authorize(Policy = AccessPolicies.ActiveMember)]
    [ApiCookieAntiforgery]
    [ProducesResponseType(typeof(ApiResponse<CreatedResourceDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] BlogWriteRequest request, CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateBlogCommand(request.Title, request.SubTitle, request.Content, request.CategoryId), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreatedResourceDto>.Ok(new(id), "Draft created.", StatusCodes.Status201Created));
    }

    /// <summary>Updates an owned blog or a blog managed by an editor/admin, preserving its original author.</summary>
    [HttpPut("{id:int}", Name = "ApiV1UpdateBlog")]
    [Authorize(Policy = AccessPolicies.BlogAuthor)]
    [Authorize(Policy = AccessPolicies.ActiveMember)]
    [ApiCookieAntiforgery]
    [ProducesResponseType(typeof(ApiResponse<ResourceChangedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, [FromBody] BlogWriteRequest request, CancellationToken cancellationToken)
        => await sender.Send(new UpdateBlogCommand(id, request.Title, request.SubTitle, request.Content, request.CategoryId), cancellationToken)
            ? Ok(ApiResponse<ResourceChangedDto>.Ok(new(id), "Blog updated."))
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Blog not found.");

    /// <summary>Moves an owned or managed blog into the trash while retaining its image.</summary>
    [HttpDelete("{id:int}", Name = "ApiV1DeleteBlog")]
    [Authorize(Policy = AccessPolicies.BlogAuthor)]
    [Authorize(Policy = AccessPolicies.ActiveMember)]
    [ApiCookieAntiforgery]
    [ProducesResponseType(typeof(ApiResponse<ResourceChangedDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => await sender.Send(new DeleteBlogCommand(id), cancellationToken)
            ? Ok(ApiResponse<ResourceChangedDto>.Ok(new(id), "Blog moved to trash."))
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Blog not found.");
}
