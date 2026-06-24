using BitScheduleServices.Features.Locations;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspireBitSchedule.Tests;

public sealed class LocationFeatureServiceTests
{
    [Fact]
    public async Task ResolveHexGridByAddressAsync_ReturnsOkWhenAddressResolvesWithoutHexGridId()
    {
        var service = new LocationFeatureService(new StubAddressLocationService(
            new ResolvedAddressLocation("401 Meadowview Drive, Fort Saskatchewan, AB", 53.7123, -113.2134, null)));

        var result = await service.ResolveHexGridByAddressAsync(
            "401 Meadowview Drive, Fort Saskatchewan, AB",
            NullLogger.Instance,
            CancellationToken.None);

        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status200OK, statusCodeResult.StatusCode);

        var valueResult = Assert.IsAssignableFrom<IValueHttpResult>(result);
        Assert.NotNull(valueResult.Value);
    }

    private sealed class StubAddressLocationService(ResolvedAddressLocation? resolvedLocation) : IAddressLocationService
    {
        public Task<ResolvedAddressLocation?> ResolveAddressAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(resolvedLocation);
        }

        public int? ResolveHexGridId(double latitude, double longitude)
        {
            return resolvedLocation?.HexGridId;
        }
    }
}
