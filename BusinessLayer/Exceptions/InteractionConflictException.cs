namespace BusinessLayer.Exceptions;

// A bounded retry could not safely apply an interaction or account mutation.
// The HTTP boundary presents a localized 409 without exposing database details.
public sealed class InteractionConflictException(Exception? innerException = null)
    : Exception("InteractionConflict", innerException);
