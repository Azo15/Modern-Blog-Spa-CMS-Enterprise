using BusinessLayer;
using BusinessLayer.Abstract;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebUI.Controllers;
using WebUI.Models;

namespace BusinessLayer.Tests;

public sealed class ProfileAvatarTests
{
    [Theory]
    [InlineData("optimized.webp", "uploads/avatars/optimized.webp")]
    [InlineData(null, "legacy/user.jpg")]
    public async Task ProfilePersistsRootRelativeAvatarPathAndRetainsExistingImageWhenNoUpload(string? optimized, string expected)
    {
        var user = new AppUser { Id = 2, FirstName = "Before", LastName = "Test", Image = "legacy/user.jpg", IsActive = true };
        var users = new Mock<UserManager<AppUser>>(Mock.Of<IUserStore<AppUser>>(), Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<AppUser>>(), Array.Empty<IUserValidator<AppUser>>(), Array.Empty<IPasswordValidator<AppUser>>(),
            Mock.Of<ILookupNormalizer>(), new IdentityErrorDescriber(), Mock.Of<IServiceProvider>(), NullLogger<UserManager<AppUser>>.Instance);
        users.Setup(x => x.FindByIdAsync("2")).ReturnsAsync(user);
        users.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(false);
        users.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        var signIn = new Mock<SignInManager<AppUser>>(users.Object, Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(), Options.Create(new IdentityOptions()), NullLogger<SignInManager<AppUser>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<AppUser>>());
        signIn.Setup(x => x.RefreshSignInAsync(user)).Returns(Task.CompletedTask);
        var images = new Mock<IImageOptimizationService>();
        images.Setup(x => x.StoreAsync(It.IsAny<IFormFile?>(), "avatars", It.IsAny<CancellationToken>())).ReturnsAsync(optimized);
        var text = new Mock<IStringLocalizer<SharedResource>>();
        text.Setup(x => x["ProfileUpdated"]).Returns(new LocalizedString("ProfileUpdated", "Profile updated"));
        var context = new DefaultHttpContext { User = AuthorizationTests.Principal(AppRoles.Customer, "2") };
        var controller = new ProfileController(users.Object, signIn.Object, Mock.Of<ISender>(), images.Object, text.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
        Assert.IsType<RedirectToActionResult>(await controller.Update(new ProfileEditModel { FirstName = " After ", LastName = " Test " }, default));
        Assert.Equal(expected, user.Image);
        Assert.Equal("After", user.FirstName);
        Assert.Equal("Test", user.LastName);
        signIn.Verify(x => x.RefreshSignInAsync(user), Times.Once);
    }
}
