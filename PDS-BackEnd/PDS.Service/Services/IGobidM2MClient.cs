using PDS.Domain.DTOs.Auth;
using PDS.Domain.Response;

namespace PDS.Service.Services;

public interface IGobidM2MClient
{
    Task<IHttpResult<GobidAppTokenResponse>> GetTokenAsync(CancellationToken ct = default);
    Task<IHttpResult<GobidAppTokenResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default);
}
