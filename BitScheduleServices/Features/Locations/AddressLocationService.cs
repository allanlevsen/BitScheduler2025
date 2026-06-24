using BitSchedulerCore.Models;
using BitSchedulerCore.Services;

namespace BitScheduleServices.Features.Locations;

public sealed class AddressLocationService(
    IGeocodingService geocodingService,
    IHexGridSearchService hexGridSearchService) : IAddressLocationService
{
    public async Task<ResolvedAddressLocation?> ResolveAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var normalizedAddress = address.Trim();
        var geocodingResult = await geocodingService.GeocodeAsync(normalizedAddress, cancellationToken);
        if (geocodingResult is null)
        {
            return null;
        }

        return new ResolvedAddressLocation(
            geocodingResult.FormattedAddress,
            geocodingResult.Latitude,
            geocodingResult.Longitude,
            ResolveHexGridId(geocodingResult.Latitude, geocodingResult.Longitude));
    }

    public int? ResolveHexGridId(double latitude, double longitude)
    {
        return hexGridSearchService.GetGridId(latitude, longitude);
    }
}
