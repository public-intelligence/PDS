using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PDS.Domain.DTOs.Auth;

namespace PDS.Service.Services;

public sealed class GobidM2MTokenProvider : IGobidM2MTokenProvider, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GobidM2MTokenProvider> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private const int ExpiryBufferSeconds = 60;

    private string? _accessToken;
    private string? _refreshToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public GobidM2MTokenProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<GobidM2MTokenProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Fast path: token still valid
        if (!string.IsNullOrWhiteSpace(_accessToken)
            && DateTimeOffset.UtcNow.AddSeconds(ExpiryBufferSeconds) < _expiresAt)
        {
            return _accessToken;
        }

        await _semaphore.WaitAsync(ct);
        try
        {
            // Re-check after acquiring semaphore (double-check locking)
            if (!string.IsNullOrWhiteSpace(_accessToken)
                && DateTimeOffset.UtcNow.AddSeconds(ExpiryBufferSeconds) < _expiresAt)
            {
                return _accessToken;
            }

            // Try refresh if we have a refresh token
            if (!string.IsNullOrWhiteSpace(_refreshToken))
            {
                var refreshed = await TryRefreshAsync(ct);
                if (refreshed) return _accessToken!;
            }

            // Full login fallback
            await FetchNewTokenAsync(ct);
            return _accessToken!;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ForceRefreshAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            _accessToken = null;
            _expiresAt = DateTimeOffset.MinValue;
            await FetchNewTokenAsync(ct);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<bool> TryRefreshAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<IGobidM2MClient>();
            var result = await client.RefreshTokenAsync(_refreshToken!, ct);

            if (result.IsSuccess)
            {
                StoreToken(result.Value);
                _logger.LogDebug("Token M2M renovado correctamente.");
                return true;
            }

            _logger.LogWarning(
                "Refresh de token M2M falló ({StatusCode}), se intentará login completo.",
                result.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Excepción durante refresh de token M2M.");
            return false;
        }
    }

    private async Task FetchNewTokenAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IGobidM2MClient>();
        var result = await client.GetTokenAsync(ct);

        if (!result.IsSuccess)
            throw new InvalidOperationException(
                $"No se pudo obtener token M2M de GOBID: {result.Error?.Title}");

        StoreToken(result.Value);
        _logger.LogDebug("Token M2M obtenido correctamente.");
    }

    private void StoreToken(GobidAppTokenResponse token)
    {
        _accessToken = token.AccessToken;
        _refreshToken = token.RefreshToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
    }

    public void Dispose() => _semaphore.Dispose();
}
