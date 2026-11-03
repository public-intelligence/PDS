namespace PDS.Domain.DTOs.Auth;

public sealed record GobidVerifyTokenRequest
{
    public required string Token { get; init; }
}
