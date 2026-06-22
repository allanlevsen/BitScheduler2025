using BitSchedulerCore.Models;
using BitScheduleServices.Features.Resources;

namespace AspireBitSchedule.ApiService.Features.Resources;

internal static class ResourceEndpoints
{
    public static IEndpointRouteBuilder MapResourceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/resources");
        group.MapGet("/", ListResourcesAsync);
        group.MapGet("/types", ListResourceTypesAsync);
        group.MapGet("/{bitResourceId:int}", GetResourceAsync);
        group.MapPost("/", CreateResourceAsync);
        group.MapPut("/{bitResourceId:int}", UpdateResourceAsync);
        group.MapDelete("/{bitResourceId:int}", DeleteResourceAsync);

        endpoints.MapGet("/Resources", ListResourcesAsync);
        endpoints.MapGet("/Resources/Types", ListResourceTypesAsync);
        endpoints.MapGet("/Resources/{bitResourceId:int}", GetResourceAsync);
        endpoints.MapPost("/Resources", CreateResourceAsync);
        endpoints.MapPut("/Resources/{bitResourceId:int}", UpdateResourceAsync);
        endpoints.MapDelete("/Resources/{bitResourceId:int}", DeleteResourceAsync);

        return endpoints;
    }

    private static Task<IResult> ListResourcesAsync(
        ResourceFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListResourcesAsync(logger, cancellationToken);
    }

    private static Task<IResult> ListResourceTypesAsync(
        ResourceFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListResourceTypesAsync(logger, cancellationToken);
    }

    private static Task<IResult> GetResourceAsync(
        int bitResourceId,
        ResourceFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.GetResourceAsync(bitResourceId, logger, cancellationToken);
    }

    private static Task<IResult> CreateResourceAsync(
        BitResourceRequest request,
        ResourceFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.CreateResourceAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> UpdateResourceAsync(
        int bitResourceId,
        BitResourceRequest request,
        ResourceFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.UpdateResourceAsync(bitResourceId, request, logger, cancellationToken);
    }

    private static Task<IResult> DeleteResourceAsync(
        int bitResourceId,
        ResourceFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.DeleteResourceAsync(bitResourceId, logger, cancellationToken);
    }
}
