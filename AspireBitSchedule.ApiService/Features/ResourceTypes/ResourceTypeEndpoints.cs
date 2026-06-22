using BitScheduleServices.Features.ResourceTypes;
using BitSchedulerCore.Models;

namespace AspireBitSchedule.ApiService.Features.ResourceTypes;

internal static class ResourceTypeEndpoints
{
    public static IEndpointRouteBuilder MapResourceTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/resource-types");
        group.MapGet("/", ListResourceTypesAsync);
        group.MapGet("/{bitResourceTypeId:int}", GetResourceTypeAsync);
        group.MapPost("/", CreateResourceTypeAsync);
        group.MapPut("/{bitResourceTypeId:int}", UpdateResourceTypeAsync);
        group.MapDelete("/{bitResourceTypeId:int}", DeleteResourceTypeAsync);

        endpoints.MapGet("/ResourceTypes", ListResourceTypesAsync);
        endpoints.MapGet("/ResourceTypes/{bitResourceTypeId:int}", GetResourceTypeAsync);
        endpoints.MapPost("/ResourceTypes", CreateResourceTypeAsync);
        endpoints.MapPut("/ResourceTypes/{bitResourceTypeId:int}", UpdateResourceTypeAsync);
        endpoints.MapDelete("/ResourceTypes/{bitResourceTypeId:int}", DeleteResourceTypeAsync);

        return endpoints;
    }

    private static Task<IResult> ListResourceTypesAsync(
        ResourceTypeFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListResourceTypesAsync(logger, cancellationToken);
    }

    private static Task<IResult> GetResourceTypeAsync(
        int bitResourceTypeId,
        ResourceTypeFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.GetResourceTypeAsync(bitResourceTypeId, logger, cancellationToken);
    }

    private static Task<IResult> CreateResourceTypeAsync(
        BitResourceTypeRequest request,
        ResourceTypeFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.CreateResourceTypeAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> UpdateResourceTypeAsync(
        int bitResourceTypeId,
        BitResourceTypeRequest request,
        ResourceTypeFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.UpdateResourceTypeAsync(bitResourceTypeId, request, logger, cancellationToken);
    }

    private static Task<IResult> DeleteResourceTypeAsync(
        int bitResourceTypeId,
        ResourceTypeFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.DeleteResourceTypeAsync(bitResourceTypeId, logger, cancellationToken);
    }
}
