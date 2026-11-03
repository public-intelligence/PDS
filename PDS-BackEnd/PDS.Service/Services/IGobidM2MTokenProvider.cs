namespace PDS.Service.Services;

public interface IGobidM2MTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);
    Task ForceRefreshAsync(CancellationToken ct = default);
}
