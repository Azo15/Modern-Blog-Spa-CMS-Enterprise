using System.Security.Claims;
using BusinessLayer.Abstract;
using BusinessLayer.Container;
using BusinessLayer.Security;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MediatR;
namespace BusinessLayer.Tests;

internal sealed class Stage2Fixture : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    public AppDbContext Db { get; }
    public ISender Sender { get; }
    public TestUser User { get; } = new();
    public Mock<ICacheService> Cache { get; } = new();
    public Mock<IManagerNotifier> Notifier { get; } = new();
    public sealed class TestUser : ICurrentUserService
    {
        public ClaimsPrincipal User { get; set; } = AuthorizationTests.Principal(AppRoles.Author, "1");
        public string? UserId => User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
        public string? Email => User.FindFirstValue(ClaimTypes.Email);
    }
    public Stage2Fixture(bool mockCache = false)
    {
        connection.Open(); var services = new ServiceCollection().AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { { "Moderation:BlockedWords:0", "yasak" } }).Build());
        services.AddSingleton<ICurrentUserService>(User);
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddAuthorization(AccessPolicies.Configure); services.AddSingleton<IAuthorizationHandler, BlogOwnerAuthorizationHandler>();
        services.AddDistributedMemoryCache(); services.ContainerDependencies();
        services.AddScoped<IBlogImageStorage>(_ => Mock.Of<IBlogImageStorage>());
        Notifier.Setup(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        services.AddSingleton(Notifier.Object);
        if (mockCache)
        {
            Cache.Setup(c => c.RemoveByPrefixAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            Cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<BusinessLayer.Dtos.PaginatedList<BusinessLayer.Dtos.BlogListDto>>(), It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);
            services.AddSingleton(Cache.Object);
        }
        provider = services.BuildServiceProvider(); scope = provider.CreateScope(); Db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); Sender = scope.ServiceProvider.GetRequiredService<ISender>();
        Db.Database.EnsureCreated();
        Db.Users.AddRange(Enumerable.Range(1, 6).Select(id => new AppUser { Id = id, UserName = "user" + id, FirstName = "Name" + id, LastName = "Test", Email = "private" + id + "@example.com", PasswordHash = "private-hash", SecurityStamp = "private-stamp" }));
        Db.Catagories.AddRange(new Catagory { Id = 1, Title = "A" }, new Catagory { Id = 2, Title = "B" });
        Db.Blogs.Add(new Blog { Id = 1, Title = "Published title", SubTitle = "Public subtitle", Content = "<p>Safe</p>", Slug = "published-title", AppUserId = 1, CatagoryId = 1, PublishStatus = PublishStatus.Published, IsPublish = true }); Db.SaveChanges(); Db.ChangeTracker.Clear();
    }
    public void As(string? role, int id = 2) => User.User = AuthorizationTests.Principal(role, id.ToString(), role != null);
    public void Dispose() { scope.Dispose(); provider.Dispose(); connection.Dispose(); }
}
