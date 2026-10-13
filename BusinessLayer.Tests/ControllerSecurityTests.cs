using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebUI.Controllers;
using WebUI.Data;
using MediatR;
namespace BusinessLayer.Tests;
public class ControllerSecurityTests
{
    [Fact]
    public async Task AdminControllerDelegatesOwnershipToTheProtectedQuery()
    {
        var sender=new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(s=>s.Send(It.Is<GetBlogByIdQuery>(q=>q.Id==42),It.IsAny<CancellationToken>())).ThrowsAsync(new UnauthorizedAccessException());
        var controller=new AdminBlogController(sender.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>controller.Edit(42,default));
        Assert.Equal(typeof(ISender),Assert.Single(typeof(AdminBlogController).GetConstructors()).GetParameters().Single().ParameterType);
    }
    [Fact]
    public async Task ProductionSeederDoesNotEvenResolveDatabaseOrIdentityServices()
    {
        var environment=new Mock<IWebHostEnvironment>();environment.SetupGet(e=>e.EnvironmentName).Returns("Production");
        using var provider=new ServiceCollection().AddSingleton(environment.Object).BuildServiceProvider();await DataSeeder.SeedAsync(provider);
    }
}
