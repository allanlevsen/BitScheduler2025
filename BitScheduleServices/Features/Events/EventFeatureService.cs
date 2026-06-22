using BitScheduleServices.Features.Schedule;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.Events;

public sealed class EventFeatureService(
    IBitEventService eventService,
    BitScheduleFactory scheduleFactory)
{
    public async Task<IResult> CreateEventAsync(
        BitEventRequest request,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (request.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        try
        {
            var bitEvent = await eventService.CreateEventAsync(
                scheduleFactory.DefaultClient,
                request,
                cancellationToken);

            return Results.Ok(bitEvent);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid event request received: {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Event creation conflict for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating an event for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Problem("An error occurred while creating the event.", statusCode: 500);
        }
    }

    public async Task<IResult> GetEventAsync(
        int bitEventId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var bitEvent = await eventService.GetEventAsync(scheduleFactory.DefaultClient, bitEventId, cancellationToken);
            return bitEvent is null ? Results.NotFound() : Results.Ok(bitEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading event {BitEventId} for ClientId {ClientId}", bitEventId, scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while reading the event.", statusCode: 500);
        }
    }
}
