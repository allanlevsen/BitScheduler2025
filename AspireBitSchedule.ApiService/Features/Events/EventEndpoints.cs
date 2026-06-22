using BitScheduleServices.Features.Events;
using BitSchedulerCore.Models;

namespace AspireBitSchedule.ApiService.Features.Events;

internal static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/events");
        group.MapPost("/", CreateEventAsync);
        group.MapGet("/{bitEventId:int}", GetEventAsync);

        endpoints.MapPost("/Events", CreateEventAsync);
        endpoints.MapGet("/Events/{bitEventId:int}", GetEventAsync);

        return endpoints;
    }

    private static Task<IResult> CreateEventAsync(
        BitEventRequest request,
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.CreateEventAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> GetEventAsync(
        int bitEventId,
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.GetEventAsync(bitEventId, logger, cancellationToken);
    }
}
