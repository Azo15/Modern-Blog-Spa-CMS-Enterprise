using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using WebUI.Middleware;

namespace BusinessLayer.Tests;

public class ExceptionMappingTests
{
    [Fact]
    public async Task ConcurrentMutationReturnsConflictWithoutDatabaseDetails()
    {
        using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(_ => throw new BusinessLayer.Exceptions.InteractionConflictException(
            new InvalidOperationException("secret database details")), NullLogger<GlobalExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("conflicted", body);
        Assert.DoesNotContain("secret database", body);
    }
    [Fact]
    public async Task ImperativeAuthorizationFailureMapsToForbidden()
    {
        var context = new DefaultHttpContext();
        var middleware = new GlobalExceptionMiddleware(_ => throw new UnauthorizedAccessException(),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task PipelineValidationFailureMapsToBadRequest()
    {
        using var services = new ServiceCollection().AddLogging().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(_ => throw new ValidationException(
            new[] { new ValidationFailure("PageIndex", "Invalid page") }), NullLogger<GlobalExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        Assert.Contains("PageIndex", await new StreamReader(context.Response.Body).ReadToEndAsync());
    }
}
