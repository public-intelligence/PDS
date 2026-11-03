namespace PDS.Domain.DTOs.Auth;

public record AuthTokenResponseDto
{
    public required string AccessToken { get; init; }
    public bool MustChangePassword { get; init; }
}
