using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitResourceService
{
    Task<IReadOnlyList<BitResourceListItem>> ListResourcesAsync(int clientId, CancellationToken cancellationToken = default);
}
