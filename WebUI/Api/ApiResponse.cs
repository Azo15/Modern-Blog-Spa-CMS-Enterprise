namespace WebUI.Api;

/// <summary>The success envelope used by every v1 API endpoint. Failures use RFC 7807.</summary>
/// <typeparam name="T">The safe response DTO type.</typeparam>
public sealed record ApiResponse<T>(bool Success, string Message, T Data, string[]? Errors, int StatusCode)
{
    public static ApiResponse<T> Ok(T data, string message = "OK", int statusCode = StatusCodes.Status200OK)
        => new(true, message, data, null, statusCode);
}
