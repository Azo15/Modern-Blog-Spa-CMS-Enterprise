using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using BusinessLayer.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebUI.Data;

namespace BusinessLayer.Tests;

public class DevelopmentSeederTests
{
    [Fact]
    public async Task DevelopmentSeedingIsRepeatableAndCreatesFiveConfirmedAccountsWithoutVisitor()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Development");
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton(environment.Object);
        services.AddSingleton<WebUI.Services.PublicMediaService>();
        services.AddDataProtection();
        services.AddDbContext<AppDbContext>(o => o.UseSqlite(connection));
        services.AddIdentityCore<AppUser>().AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
        await DataSeeder.SeedAsync(provider);
        using (var seeded = provider.CreateScope())
        {
            var db = seeded.ServiceProvider.GetRequiredService<AppDbContext>();
            var contact = await db.Contacts.SingleAsync();
            contact.Adres = "Preserve this business address";
            contact.Mail = "preserved@example.test";
            contact.Phone = "old-number";
            await db.SaveChangesAsync();
        }
        await DataSeeder.SeedAsync(provider);
        using var verification = provider.CreateScope();
        var users = verification.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roles = verification.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var persistedContact = await verification.ServiceProvider.GetRequiredService<AppDbContext>().Contacts.SingleAsync();
        Assert.Equal("+90 538 878 70 00", persistedContact.Phone);
        Assert.Equal("Preserve this business address", persistedContact.Adres);
        Assert.Equal("preserved@example.test", persistedContact.Mail);
        Assert.Equal(5, await users.Users.CountAsync());
        Assert.Equal(5, await roles.Roles.CountAsync());
        Assert.False(await roles.RoleExistsAsync("Visitor"));
        foreach (var (email, role) in new[] {
            ("admin@spa.com", AppRoles.Admin), ("editor@spa.com", AppRoles.ContentManager),
            ("author@spa.com", AppRoles.Author), ("moderator@spa.com", AppRoles.Moderator),
            ("user@spa.com", AppRoles.RegisteredUser) })
        {
            var account = await users.FindByEmailAsync(email);
            Assert.NotNull(account);
            Assert.True(account.EmailConfirmed);
            Assert.True(await users.CheckPasswordAsync(account, "P@ssword123!"));
            Assert.Equal(role, Assert.Single(await users.GetRolesAsync(account)));
        }
    }
}
