using System.Security.Claims;
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using EntityLayer.Concrete;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace BusinessLayer.CQRS.Handlers;

public sealed class ApiLoginCommandHandler(UserManager<AppUser> users, SignInManager<AppUser> signIn, IApiTokenIssuer tokens)
    : IRequestHandler<ApiLoginCommand, ApiTokenDto?>
{
    public async Task<ApiTokenDto?> Handle(ApiLoginCommand request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || await users.IsLockedOutAsync(user)) return null;
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return null;
        var roles = (await users.GetRolesAsync(user)).Where(role => AppRoles.All.Contains(role)).ToArray();
        if (roles.Length == 0) return null;
        return await tokens.IssueAsync(user, roles, ct);
    }
}

public sealed class GetApiIdentityQueryHandler(ICurrentUserService current, IActiveMemberService members, UserManager<AppUser> users)
    : IRequestHandler<GetApiIdentityQuery, ApiIdentityDto>
{
    public async Task<ApiIdentityDto> Handle(GetApiIdentityQuery request, CancellationToken ct)
    {
        var id = await members.RequireAsync(ct);
        var user = await users.FindByIdAsync(id.ToString(System.Globalization.CultureInfo.InvariantCulture))
            ?? throw new UnauthorizedAccessException();
        return new ApiIdentityDto(id, user.FullName, (await users.GetRolesAsync(user)).Where(role => AppRoles.All.Contains(role)).ToArray());
    }
}
