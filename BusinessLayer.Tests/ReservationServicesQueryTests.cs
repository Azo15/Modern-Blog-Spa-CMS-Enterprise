using System.Net;
using System.Text.RegularExpressions;
using BusinessLayer.CQRS.Queries;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;

public sealed class ReservationServicesQueryTests
{
    [Theory]
    [InlineData(12)]
    [InlineData(53)]
    public async Task ReservationListsEveryPublishedServiceWithoutDeletedOrDraftServicesAndProjectsDuration(int count)
    {
        using var factory = new PlatformWebFactory();
        await factory.InitializeAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Services.AddRange(Enumerable.Range(100, count).Select(id => new Service
            {
                Id = id, Title = "Ritüel " + id, Content = "60 dk — 650 ₺", IsPublish = true
            }));
            db.Services.AddRange(new Service { Id = 500, Title = "Draft service", IsPublish = false },
                new Service { Id = 501, Title = "Deleted service", IsPublish = true });
            await db.SaveChangesAsync();
            db.Remove(await db.Services.FindAsync(501) ?? throw new InvalidOperationException());
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var services = await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GetReservationServicesQuery());
            Assert.Equal(count, services.Count);
            Assert.All(services, service => Assert.Equal(60, service.DurationMinutes));
            Assert.DoesNotContain(services, service => service.Id is 500 or 501);
            Assert.Empty(db.ChangeTracker.Entries<Service>());
        }
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/randevu");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        var modal = Regex.Match(html, "<form[^>]*id=\"vip-form\"[\\s\\S]*?</form>").Value;
        Assert.Equal(count, Regex.Matches(modal, "name=\"ServiceId\"").Count);
        Assert.Equal(count, Regex.Matches(modal, "class=\"luxe-vip-duration\"").Count);
        Assert.DoesNotContain("Draft service", modal);
        Assert.DoesNotContain("Deleted service", modal);
    }
}
