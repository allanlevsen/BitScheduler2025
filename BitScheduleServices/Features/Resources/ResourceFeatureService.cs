using BitScheduleServices.Features.Schedule;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.Resources;

public sealed class ResourceFeatureService(
    IBitResourceService resourceService,
    BitScheduleFactory scheduleFactory)
{
    public async Task<IResult> ListResourcesAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resources = await resourceService.ListResourcesAsync(scheduleFactory.DefaultClient, cancellationToken);
            return Results.Ok(resources);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing resources for ClientId {ClientId}", scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while listing resources.", statusCode: 500);
        }
    }

    public async Task<IResult> ListResourceTypesAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resourceTypes = await resourceService.ListResourceTypesAsync(scheduleFactory.DefaultClient, cancellationToken);
            return Results.Ok(resourceTypes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing resource types for ClientId {ClientId}.", scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while listing resource types.", statusCode: 500);
        }
    }

    public async Task<IResult> GetResourceAsync(int bitResourceId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resource = await resourceService.GetResourceAsync(scheduleFactory.DefaultClient, bitResourceId, cancellationToken);
            return resource is null ? Results.NotFound() : Results.Ok(resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading resource {BitResourceId} for ClientId {ClientId}", bitResourceId, scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while reading the resource.", statusCode: 500);
        }
    }

    public async Task<IResult> CreateResourceAsync(BitResourceRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        if (request.BitResourceTypeId <= 0)
        {
            return Results.BadRequest("A valid BitResourceTypeId is required.");
        }

        try
        {
            var resource = await resourceService.CreateResourceAsync(scheduleFactory.DefaultClient, request, cancellationToken);
            return Results.Ok(resource);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid resource request received: {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Resource creation conflict for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating a resource for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Problem("An error occurred while creating the resource.", statusCode: 500);
        }
    }

    public async Task<IResult> UpdateResourceAsync(int bitResourceId, BitResourceRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        if (request.BitResourceTypeId <= 0)
        {
            return Results.BadRequest("A valid BitResourceTypeId is required.");
        }

        try
        {
            var resource = await resourceService.UpdateResourceAsync(scheduleFactory.DefaultClient, bitResourceId, request, cancellationToken);
            return resource is null ? Results.NotFound() : Results.Ok(resource);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid resource update request for BitResourceId {BitResourceId}: {@Request}", bitResourceId, request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Resource update conflict for ClientId {ClientId}, BitResourceId {BitResourceId}, request {@Request}", scheduleFactory.DefaultClient, bitResourceId, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating resource {BitResourceId} for ClientId {ClientId} with request {@Request}", bitResourceId, scheduleFactory.DefaultClient, request);
            return Results.Problem("An error occurred while updating the resource.", statusCode: 500);
        }
    }

    public async Task<IResult> DeleteResourceAsync(int bitResourceId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await resourceService.DeleteResourceAsync(scheduleFactory.DefaultClient, bitResourceId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting resource {BitResourceId} for ClientId {ClientId}", bitResourceId, scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while deleting the resource.", statusCode: 500);
        }
    }
}
