using BusinessLayer.CQRS.Commands;
using BusinessLayer.ValidationRules;
using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using WebUI.Data;
namespace BusinessLayer.Tests;
public sealed class CatalogSynchronizationTests
{
    [Fact]
    public async Task FourExistingServicesAreCompletedWithoutChangingTheirPriceOrPublication()
    {
        using var fixture=new Stage2Fixture();
        var initial=DataSeeder.CreateServiceCatalog().Take(4).ToArray();
        initial[0].Content="90 dk — 5.000 ₺";initial[0].IsPublish=false;
        fixture.Db.Services.AddRange(initial);await fixture.Db.SaveChangesAsync();
        Assert.Equal(8,await DataSeeder.SyncServiceCatalogAsync(fixture.Db));
        Assert.Equal(12,await fixture.Db.Services.IgnoreQueryFilters().CountAsync());
        Assert.Equal(0,await DataSeeder.SyncServiceCatalogAsync(fixture.Db));
        Assert.Equal("90 dk — 5.000 ₺",(await fixture.Db.Services.IgnoreQueryFilters().SingleAsync(x=>x.Id==initial[0].Id)).Content);
        Assert.False(initial[0].IsPublish);
    }
    [Fact]
    public async Task OldPeelingTitleAndSoftDeletedServiceAreNotDuplicatedOrRestored()
    {
        using var fixture=new Stage2Fixture();
        var catalog=DataSeeder.CreateServiceCatalog();
        catalog.Single(x=>x.Title=="Yosun Peeling").Title="Cilt Peeling Seansı";
        fixture.Db.Services.AddRange(catalog);await fixture.Db.SaveChangesAsync();
        fixture.Db.Services.Remove(catalog[0]);await fixture.Db.SaveChangesAsync();
        Assert.Equal(0,await DataSeeder.SyncServiceCatalogAsync(fixture.Db));
        Assert.Equal(12,await fixture.Db.Services.IgnoreQueryFilters().CountAsync());
        Assert.True(catalog[0].IsDeleted);
    }
    [Fact]
    public void EverySeededServiceHasAnExistingDistinctWebpAssetAndASeparateInstance()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"WebUI","wwwroot")))root=root.Parent;
        Assert.NotNull(root);
        var catalog=DataSeeder.CreateServiceCatalog();
        Assert.Equal(12,catalog.Count);Assert.Equal(12,catalog.Select(x=>x.Image).Distinct().Count());
        foreach(var service in catalog)
        {
            Assert.EndsWith(".webp",service.Image);
            Assert.True(File.Exists(Path.Combine(root!.FullName,"WebUI","wwwroot",service.Image!)),service.Image);
            Assert.Contains("dk",service.Content);Assert.Contains("₺",service.Content);
        }
        catalog[0].Content="changed";
        Assert.NotEqual("changed",DataSeeder.CreateServiceCatalog()[0].Content);
    }
    [Theory]
    [InlineData(1,false)] [InlineData(2,true)] [InlineData(4,true)] [InlineData(8,true)] [InlineData(9,false)]
    public void ReadingTimeHasMatchingServerBounds(int minutes,bool valid)
    {
        var result=new GenerateAiBlogValidator().Validate(new GenerateAiBlogCommand("Spa rituals",1,ReadingMinutes:minutes));
        Assert.Equal(valid,result.IsValid);
    }
}
