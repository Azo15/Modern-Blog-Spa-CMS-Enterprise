using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace WebUI.Logging;

/// <summary>Only sanitized events are written to the enclosed console/file logger.</summary>
public sealed class SensitiveDataRedactingSink(Serilog.ILogger destination) : ILogEventSink, IDisposable
{
    private static readonly MessageTemplateParser Parser = new();
    public void Emit(LogEvent logEvent)
    {
        var safeException = logEvent.Exception is null ? null : new RedactedException(logEvent.Exception);
        var safe = new LogEvent(logEvent.Timestamp, logEvent.Level, safeException,
            Parser.Parse(SensitiveDataRedactor.Redact(logEvent.MessageTemplate.Text)),
            logEvent.Properties.Select(pair => SensitiveDataRedactor.RedactProperty(new LogEventProperty(pair.Key, pair.Value))),
            logEvent.TraceId ?? default, logEvent.SpanId ?? default);
        destination.Write(safe);
    }

    public void Dispose() { if (destination is IDisposable disposable) disposable.Dispose(); }

    // Do not preserve original InnerException, Data, Source or stack fields that can leak secrets.
    private sealed class RedactedException(Exception original) : Exception(SensitiveDataRedactor.Redact(original.Message))
    {
        private readonly string safeDetails = SensitiveDataRedactor.Redact(original.ToString());
        public override string ToString() => safeDetails;
    }
}
