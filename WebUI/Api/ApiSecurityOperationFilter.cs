using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace WebUI.Api;

/// <summary>Documents anonymous operations and the standard error contract.</summary>
public sealed class ApiSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpoint = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if (endpoint.OfType<IAllowAnonymous>().Any()) operation.Security = [];
        foreach (var status in new[] { "400", "429", "500" })
            operation.Responses.TryAdd(status, ErrorResponse(context, status));
        if (endpoint.OfType<IAuthorizeData>().Any() && !endpoint.OfType<IAllowAnonymous>().Any())
            foreach (var status in new[] { "401", "403" }) operation.Responses.TryAdd(status, ErrorResponse(context, status));
    }

    private static OpenApiResponse ErrorResponse(OperationFilterContext context, string status) => new()
    {
        Description = $"HTTP {status} — RFC 7807 Problem Details",
        Content = new Dictionary<string, OpenApiMediaType>
        {
            ["application/problem+json"] = new() { Schema = context.SchemaGenerator.GenerateSchema(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), context.SchemaRepository) }
        }
    };
}
