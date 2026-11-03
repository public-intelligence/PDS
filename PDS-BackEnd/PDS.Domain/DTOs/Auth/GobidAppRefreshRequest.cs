namespace PDS.Domain.DTOs.Auth;

public sealed record GobidAppRefreshRequest
{
    public required string RefreshToken { get; init; }
}
