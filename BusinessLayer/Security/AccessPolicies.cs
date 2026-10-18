using Microsoft.AspNetCore.Authorization;

namespace BusinessLayer.Security;

public static class AppRoles
{
    public const string Admin = "System Admin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";
    public static readonly string[] All = [Admin, Staff, Customer];
}

public static class AccessPolicies
{
    public const string Admin = "RequireAdmin";
    public const string ContentManager = "RequireContentManager";
    public const string Moderator = "RequireModerator";
    public const string BlogAuthor = "RequireBlogAuthor";
    public const string BlogResource = "ManageBlogResource";
    public const string Visitor = "Visitor";
    public const string CommentModerator = "RequireCommentModerator";
    public const string Member = "RequireMember";
    public const string ActiveMember = "RequireActiveMember";
    public const string AccountModerator = "RequireAccountModerator";

    // Called by Program.cs; tests use the same policy definitions.
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(Admin, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.Admin));
        options.AddPolicy(ContentManager, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.Staff));
        options.AddPolicy(Moderator, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.Staff));
        options.AddPolicy(BlogAuthor, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.Staff));
        options.AddPolicy(BlogResource, p => p.RequireAuthenticatedUser().AddRequirements(new BlogOwnerRequirement()));
        options.AddPolicy(CommentModerator, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.Staff));
        options.AddPolicy(Member, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.All));
        options.AddPolicy(ActiveMember, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.All).AddRequirements(new ActiveMemberRequirement()));
        options.AddPolicy(AccountModerator, p => p.RequireAuthenticatedUser().RequireRole(AppRoles.Admin, AppRoles.Staff).AddRequirements(new ActiveMemberRequirement()));
        // Public access includes anonymous visitors and signed-in readers. No Visitor account/role.
        options.AddPolicy(Visitor, p => p.RequireAssertion(_ => true));
    }
}
