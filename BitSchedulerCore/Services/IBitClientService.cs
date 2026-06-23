using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitClientService
{
    Task<BitClientListItem> CreateClientAsync(BitClientRequest request, CancellationToken cancellationToken = default);

    Task<BitClientListItem?> GetClientAsync(int bitClientId, CancellationToken cancellationToken = default);

    Task<BitClientListItem?> GetFirstClientAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BitClientListItem>> ListClientsAsync(CancellationToken cancellationToken = default);

    Task<BitClientListItem?> UpdateClientAsync(int bitClientId, BitClientRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteClientAsync(int bitClientId, CancellationToken cancellationToken = default);
}
