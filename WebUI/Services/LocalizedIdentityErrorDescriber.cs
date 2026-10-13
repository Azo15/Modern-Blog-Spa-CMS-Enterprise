using BusinessLayer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;

namespace WebUI.Services;

public sealed class LocalizedIdentityErrorDescriber(IStringLocalizer<SharedResource> text) : IdentityErrorDescriber
{
    private IdentityError Error(string code, params object[] arguments)
        => new() { Code = code, Description = text["Identity." + code, arguments] };
    public override IdentityError DefaultError() => Error(nameof(DefaultError));
    public override IdentityError ConcurrencyFailure() => Error(nameof(ConcurrencyFailure));
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch));
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), length);
    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) => Error(nameof(PasswordRequiresUniqueChars), uniqueChars);
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric));
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit));
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower));
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper));
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), email);
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), userName);
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), email ?? "");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), userName ?? "");
    public override IdentityError UserAlreadyHasPassword() => Error(nameof(UserAlreadyHasPassword));
}
