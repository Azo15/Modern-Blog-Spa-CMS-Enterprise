using System.Globalization;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.ValidationRules;
using EntityLayer.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace BusinessLayer.Tests;

public sealed class ValidationLocalizationTests
{
    private static ServiceProvider Services()
        => new ServiceCollection().AddLogging().AddLocalization(options => options.ResourcesPath = "Resources")
            .BuildServiceProvider();

    [Theory]
    [InlineData("tr-TR", "Bu alan zorunludur.", "En fazla 1000 karakter girin.")]
    [InlineData("en-US", "This field is required.", "Enter at most 1000 characters.")]
    public void CommentValidationUsesRealCultureResourcesAndFormatsLimits(string culture, string required, string maximum)
    {
        using var services = Services();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            var validator = new CreateCommentCommandValidator(services.GetRequiredService<IStringLocalizer<SharedResource>>());
            Assert.Contains(validator.Validate(new CreateCommentCommand(1, "")).Errors, error => error.ErrorMessage == required);
            Assert.Contains(validator.Validate(new CreateCommentCommand(1, new string('x', 1001))).Errors, error => error.ErrorMessage == maximum);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void ExistingValidatorFollowsCultureChangesAtValidationTime()
    {
        using var services = Services();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR");
            var validator = new GetPublicBlogsQueryValidator(services.GetRequiredService<IStringLocalizer<SharedResource>>());
            Assert.Contains(validator.Validate(new GetPublicBlogsQuery(PageSize: 101)).Errors,
                error => error.ErrorMessage == "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Contains(validator.Validate(new GetPublicBlogsQuery(PageSize: 101)).Errors,
                error => error.ErrorMessage == "Page size must be between 1 and 100.");
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void ParameterlessValidatorRetainsValidationForDirectServiceUsage()
    {
        Assert.False(new ContactFormValidators().Validate(new ContactForm { Email = "invalid" }).IsValid);
        Assert.False(new CreateBlogCommandValidator().Validate(new CreateBlogCommand("", "", "", 0)).IsValid);
        Assert.False(new CreateCommentCommandValidator().Validate(new CreateCommentCommand(0, "")).IsValid);
    }
}
