using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitResourceTypeService
{
    Task<BitResourceTypeListItem> CreateResourceTypeAsync(BitResourceTypeRequest request, CancellationToken cancellationToken = default);
    Task<BitResourceTypeListItem?> GetResourceTypeAsync(int clientId, int bitResourceTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BitResourceTypeListItem>> ListResourceTypesAsync(int clientId, CancellationToken cancellationToken = default);
    Task<BitResourceTypeListItem?> UpdateResourceTypeAsync(int clientId, int bitResourceTypeId, BitResourceTypeRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteResourceTypeAsync(int clientId, int bitResourceTypeId, CancellationToken cancellationToken = default);
}
