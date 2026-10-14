using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Api;

namespace WebUI.Controllers.Api.V1;

/// <summary>Public category options with counts that include published blogs only.</summary>
[ApiController]
[Route("api/v1/categories")]
[Produces("application/json")]
public sealed class CategoriesApiController(ISender sender) : ControllerBase
{
    /// <summary>Gets available categories and their publicly visible blog counts.</summary>
    [HttpGet(Name = "ApiV1Categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PublicCategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => Ok(ApiResponse<IReadOnlyList<PublicCategoryDto>>.Ok(await sender.Send(new GetPublicCategoriesQuery(), cancellationToken)));
}
