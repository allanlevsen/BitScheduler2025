using BitScheduleServices.Features.Schedule;
using BitScheduleServices.Features.Clients;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.Resources;

public sealed class ResourceFeatureService(
    IBitResourceService resourceService,
    ICurrentBitClientAccessor currentBitClientAccessor)
{
    public async Task<IResult> ListResourcesAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var resources = await resourceService.ListResourcesAsync(bitClientId, cancellationToken);
            return Results.Ok(resources);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing resources for the current BitClient.");
            return Results.Problem("An error occurred while listing resources.", statusCode: 500);
        }
    }

    public async Task<IResult> ListResourceTypesAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var resourceTypes = await resourceService.ListResourceTypesAsync(bitClientId, cancellationToken);
            return Results.Ok(resourceTypes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing resource types for the current BitClient.");
            return Results.Problem("An error occurred while listing resource types.", statusCode: 500);
        }
    }

    public async Task<IResult> GetResourceAsync(int bitResourceId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var resource = await resourceService.GetResourceAsync(bitClientId, bitResourceId, cancellationToken);
            return resource is null ? Results.NotFound() : Results.Ok(resource);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading resource {BitResourceId} for the current BitClient.", bitResourceId);
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
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var resource = await resourceService.CreateResourceAsync(bitClientId, request, cancellationToken);
            return Results.Ok(resource);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid resource request received: {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Resource creation conflict for the current BitClient with request {@Request}", request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating a resource for the current BitClient with request {@Request}", request);
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
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var resource = await resourceService.UpdateResourceAsync(bitClientId, bitResourceId, request, cancellationToken);
            return resource is null ? Results.NotFound() : Results.Ok(resource);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid resource update request for BitResourceId {BitResourceId}: {@Request}", bitResourceId, request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Resource update conflict for BitResourceId {BitResourceId} on the current BitClient, request {@Request}", bitResourceId, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating resource {BitResourceId} for the current BitClient with request {@Request}", bitResourceId, request);
            return Results.Problem("An error occurred while updating the resource.", statusCode: 500);
        }
    }

    public async Task<IResult> DeleteResourceAsync(int bitResourceId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var deleted = await resourceService.DeleteResourceAsync(bitClientId, bitResourceId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting resource {BitResourceId} for the current BitClient.", bitResourceId);
            return Results.Problem("An error occurred while deleting the resource.", statusCode: 500);
        }
    }
}
