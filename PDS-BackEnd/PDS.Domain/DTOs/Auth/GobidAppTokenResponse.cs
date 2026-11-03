namespace PDS.Domain.DTOs.Auth;

public sealed record GobidAppTokenResponse
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = "Bearer";
}
