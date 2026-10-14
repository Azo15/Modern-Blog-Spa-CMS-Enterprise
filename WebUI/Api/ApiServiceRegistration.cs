using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusinessLayer.Abstract;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace WebUI.Api;

public static class ApiServiceRegistration
{
    /// <summary>Adds validated JWT/cookie authentication and v1 OpenAPI documentation.</summary>
    public static IServiceCollection AddPlatformApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(settings => Encoding.UTF8.GetByteCount(settings.SigningKey) is >= 32 and <= 4096,
                "Jwt:SigningKey must be supplied through secrets/environment with at least 32 UTF-8 bytes.")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer) && !string.IsNullOrWhiteSpace(settings.Audience),
                "JWT issuer and audience are required.")
            .Validate(settings => settings.AccessTokenMinutes is >= 5 and <= 60, "JWT lifetime must be between 5 and 60 minutes.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IApiTokenIssuer, JwtTokenIssuer>();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = ApiAuthentication.HybridScheme;
            options.DefaultChallengeScheme = ApiAuthentication.HybridScheme;
            options.DefaultForbidScheme = ApiAuthentication.HybridScheme;
        }).AddPolicyScheme(ApiAuthentication.HybridScheme, "Identity cookie or JWT", options =>
        {
            // Any bearer attempt is authenticated solely as a bearer. Invalid tokens never
            // gain access by falling back to an otherwise valid browser cookie.
            options.ForwardDefaultSelector = context => context.Request.Headers.Authorization.ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? JwtBearerDefaults.AuthenticationScheme : IdentityConstants.ApplicationScheme;
        }).AddJwtBearer(options => options.IncludeErrorDetails = false);
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, configured) =>
            {
                var settings = configured.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    AuthenticationType = ApiAuthentication.BearerIdentityType,
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
                bearer.Events = new JwtBearerEvents { OnTokenValidated = ValidateAccountAsync };
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Modern Spa & Blog Platform API v1",
                Version = "v1",
                Description = "Published blog queries and Identity protected author/member commands. Successes use ApiResponse<T>; errors use RFC 7807 Problem Details. Cookie commands require antiforgery validation.",
                License = new OpenApiLicense { Name = configuration["Swagger:LicenseName"] ?? "License not specified; contact project owner" }
            });
            options.DocInclusionPredicate((document, description) => document == "v1" &&
                description.RelativePath?.StartsWith("api/v1/", StringComparison.OrdinalIgnoreCase) == true);
            foreach (var xmlFile in new[] { "WebUI.xml", "BusinessLayer.xml" })
            {
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath);
            }
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Enter the accessToken returned by POST /api/v1/auth/login."
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
            });
            options.OperationFilter<ApiSecurityOperationFilter>();
        });
        return services;
    }

    /// <summary>Exposes documentation only in development or when explicitly enabled.</summary>
    public static WebApplication UsePlatformSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Modern Spa & Blog Platform API v1");
                options.DocumentTitle = "Modern Spa & Blog Platform API v1";
            });
        }
        return app;
    }

    private static async Task ValidateAccountAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var version = principal?.FindFirstValue(ApiAuthentication.SecurityVersionClaim);
        if (!int.TryParse(id, out var accountId) || accountId <= 0 || version?.Length != 64)
        { context.Fail("Account credential is invalid."); return; }
        var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByIdAsync(id!);
        if (user is null || !user.IsActive || string.IsNullOrEmpty(user.SecurityStamp) || await users.IsLockedOutAsync(user))
        { context.Fail("Account credential is unavailable."); return; }
        var expected = JwtTokenIssuer.SecurityVersion(user.SecurityStamp);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(version!), Encoding.UTF8.GetBytes(expected)))
        { context.Fail("Account credential has changed."); return; }
        var currentRoles = (await users.GetRolesAsync(user)).Where(role => AppRoles.All.Contains(role)).ToHashSet(StringComparer.Ordinal);
        var tokenRoles = principal!.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToHashSet(StringComparer.Ordinal);
        if (currentRoles.Count == 0 || !currentRoles.SetEquals(tokenRoles)) context.Fail("Account access has changed.");
    }
}
