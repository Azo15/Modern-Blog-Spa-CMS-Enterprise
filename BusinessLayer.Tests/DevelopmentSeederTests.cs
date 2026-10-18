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
        var seededDb = verification.ServiceProvider.GetRequiredService<AppDbContext>();
        var blogImages = await seededDb.Blogs.Select(x => x.Image).ToArrayAsync();
        var serviceImages = await seededDb.Services.Select(x => x.Image).ToArrayAsync();
        var galleryImages = await seededDb.Galaries.Select(x => x.Image).ToArrayAsync();
        var editorialImages = blogImages.Concat(serviceImages).Concat(galleryImages).ToArray();
        Assert.Equal(29, blogImages.Length);
        Assert.Equal(12, serviceImages.Length);
        Assert.Equal(12, galleryImages.Length);
        Assert.Equal(editorialImages.Length, editorialImages.Distinct().Count());
        Assert.All(editorialImages, path => Assert.EndsWith(".webp", path));
        Assert.Equal("XSPA Ekibi", (await users.FindByEmailAsync("admin@spa.com"))!.FullName);
        Assert.Equal("XSPA Editörü", (await users.FindByEmailAsync("editor@spa.com"))!.FullName);
        Assert.Equal("Wellness Uzmanı", (await users.FindByEmailAsync("author@spa.com"))!.FullName);
        Assert.Equal(5, await users.Users.CountAsync());
        Assert.Equal(3, await roles.Roles.CountAsync());
        Assert.False(await roles.RoleExistsAsync("Visitor"));
        foreach (var (email, role) in new[] {
            ("admin@spa.com", AppRoles.Admin), ("editor@spa.com", AppRoles.Staff),
            ("author@spa.com", AppRoles.Staff), ("moderator@spa.com", AppRoles.Staff),
            ("user@spa.com", AppRoles.Customer) })
        {
            var account = await users.FindByEmailAsync(email);
            Assert.NotNull(account);
            Assert.True(account.EmailConfirmed);
            Assert.True(await users.CheckPasswordAsync(account, "P@ssword123!"));
            Assert.Equal(role, Assert.Single(await users.GetRolesAsync(account)));
        }
    }

    [Fact]
    public async Task ExistingLegacyRolesMigrateWithoutCreatingProductionDemoUsers()
    {
        using var connection=new SqliteConnection("Data Source=:memory:");connection.Open();
        var services=new ServiceCollection().AddLogging();services.AddDataProtection();services.AddDbContext<AppDbContext>(o=>o.UseSqlite(connection));
        services.AddIdentityCore<AppUser>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        using var provider=services.BuildServiceProvider();
        using(var scope=provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
            var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            foreach(var role in new[]{"Author","Registered User",AppRoles.Admin})Assert.True((await roles.CreateAsync(new(role))).Succeeded);
            foreach(var pair in new[]{("legacy-staff@example.test","Author"),("legacy-customer@example.test","Registered User"),("existing-admin@example.test",AppRoles.Admin)})
            {var user=new AppUser{UserName=pair.Item1,Email=pair.Item1};Assert.True((await users.CreateAsync(user,"OriginalPassword1!")).Succeeded);Assert.True((await users.AddToRoleAsync(user,pair.Item2)).Succeeded);if(pair.Item2=="Author")Assert.True((await users.AddToRoleAsync(user,"Registered User")).Succeeded);}
        }
        await DataSeeder.SeedRolesAsync(provider);await DataSeeder.SeedRolesAsync(provider);
        using var verify=provider.CreateScope();var manager=verify.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Assert.Equal(3,await manager.Users.CountAsync());
        foreach(var pair in new[]{("legacy-staff@example.test",AppRoles.Staff),("legacy-customer@example.test",AppRoles.Customer),("existing-admin@example.test",AppRoles.Admin)})
        {var user=(await manager.FindByEmailAsync(pair.Item1))!;Assert.Equal(pair.Item2,Assert.Single(await manager.GetRolesAsync(user)));Assert.True(await manager.CheckPasswordAsync(user,"OriginalPassword1!"));}
    }
}
