using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.Locations;

public sealed class LocationFeatureService(IAddressLocationService addressLocationService)
{
    public async Task<IResult> GeocodeAddressAsync(
        string? address,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return Results.BadRequest("An address is required.");
        }

        try
        {
            var resolvedLocation = await addressLocationService.ResolveAddressAsync(address, cancellationToken);
            return resolvedLocation is null ? Results.NotFound() : Results.Ok(resolvedLocation);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while geocoding address '{Address}'.", address);
            return Results.Problem("An error occurred while geocoding the address.", statusCode: 500);
        }
    }

    public async Task<IResult> ResolveHexGridByAddressAsync(
        string? address,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return Results.BadRequest("An address is required.");
        }

        try
        {
            var resolvedLocation = await addressLocationService.ResolveAddressAsync(address, cancellationToken);
            if (resolvedLocation is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                resolvedLocation.Address,
                resolvedLocation.Latitude,
                resolvedLocation.Longitude,
                resolvedLocation.HexGridId
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while resolving hex grid for address '{Address}'.", address);
            return Results.Problem("An error occurred while resolving the hex grid id.", statusCode: 500);
        }
    }
}
