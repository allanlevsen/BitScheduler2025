using BitScheduleServices.Features.Locations;

namespace AspireBitSchedule.ApiService.Features.Locations;

internal static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/locations");
        group.MapGet("/geocode", GeocodeAddressAsync);
        group.MapGet("/hex-grid", ResolveHexGridByAddressAsync);

        endpoints.MapGet("/Locations/Geocode", GeocodeAddressAsync);
        endpoints.MapGet("/Locations/HexGrid", ResolveHexGridByAddressAsync);

        return endpoints;
    }

    private static Task<IResult> GeocodeAddressAsync(
        string? address,
        LocationFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.GeocodeAddressAsync(address, logger, cancellationToken);
    }

    private static Task<IResult> ResolveHexGridByAddressAsync(
        string? address,
        LocationFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ResolveHexGridByAddressAsync(address, logger, cancellationToken);
    }
}
