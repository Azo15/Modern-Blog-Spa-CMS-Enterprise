using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using WebUI.Logging;

namespace BusinessLayer.Tests;

public sealed class LogPrivacyTests
{
    private sealed class CaptureSink : ILogEventSink
    {
        public readonly List<LogEvent> Events = [];
        public void Emit(LogEvent value) => Events.Add(value);
        public string Json()
        {
            using var writer = new StringWriter();
            foreach (var value in Events) new JsonFormatter().Format(value, writer);
            return writer.ToString();
        }
    }

    [Theory]
    [InlineData("contact alice@example.com", "alice@example.com")]
    [InlineData("password=P@ssword123!", "P@ssword123!")]
    [InlineData("Password: \"two word secret\"", "two word secret")]
    [InlineData("Bearer eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.secretSig", "eyJhbGciOiJIUzI1NiJ9")]
    [InlineData("token=opaque-credential-123", "opaque-credential-123")]
    [InlineData("card 4111 1111 1111 1111", "4111")]
    [InlineData("redis://user:server-secret@localhost:6379", "server-secret")]
    [InlineData("GET /?email=user%40example.com", "user")]
    [InlineData("GET /?token=opaque%20credential&safe=ok", "credential")]
    [InlineData("GET /?email=user%2540example.com", "user")]
    public void UnstructuredSecretsAreRedacted(string input, string secret)
    {
        var result = SensitiveDataRedactor.Redact(input);
        Assert.DoesNotContain(secret, result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(SensitiveDataRedactor.Mask, result);
    }

    [Fact]
    public void NestedStructuredValuesAndExceptionChainsNeverReachTheOutput()
    {
        var captured = new CaptureSink();
        using var output = new LoggerConfiguration().WriteTo.Sink(captured).CreateLogger();
        using var logger = new LoggerConfiguration().WriteTo.Sink(new SensitiveDataRedactingSink(output)).CreateLogger();
        var exception = new InvalidOperationException("SMTP failed for person@example.com password=exception-secret",
            new Exception("Authorization: Bearer nested-token"));
        logger.Error(exception, "Request failed {@Payload} {Password} {Authorization}",
            new { Email = "member@example.com", Items = new[] { new { RefreshToken = "private-refresh", Note = "4111111111111111" } }, Dictionary = new Dictionary<string, object> { ["ApiKey"] = "private-api", ["Text"] = "plain note" } },
            "literal-password", "Bearer private-access");
        var json = captured.Json();
        foreach (var secret in new[] { "person@example.com", "member@example.com", "exception-secret", "nested-token", "private-refresh", "4111111111111111", "private-api", "literal-password", "private-access" })
            Assert.DoesNotContain(secret, json);
        Assert.Contains("plain note", json);
        Assert.Contains("SMTP failed", json);
        Assert.Contains("[REDACTED]", json);
    }

    [Fact]
    public void LiteralTemplatesAndSafeDiagnosticsAreHandledWithoutLosingUsefulContext()
    {
        var captured = new CaptureSink();
        using var output = new LoggerConfiguration().WriteTo.Sink(captured).CreateLogger();
        using var logger = new LoggerConfiguration().WriteTo.Sink(new SensitiveDataRedactingSink(output)).CreateLogger();
        logger.Information("Login user@example.com password=literal-secret failed at {Endpoint} ({StatusCode})", "/api/v1/auth/login", 401);
        var json = captured.Json();
        Assert.DoesNotContain("literal-secret", json);
        Assert.DoesNotContain("user@example.com", json);
        Assert.Contains("/api/v1/auth/login", json);
        Assert.Contains("401", json);
    }
}
