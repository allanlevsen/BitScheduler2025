using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IBitEventService
{
    Task<BitEvent> CreateEventAsync(int clientId, BitEventRequest request, CancellationToken cancellationToken = default);
    Task<BitEvent?> GetEventAsync(int clientId, int bitEventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BitEvent>> ListEventsAsync(int clientId, BitEventListRequest? request, CancellationToken cancellationToken = default);
    Task<BitEvent?> UpdateEventAsync(int clientId, int bitEventId, BitEventRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(int clientId, int bitEventId, CancellationToken cancellationToken = default);
}
