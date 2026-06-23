using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitClientService
{
    Task<BitClientListItem?> GetClientAsync(int bitClientId, CancellationToken cancellationToken = default);

    Task<BitClientListItem?> GetFirstClientAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BitClientListItem>> ListClientsAsync(CancellationToken cancellationToken = default);
}
