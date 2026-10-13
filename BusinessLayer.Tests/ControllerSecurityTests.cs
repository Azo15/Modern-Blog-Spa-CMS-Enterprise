using AutoMapper;
using BusinessLayer.Abstract;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebUI.Controllers;
using WebUI.Data;
using Microsoft.AspNetCore.Hosting;

namespace BusinessLayer.Tests;

public class ControllerSecurityTests
{
    [Theory]
    [InlineData("edit")]
    [InlineData("editPost")]
    [InlineData("delete")]
    [InlineData("deletePost")]
    [InlineData("submit")]
    public async Task AnotherAuthorsIdReturnsForbidBeforeMutation(string action)
    {
        using var provider = AuthorizationTests.Provider();
        var blogs = new Mock<IBlogService>();
        blogs.Setup(b => b.GetByIDAsync(42, It.IsAny<System.Linq.Expressions.Expression<Func<Blog, object>>[]>()))
            .ReturnsAsync(new Blog { Id = 42, AppUserId = 2 });
        var workflow = new Mock<IBlogWorkflowService>(MockBehavior.Strict);
        var files = new Mock<IFileHelper>(MockBehavior.Strict);
        var controller = new AdminBlogController(blogs.Object, Mock.Of<ICatagoryService>(), files.Object,
            Mock.Of<IWebHostEnvironment>(), Mock.Of<IMapper>(), provider.GetRequiredService<IAuthorizationService>(), workflow.Object)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = AuthorizationTests.Principal(AppRoles.Author, "1") } } };
        IActionResult result = action switch
        {
            "edit" => await controller.Edit(42),
            "editPost" => await controller.Edit(new BlogUpdateDto { Id = 42 }, 42, null),
            "delete" => await controller.Delete(42),
            "deletePost" => await controller.DeleteConfirmed(42),
            _ => await controller.SubmitForReview(42)
        };
        Assert.IsType<ForbidResult>(result);
        workflow.VerifyNoOtherCalls(); files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ProductionSeederDoesNotEvenResolveDatabaseOrIdentityServices()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns("Production");
        using var provider = new ServiceCollection().AddSingleton(environment.Object).BuildServiceProvider();
        await DataSeeder.SeedAsync(provider);
    }
}
