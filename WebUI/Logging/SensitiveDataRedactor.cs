using System.Globalization;
using System.Text.RegularExpressions;
using Serilog.Events;

namespace WebUI.Logging;

/// <summary>Redacts common personal data and credentials before events reach any application sink.</summary>
public static class SensitiveDataRedactor
{
    public const string Mask = "[REDACTED]";
    private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(100);
    private static readonly Regex SensitiveName = Pattern(@"(?:password|passwd|pwd|secret|token|authorization|cookie|credential|email|mailaddress|cardnumber|creditcard|cvv|cvc|apikey|connectionstring|signingkey|securitystamp)");
    private static readonly Regex Credentials = Pattern("(?<key>\\b(?:password|passwd|pwd|secret|token|access[_-]?token|refresh[_-]?token|id[_-]?token|api[_-]?key|client[_-]?secret|authorization|cookie|cvv|cvc)\\b[\\\"']?\\s*[:=]\\s*)(?:[\\\"'][^\\\"'\\r\\n]*[\\\"']|[^,;}&\\r\\n]+)");
    private static readonly Regex Bearer = Pattern(@"\b(?:Bearer|Basic)\s+[A-Za-z0-9._~+/=-]+");
    private static readonly Regex Jwt = Pattern(@"\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b");
    private static readonly Regex Email = Pattern(@"[A-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[A-Z0-9](?:[A-Z0-9.-]*[A-Z0-9])?\.[A-Z]{2,}");
    private static readonly Regex Card = Pattern(@"(?<!\d)(?:\d[ -]?){12,18}\d(?!\d)");
    private static readonly Regex UrlUserInfo = Pattern(@"(?<scheme>\b[a-z][a-z0-9+.-]*://)[^\s/@:]+:[^\s/@]+@");
    private static Regex Pattern(string value) => new(value, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, Timeout);

    public static bool IsSensitiveName(string name)
    {
        try { return SensitiveName.IsMatch(name.Replace("_", "").Replace("-", "")); }
        catch (RegexMatchTimeoutException) { return true; }
    }

    public static string Redact(string value)
    {
        try
        {
            // Framework request URLs and exception strings may contain percent-encoded personal data.
            // Decode twice to detect common double-encoding, then apply the same fail-closed patterns.
            for (var pass = 0; pass < 2 && value.Contains('%'); pass++) value = Uri.UnescapeDataString(value);
            value = Bearer.Replace(value, "Bearer " + Mask);
            value = Jwt.Replace(value, Mask);
            value = Credentials.Replace(value, m => m.Groups["key"].Value + Mask);
            value = Email.Replace(value, Mask);
            value = Card.Replace(value, Mask);
            return UrlUserInfo.Replace(value, m => m.Groups["scheme"].Value + Mask + "@");
        }
        catch (Exception ex) when (ex is RegexMatchTimeoutException or UriFormatException)
        { return "[REDACTED: sensitive message could not be parsed]"; }
    }

    public static LogEventPropertyValue RedactValue(LogEventPropertyValue value)
        => value switch
        {
            ScalarValue { Value: string text } => new ScalarValue(Redact(text)),
            ScalarValue { Value: Exception exception } => new ScalarValue(Redact(exception.ToString())),
            ScalarValue scalar when scalar.Value is long or ulong or decimal => new ScalarValue(Redact(Convert.ToString(scalar.Value, CultureInfo.InvariantCulture) ?? string.Empty)),
            ScalarValue scalar when scalar.Value is null or bool or byte or short or int or long or float or double or decimal or DateTime or DateTimeOffset or TimeSpan or Guid => scalar,
            ScalarValue scalar => new ScalarValue(Redact(Convert.ToString(scalar.Value, CultureInfo.InvariantCulture) ?? string.Empty)),
            SequenceValue sequence => new SequenceValue(sequence.Elements.Select(RedactValue)),
            StructureValue structure => new StructureValue(structure.Properties.Select(RedactProperty), structure.TypeTag is null ? null : Redact(structure.TypeTag)),
            DictionaryValue dictionary => new DictionaryValue(dictionary.Elements.Select(pair =>
                new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    new ScalarValue(Redact(Convert.ToString(pair.Key.Value, CultureInfo.InvariantCulture) ?? string.Empty)),
                    IsSensitiveName(Convert.ToString(pair.Key.Value, CultureInfo.InvariantCulture) ?? string.Empty) ? new ScalarValue(Mask) : RedactValue(pair.Value)))),
            _ => new ScalarValue(Mask)
        };

    public static LogEventProperty RedactProperty(LogEventProperty property)
        => new(property.Name, IsSensitiveName(property.Name) ? new ScalarValue(Mask) : RedactValue(property.Value));
}
