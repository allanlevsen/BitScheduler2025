using BitSchedulerCore.Models;

namespace BitScheduleServices.Features.Clients;

public interface ICurrentBitClientAccessor
{
    Task<BitClientListItem> GetCurrentClientAsync(CancellationToken cancellationToken = default);

    Task<int> GetCurrentClientIdAsync(CancellationToken cancellationToken = default);

    Task<BitClientListItem> SetCurrentClientAsync(int bitClientId, CancellationToken cancellationToken = default);
}
