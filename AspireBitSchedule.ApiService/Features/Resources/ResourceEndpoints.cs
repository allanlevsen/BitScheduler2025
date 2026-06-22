using BitScheduleServices.Features.Resources;

namespace AspireBitSchedule.ApiService.Features.Resources;

internal static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/resources");
        group.MapGet("/", ListResourcesAsync);

        endpoints.MapGet("/Resources", ListResourcesAsync);

        return endpoints;
    }

    private static Task<IResult> ListResourcesAsync(
        ResourceFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListResourcesAsync(logger, cancellationToken);
    }
}
