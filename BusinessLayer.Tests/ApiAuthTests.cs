using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Handlers;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using BusinessLayer.ValidationRules;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using WebUI.Api;

namespace BusinessLayer.Tests;

public class ApiAuthTests
{
    private static Mock<UserManager<AppUser>> UserManagerMock() => new(Mock.Of<IUserStore<AppUser>>(),
        Options.Create(new IdentityOptions()), Mock.Of<IPasswordHasher<AppUser>>(), Array.Empty<IUserValidator<AppUser>>(),
        Array.Empty<IPasswordValidator<AppUser>>(), Mock.Of<ILookupNormalizer>(), new IdentityErrorDescriber(),
        Mock.Of<IServiceProvider>(), NullLogger<UserManager<AppUser>>.Instance);

    private static Mock<SignInManager<AppUser>> SignInMock(UserManager<AppUser> users) => new(users,
        Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(), Options.Create(new IdentityOptions()),
        NullLogger<SignInManager<AppUser>>.Instance, Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<AppUser>>());

    [Theory]
    [InlineData(AppRoles.Admin)]
    [InlineData(AppRoles.ContentManager)]
    [InlineData(AppRoles.Author)]
    [InlineData(AppRoles.Moderator)]
    [InlineData(AppRoles.RegisteredUser)]
    public async Task LoginVerifiesIdentityPasswordAndIssuesTokenWithoutCreatingCookie(string role)
    {
        var user = new AppUser { Id = 7, IsActive = true, SecurityStamp = "private-stamp" };
        var users = UserManagerMock();
        users.Setup(x => x.FindByEmailAsync("member@example.test")).ReturnsAsync(user);
        users.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
        users.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([role, "Unrecognized role"]);
        var signIn = SignInMock(users.Object);
        signIn.Setup(x => x.CheckPasswordSignInAsync(user, "valid-password", true)).ReturnsAsync(SignInResult.Success);
        var tokens = new Mock<IApiTokenIssuer>();
        tokens.Setup(x => x.IssueAsync(user, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApiTokenDto("signed-token", "Bearer", DateTimeOffset.UtcNow.AddMinutes(30), 7, [role]));
        var result = await new ApiLoginCommandHandler(users.Object, signIn.Object, tokens.Object)
            .Handle(new ApiLoginCommand(" member@example.test ", "valid-password"), CancellationToken.None);
        Assert.Equal("signed-token", result!.AccessToken);
        tokens.Verify(x => x.IssueAsync(user, It.Is<IReadOnlyCollection<string>>(roles => roles.Count == 1 && roles.Contains(role)), It.IsAny<CancellationToken>()), Times.Once);
        signIn.Verify(x => x.SignInAsync(It.IsAny<AppUser>(), It.IsAny<AuthenticationProperties>(), It.IsAny<string?>()), Times.Never);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("inactive")]
    [InlineData("locked")]
    [InlineData("password")]
    [InlineData("no-roles")]
    public async Task LoginRejectsUnavailableOrInvalidAccountsWithoutIssuingCredentials(string reason)
    {
        var user = new AppUser { Id = 7, IsActive = reason != "inactive" };
        var users = UserManagerMock();
        users.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(reason == "missing" ? null : user);
        users.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(reason == "locked");
        users.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(reason == "no-roles" ? [] : [AppRoles.Author]);
        var signIn = SignInMock(users.Object);
        signIn.Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
            .ReturnsAsync(reason == "password" ? SignInResult.Failed : SignInResult.Success);
        var tokens = new Mock<IApiTokenIssuer>(MockBehavior.Strict);
        Assert.Null(await new ApiLoginCommandHandler(users.Object, signIn.Object, tokens.Object)
            .Handle(new ApiLoginCommand("member@example.test", "attempt"), CancellationToken.None));
        tokens.VerifyNoOtherCalls();
        if (reason is "inactive" or "locked" or "missing")
            signIn.Verify(x => x.CheckPasswordSignInAsync(It.IsAny<AppUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Theory]
    [InlineData("", "valid")]
    [InlineData("not-an-email", "valid")]
    [InlineData("member@example.test", "")]
    public void LoginValidationRejectsInvalidInputs(string email, string password)
        => Assert.False(new ApiLoginCommandValidator().Validate(new(email, password)).IsValid);

    [Fact]
    public void LoginValidationBoundsCredentialLengths()
    {
        var validator = new ApiLoginCommandValidator();
        Assert.False(validator.Validate(new(new string('a', 257) + "@example.test", "valid")).IsValid);
        Assert.False(validator.Validate(new("member@example.test", new string('a', 1025))).IsValid);
        Assert.True(validator.Validate(new("member@example.test", "valid")).IsValid);
    }

    [Fact]
    public async Task CurrentIdentityUsesActiveAccountCheckAndProjectsOnlySafeProperties()
    {
        var members = new Mock<IActiveMemberService>();
        members.Setup(x => x.RequireAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var users = UserManagerMock();
        var user = new AppUser { Id = 7, FirstName = "Example", LastName = "Member", Email = "private@example.test", PasswordHash = "private-hash" };
        users.Setup(x => x.FindByIdAsync("7")).ReturnsAsync(user);
        users.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([AppRoles.RegisteredUser, "Unknown"]);
        var identity = await new GetApiIdentityQueryHandler(members.Object, users.Object).Handle(new(), CancellationToken.None);
        Assert.Equal(7, identity.UserId);
        Assert.Equal("Example Member", identity.DisplayName);
        Assert.Equal(AppRoles.RegisteredUser, Assert.Single(identity.Roles));
        var json = System.Text.Json.JsonSerializer.Serialize(identity);
        Assert.DoesNotContain("private@example", json);
        Assert.DoesNotContain("private-hash", json);
    }

    [Fact]
    public async Task CurrentIdentityCannotReturnLockedAccountOrDeletedAccount()
    {
        var members = new Mock<IActiveMemberService>();
        members.Setup(x => x.RequireAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new UnauthorizedAccessException());
        var users = UserManagerMock();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new GetApiIdentityQueryHandler(members.Object, users.Object).Handle(new(), CancellationToken.None));
        members.Setup(x => x.RequireAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);
        users.Setup(x => x.FindByIdAsync("7")).ReturnsAsync((AppUser?)null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new GetApiIdentityQueryHandler(members.Object, users.Object).Handle(new(), CancellationToken.None));
    }

    private static JwtOptions Settings() => new() { SigningKey = new string('s', 64), Issuer = "test-issuer", Audience = "test-clients", AccessTokenMinutes = 15 };

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);
    }

    [Fact]
    public async Task JwtHasBoundedLifetimeExpectedRolesAndNoIdentitySecrets()
    {
        var options = Settings();
        var user = new AppUser { Id = 7, Email = "private@example.test", PasswordHash = "private-hash", SecurityStamp = "private-stamp" };
        var result = await new JwtTokenIssuer(Options.Create(options), new FixedClock()).IssueAsync(user, [AppRoles.Author, AppRoles.Author]);
        Assert.Equal(new FixedClock().GetUtcNow().AddMinutes(15), result.ExpiresAtUtc);
        Assert.Single(result.Roles);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal(options.Issuer, jwt.Issuer);
        Assert.Equal(options.Audience, Assert.Single(jwt.Audiences));
        Assert.Equal(SecurityAlgorithms.HmacSha256, jwt.Header.Alg);
        Assert.Equal("7", jwt.Claims.Single(x => x.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal(AppRoles.Author, jwt.Claims.Single(x => x.Type == ClaimTypes.Role).Value);
        var payload = jwt.Payload.SerializeToJson();
        foreach (var secret in new[] { "private@example", "private-hash", "private-stamp", "PasswordHash", "SecurityStamp" }) Assert.DoesNotContain(secret, payload);
        Assert.Equal(JwtTokenIssuer.SecurityVersion("private-stamp"), jwt.Claims.Single(x => x.Type == ApiAuthentication.SecurityVersionClaim).Value);
        var validated = new JwtSecurityTokenHandler().ValidateToken(result.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = options.Issuer, ValidateAudience = true, ValidAudience = options.Audience,
            ValidateLifetime = false, ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey))
        }, out _);
        Assert.True(validated.Identity!.IsAuthenticated);
    }

    [Fact]
    public async Task JwtIssuerRejectsStampLessAccountAndHonorsCancellation()
    {
        var issuer = new JwtTokenIssuer(Options.Create(Settings()), new FixedClock());
        await Assert.ThrowsAsync<InvalidOperationException>(() => issuer.IssueAsync(new AppUser(), []));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => issuer.IssueAsync(new AppUser(), [], new CancellationToken(true)));
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(31, 30)]
    [InlineData(64, 4)]
    [InlineData(64, 61)]
    public void ApiConfigurationRejectsWeakKeysOrUnboundedLifetime(int keyLength, int lifetime)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Jwt:SigningKey"] = new string('x', keyLength), ["Jwt:AccessTokenMinutes"] = lifetime.ToString() }).Build();
        using var provider = new ServiceCollection().AddLogging().AddPlatformApi(configuration).BuildServiceProvider();
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtOptions>>().Value);
    }

    [Theory]
    [InlineData("valid", true)]
    [InlineData("missing", false)]
    [InlineData("inactive", false)]
    [InlineData("locked", false)]
    [InlineData("stamp", false)]
    [InlineData("roles", false)]
    [InlineData("bad-id", false)]
    [InlineData("no-version", false)]
    public async Task JwtValidationChecksAccountLockoutSecurityStampAndCurrentRolesOnEveryRequest(string reason, bool valid)
    {
        var user = new AppUser { Id = 7, IsActive = reason != "inactive", SecurityStamp = "current-stamp" };
        var users = UserManagerMock();
        users.Setup(x => x.FindByIdAsync("7")).ReturnsAsync(reason == "missing" ? null : user);
        users.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(reason == "locked");
        users.Setup(x => x.GetRolesAsync(user)).ReturnsAsync([AppRoles.Author]);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = new string('s', 64) }).Build();
        var services = new ServiceCollection().AddLogging().AddPlatformApi(configuration);
        services.AddSingleton(users.Object);
        using var provider = services.BuildServiceProvider();
        var bearer = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, reason == "bad-id" ? "invalid" : "7"),
            new(ClaimTypes.Role, reason == "roles" ? AppRoles.Admin : AppRoles.Author)
        };
        if (reason != "no-version") claims.Add(new(ApiAuthentication.SecurityVersionClaim, JwtTokenIssuer.SecurityVersion(reason == "stamp" ? "old-stamp" : "current-stamp")));
        var context = new TokenValidatedContext(new DefaultHttpContext { RequestServices = provider },
            new AuthenticationScheme(JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler)), bearer)
        { Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, ApiAuthentication.BearerIdentityType)) };
        await bearer.Events.OnTokenValidated(context);
        Assert.Equal(valid, context.Result?.Failure is null);
    }
}
