using BusinessLayer.Abstract;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BusinessLayer.Tests;

internal sealed class PlatformWebFactory(int permitLimit = 1000, bool failQueries = false, bool googleEnabled = false) : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public const string Password = "P@ssword123!";
    public static readonly (int Id, string Email, string Role)[] Accounts =
    [
        (1, "admin@acceptance.test", AppRoles.Admin), (2, "editor@acceptance.test", AppRoles.Staff),
        (3, "author@acceptance.test", AppRoles.Staff), (4, "moderator@acceptance.test", AppRoles.Staff),
        (5, "user@acceptance.test", AppRoles.Customer), (6, "other-author@acceptance.test", AppRoles.Staff)
    ];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseEnvironment("Testing");
        if (googleEnabled)
        {
            builder.UseSetting("Authentication:Google:ClientId", "google-client-for-tests");
            builder.UseSetting("Authentication:Google:ClientSecret", "test-only-secret-never-deployed");
        }
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Google:ClientId"] = googleEnabled ? "google-client-for-tests" : null,
            ["Authentication:Google:ClientSecret"] = googleEnabled ? "test-only-secret-never-deployed" : null,
            ["Jwt:SigningKey"] = "acceptance-test-key-only-32-bytes-never-used-for-deployment",
            ["Swagger:Enabled"] = "true", ["ApiRateLimit:PermitLimit"] = permitLimit.ToString(),
            ["ApiRateLimit:WindowSeconds"] = "60", ["Moderation:BlockedWords:0"] = "yasak"
        }));
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            var notifier = new Mock<IManagerNotifier>();
            notifier.Setup(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            services.AddSingleton(notifier.Object);
            services.AddSingleton(Mock.Of<IContactEmailQueue>());
            services.AddSingleton(Mock.Of<IAppointmentEmailQueue>());
            if (failQueries)
            {
                var sender = new Mock<MediatR.ISender>();
                sender.Setup(s => s.Send(It.IsAny<BusinessLayer.CQRS.Queries.GetPublicBlogsQuery>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new InvalidOperationException("Password=super-secret; email=private@example.test; token=private-token"));
                services.AddSingleton(sender.Object);
            }
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        foreach (var role in AppRoles.All) Assert.True((await roles.CreateAsync(new IdentityRole<int>(role))).Succeeded);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        foreach (var account in Accounts)
        {
            var user = new AppUser { Id = account.Id, UserName = account.Email, Email = account.Email,
                FirstName = "Test", LastName = account.Role, EmailConfirmed = true, IsActive = true };
            Assert.True((await users.CreateAsync(user, Password)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, account.Role)).Succeeded);
        }
        db.Catagories.AddRange(new Catagory { Id = 1, Title = "Wellness" }, new Catagory { Id = 2, Title = "Empty" });
        db.Blogs.AddRange(
            new Blog { Id = 1, Title = "Published Massage", SubTitle = "Wellness guide subtitle", Content = "<p>Public content</p>",
                Slug = "approved-massage", CatagoryId = 1, AppUserId = 3, PublishStatus = PublishStatus.Published },
            new Blog { Id = 2, Title = "Hidden draft", Slug = "hidden-draft", AppUserId = 3, CatagoryId = 1, PublishStatus = PublishStatus.Draft },
            new Blog { Id = 3, Title = "Another author article", Slug = "other-author", AppUserId = 6, CatagoryId = 1, PublishStatus = PublishStatus.Published },
            new Blog { Id = 4, Title = "Deleted article", Slug = "deleted-article", AppUserId = 3, CatagoryId = 1, PublishStatus = PublishStatus.Published });
        db.Comments.AddRange(new Comment { Id = 10, BlogId = 1, AppUserId = 5, Content = "Approved root", IsApproved = true },
            new Comment { Id = 11, BlogId = 1, AppUserId = 5, ParentCommentId = 10, Content = "Approved reply", IsApproved = true },
            new Comment { Id = 12, BlogId = 1, AppUserId = 5, Content = "Private pending comment", IsApproved = false });
        await db.SaveChangesAsync();
        db.Remove(await db.Blogs.SingleAsync(b => b.Id == 4)); await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    { base.Dispose(disposing); if (disposing) connection.Dispose(); }
}
