using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;

namespace BitScheduleServices.Features.Clients;

public sealed class SessionBitClientAccessor(
    IHttpContextAccessor httpContextAccessor,
    IBitClientService bitClientService) : ICurrentBitClientAccessor
{
    private const string SessionKey = "CurrentBitClientId";

    public async Task<BitClientListItem> GetCurrentClientAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = GetHttpContext();
        var session = httpContext.Session;
        var bitClientId = session.GetInt32(SessionKey);

        if (bitClientId.HasValue)
        {
            var existingClient = await bitClientService.GetClientAsync(bitClientId.Value, cancellationToken);
            if (existingClient is not null)
            {
                return existingClient;
            }
        }

        var firstClient = await bitClientService.GetFirstClientAsync(cancellationToken);
        if (firstClient is null)
        {
            throw new InvalidOperationException("No BitClients are available.");
        }

        session.SetInt32(SessionKey, firstClient.BitClientId);
        return firstClient;
    }

    public async Task<int> GetCurrentClientIdAsync(CancellationToken cancellationToken = default)
    {
        var currentClient = await GetCurrentClientAsync(cancellationToken);
        return currentClient.BitClientId;
    }

    public async Task<BitClientListItem> SetCurrentClientAsync(int bitClientId, CancellationToken cancellationToken = default)
    {
        if (bitClientId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitClientId), "A valid BitClientId is required.");
        }

        var client = await bitClientService.GetClientAsync(bitClientId, cancellationToken);
        if (client is null)
        {
            throw new InvalidOperationException($"BitClient {bitClientId} does not exist.");
        }

        GetHttpContext().Session.SetInt32(SessionKey, bitClientId);
        return client;
    }

    private HttpContext GetHttpContext()
    {
        return httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("The current HTTP context is not available.");
    }
}
