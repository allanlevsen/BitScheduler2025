using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IAddressLocationService
{
    Task<ResolvedAddressLocation?> ResolveAddressAsync(string address, CancellationToken cancellationToken = default);
    int? ResolveHexGridId(double latitude, double longitude);
}
