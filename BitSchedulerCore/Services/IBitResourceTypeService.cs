using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitResourceTypeService
{
    Task<BitResourceTypeListItem> CreateResourceTypeAsync(BitResourceTypeRequest request, CancellationToken cancellationToken = default);
    Task<BitResourceTypeListItem?> GetResourceTypeAsync(int bitResourceTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BitResourceTypeListItem>> ListResourceTypesAsync(CancellationToken cancellationToken = default);
    Task<BitResourceTypeListItem?> UpdateResourceTypeAsync(int bitResourceTypeId, BitResourceTypeRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteResourceTypeAsync(int bitResourceTypeId, CancellationToken cancellationToken = default);
}
