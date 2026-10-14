using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Api;

namespace WebUI.Controllers.Api.V1;

/// <summary>Identity password verification and short lived API bearer credentials.</summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthApiController(ISender sender) : ControllerBase
{
    /// <summary>Issues a JWT after an active account passes Identity password and lockout checks.</summary>
    [HttpPost("login", Name = "ApiV1Login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ApiTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] ApiLoginRequest request, CancellationToken cancellationToken)
    {
        var token = await sender.Send(new ApiLoginCommand(request.Email, request.Password), cancellationToken);
        return token is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials or unavailable account.")
            : Ok(ApiResponse<ApiTokenDto>.Ok(token));
    }

    /// <summary>Returns the authenticated, active client's identity and access profiles.</summary>
    [HttpGet("me", Name = "ApiV1Identity")]
    [Authorize(Policy = AccessPolicies.ActiveMember)]
    [ProducesResponseType(typeof(ApiResponse<ApiIdentityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
        => Ok(ApiResponse<ApiIdentityDto>.Ok(await sender.Send(new GetApiIdentityQuery(), cancellationToken)));
}
