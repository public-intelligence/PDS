using Microsoft.Extensions.Logging;
using PDS.Service.Services;
using System.Net;
using System.Net.Http.Headers;

namespace PDS.Service.Http;

public sealed class GobidM2MBearerHandler : DelegatingHandler
{
    private readonly IGobidM2MTokenProvider _tokenProvider;
    private readonly ILogger<GobidM2MBearerHandler> _logger;

    public GobidM2MBearerHandler(
        IGobidM2MTokenProvider tokenProvider,
        ILogger<GobidM2MBearerHandler> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Buffer content so it can be re-sent on a 401 retry
        if (request.Content is not null)
            await request.Content.LoadIntoBufferAsync();

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Recibido 401 en llamada M2M; forzando renovación de token y reintentando.");

            response.Dispose();

            await _tokenProvider.ForceRefreshAsync(cancellationToken);

            var newToken = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}
