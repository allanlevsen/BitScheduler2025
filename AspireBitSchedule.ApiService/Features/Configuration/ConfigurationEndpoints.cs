using Microsoft.Extensions.Options;

namespace AspireBitSchedule.ApiService.Features.Configuration;

internal static class ConfigurationEndpoints
{
    public static IEndpointRouteBuilder MapConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/config/google-mapping", GetGoogleMappingConfiguration);
        return endpoints;
    }

    private static IResult GetGoogleMappingConfiguration(IOptions<GoogleMappingOptions> options)
    {
        var googleMapping = options.Value;

        return Results.Ok(new GoogleMappingClientConfiguration(
            googleMapping.ApiKey,
            googleMapping.MapId,
            googleMapping.Region,
            googleMapping.Language,
            googleMapping.Libraries,
            new GoogleMapCenterConfiguration(
                googleMapping.DefaultCenter.Latitude,
                googleMapping.DefaultCenter.Longitude),
            googleMapping.DefaultZoom));
    }
}
