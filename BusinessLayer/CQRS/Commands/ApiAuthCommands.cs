using BusinessLayer.Dtos;
using MediatR;

namespace BusinessLayer.CQRS.Commands;

/// <summary>Checks an Identity password and issues a token without signing in a browser cookie.</summary>
public sealed record ApiLoginCommand(string Email, string Password) : IRequest<ApiTokenDto?>
{
    public override string ToString() => "ApiLoginCommand { Credentials = [REDACTED] }";
}
