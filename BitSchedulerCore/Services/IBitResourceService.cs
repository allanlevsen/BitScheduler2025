using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitResourceService
{
    Task<BitResourceListItem> CreateResourceAsync(int clientId, BitResourceRequest request, CancellationToken cancellationToken = default);
    Task<BitResourceListItem?> GetResourceAsync(int clientId, int bitResourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BitResourceListItem>> ListResourcesAsync(int clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BitResourceTypeListItem>> ListResourceTypesAsync(CancellationToken cancellationToken = default);
    Task<BitResourceListItem?> UpdateResourceAsync(int clientId, int bitResourceId, BitResourceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteResourceAsync(int clientId, int bitResourceId, CancellationToken cancellationToken = default);
}
