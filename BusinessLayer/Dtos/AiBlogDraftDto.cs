namespace BusinessLayer.Dtos;
public sealed record AiBlogPrompt(string Topic,string Category,string Language,string Tone);
public sealed record AiBlogDraftDto(string Title,string Subtitle,string Summary,string Html);
public sealed class AiBlogUnavailableException(string message) : Exception(message);
