namespace BusinessLayer.Dtos;

/// <summary>A signed bearer token. No passwords, email addresses or Identity entities are returned.</summary>
public sealed record ApiTokenDto(string AccessToken, string TokenType, DateTimeOffset ExpiresAtUtc, int UserId, IReadOnlyList<string> Roles);

/// <summary>The current API account's limited identity and access profiles.</summary>
public sealed record ApiIdentityDto(int UserId, string? DisplayName, IReadOnlyList<string> Roles);
