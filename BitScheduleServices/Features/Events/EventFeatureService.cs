using BitScheduleServices.Features.Clients;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.Events;

public sealed class EventFeatureService(
    IBitEventService eventService,
    ICurrentBitClientAccessor currentBitClientAccessor)
{
    public async Task<IResult> ListEventsAsync(
        BitEventListRequest? request,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var bitEvents = await eventService.ListEventsAsync(bitClientId, request, cancellationToken);
            return Results.Ok(bitEvents);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid event list request received: {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing events for the current BitClient with request {@Request}", request);
            return Results.Problem("An error occurred while listing events.", statusCode: 500);
        }
    }

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
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var bitEvent = await eventService.CreateEventAsync(
                bitClientId,
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
            logger.LogWarning(ex, "Event creation conflict for the current BitClient with request {@Request}", request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating an event for the current BitClient with request {@Request}", request);
            return Results.Problem("An error occurred while creating the event.", statusCode: 500);
        }
    }

    public async Task<IResult> UpdateEventAsync(
        int bitEventId,
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
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var bitEvent = await eventService.UpdateEventAsync(
                bitClientId,
                bitEventId,
                request,
                cancellationToken);

            return bitEvent is null ? Results.NotFound() : Results.Ok(bitEvent);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid event update request for BitEventId {BitEventId}: {@Request}", bitEventId, request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Event update conflict for BitEventId {BitEventId} on the current BitClient, request {@Request}", bitEventId, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating event {BitEventId} for the current BitClient with request {@Request}", bitEventId, request);
            return Results.Problem("An error occurred while updating the event.", statusCode: 500);
        }
    }

    public async Task<IResult> GetEventAsync(
        int bitEventId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var bitEvent = await eventService.GetEventAsync(bitClientId, bitEventId, cancellationToken);
            return bitEvent is null ? Results.NotFound() : Results.Ok(bitEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading event {BitEventId} for the current BitClient.", bitEventId);
            return Results.Problem("An error occurred while reading the event.", statusCode: 500);
        }
    }

    public async Task<IResult> DeleteEventAsync(
        int bitEventId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var deleted = await eventService.DeleteEventAsync(bitClientId, bitEventId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting event {BitEventId} for the current BitClient.", bitEventId);
            return Results.Problem("An error occurred while deleting the event.", statusCode: 500);
        }
    }
}
