using BitScheduleServices.Features.Events;
using BitSchedulerCore.Models;

namespace AspireBitSchedule.ApiService.Features.Events;

internal static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEventEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/events");
        group.MapGet("/", ListAllEventsAsync);
        group.MapPost("/list", ListEventsAsync);
        group.MapPost("/", CreateEventAsync);
        group.MapPut("/{bitEventId:int}", UpdateEventAsync);
        group.MapGet("/{bitEventId:int}", GetEventAsync);
        group.MapDelete("/{bitEventId:int}", DeleteEventAsync);

        endpoints.MapGet("/Events", ListAllEventsAsync);
        endpoints.MapPost("/Events/List", ListEventsAsync);
        endpoints.MapPost("/Events", CreateEventAsync);
        endpoints.MapPut("/Events/{bitEventId:int}", UpdateEventAsync);
        endpoints.MapGet("/Events/{bitEventId:int}", GetEventAsync);
        endpoints.MapDelete("/Events/{bitEventId:int}", DeleteEventAsync);

        return endpoints;
    }

    private static Task<IResult> ListAllEventsAsync(
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListEventsAsync(null, logger, cancellationToken);
    }

    private static Task<IResult> ListEventsAsync(
        BitEventListRequest request,
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListEventsAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> CreateEventAsync(
        BitEventRequest request,
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.CreateEventAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> UpdateEventAsync(
        int bitEventId,
        BitEventRequest request,
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.UpdateEventAsync(bitEventId, request, logger, cancellationToken);
    }

    private static Task<IResult> GetEventAsync(
        int bitEventId,
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.GetEventAsync(bitEventId, logger, cancellationToken);
    }

    private static Task<IResult> DeleteEventAsync(
        int bitEventId,
        EventFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.DeleteEventAsync(bitEventId, logger, cancellationToken);
    }
}
