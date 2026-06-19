using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IGeocodingService
{
    Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
}
