using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitEventService
{
    Task<BitEvent> CreateEventAsync(int clientId, BitEventRequest request, CancellationToken cancellationToken = default);
    Task<BitEvent?> GetEventAsync(int clientId, int bitEventId, CancellationToken cancellationToken = default);
}
