namespace PDS.Domain.DTOs.Auth;

public sealed record GobidAppLoginRequest
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string? Scope { get; init; }
}
