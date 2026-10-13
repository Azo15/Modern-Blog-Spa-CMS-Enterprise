using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebUI.Controllers;

namespace BusinessLayer.Tests;

public class UserDeletionTests
{
    [Fact]
    public async Task RestrictedUserDeletionPreservesPhotoAndReturnsToUserList()
    {
        var users = new Mock<UserManager<AppUser>>(Mock.Of<IUserStore<AppUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        users.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(new AppUser { Id = 4 });
        users.Setup(u => u.FindByIdAsync("2")).ReturnsAsync(new AppUser { Id = 2, Image = "users/photo.png" });
        users.Setup(u => u.DeleteAsync(It.IsAny<AppUser>())).ThrowsAsync(new DbUpdateException("Restricted relation"));
        var files = new Mock<IFileHelper>(MockBehavior.Strict);
        var context = new DefaultHttpContext();
        var controller = new AdminUserListController(users.Object, files.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
            TempData = new TempDataDictionary(context, Mock.Of<ITempDataProvider>())
        };
        var result = Assert.IsType<RedirectToActionResult>(await controller.Delete(2));
        Assert.Equal("UserList", result.ActionName);
        Assert.Equal("warning", controller.TempData["ToastType"]);
        files.VerifyNoOtherCalls();
    }
}
