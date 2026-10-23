using System.ComponentModel.DataAnnotations;
using WebUI.Models;
namespace BusinessLayer.Tests;
public sealed class PasswordRecoveryViewModelTests
{
    [Theory]
    [InlineData(" owner@example.test ", true)]
    [InlineData("", false)]
    [InlineData("invalid-address", false)]
    public void ForgotModelNormalizesAndValidatesEmail(string email, bool valid)
    {
        var model = new ForgotPasswordViewModel { Email = email };
        Assert.Equal(email.Trim(), model.Email);
        Assert.Equal(valid, Validator.TryValidateObject(model, new ValidationContext(model), [], true));
    }
    [Theory]
    [InlineData("1", "valid_token-123", true)]
    [InlineData("0", "valid_token-123", false)]
    [InlineData("2147483648", "valid_token-123", false)]
    [InlineData("not-an-id", "valid_token-123", false)]
    [InlineData("1", "invalid token", false)]
    [InlineData("1", "", false)]
    public void ResetModelRejectsInvalidIdentityAndToken(string id, string token, bool valid)
    {
        var model = new ResetPasswordViewModel { UserId = id, Token = token, Password = "Test-password-123", ConfirmPassword = "Test-password-123" };
        Assert.Equal(valid, Validator.TryValidateObject(model, new ValidationContext(model), [], true));
    }
}
