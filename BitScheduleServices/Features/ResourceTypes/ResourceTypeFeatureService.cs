using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.ResourceTypes;

public sealed class ResourceTypeFeatureService(IBitResourceTypeService resourceTypeService)
{
    public async Task<IResult> ListResourceTypesAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resourceTypes = await resourceTypeService.ListResourceTypesAsync(cancellationToken);
            return Results.Ok(resourceTypes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing resource types.");
            return Results.Problem("An error occurred while listing resource types.", statusCode: 500);
        }
    }

    public async Task<IResult> GetResourceTypeAsync(int bitResourceTypeId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resourceType = await resourceTypeService.GetResourceTypeAsync(bitResourceTypeId, cancellationToken);
            return resourceType is null ? Results.NotFound() : Results.Ok(resourceType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading resource type {BitResourceTypeId}", bitResourceTypeId);
            return Results.Problem("An error occurred while reading the resource type.", statusCode: 500);
        }
    }

    public async Task<IResult> CreateResourceTypeAsync(BitResourceTypeRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resourceType = await resourceTypeService.CreateResourceTypeAsync(request, cancellationToken);
            return Results.Ok(resourceType);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid resource type request received: {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Resource type creation conflict with request {@Request}", request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating a resource type with request {@Request}", request);
            return Results.Problem("An error occurred while creating the resource type.", statusCode: 500);
        }
    }

    public async Task<IResult> UpdateResourceTypeAsync(int bitResourceTypeId, BitResourceTypeRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resourceType = await resourceTypeService.UpdateResourceTypeAsync(bitResourceTypeId, request, cancellationToken);
            return resourceType is null ? Results.NotFound() : Results.Ok(resourceType);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid resource type update request for BitResourceTypeId {BitResourceTypeId}: {@Request}", bitResourceTypeId, request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Resource type update conflict for BitResourceTypeId {BitResourceTypeId}, request {@Request}", bitResourceTypeId, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating resource type {BitResourceTypeId} with request {@Request}", bitResourceTypeId, request);
            return Results.Problem("An error occurred while updating the resource type.", statusCode: 500);
        }
    }

    public async Task<IResult> DeleteResourceTypeAsync(int bitResourceTypeId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await resourceTypeService.DeleteResourceTypeAsync(bitResourceTypeId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Resource type delete blocked for BitResourceTypeId {BitResourceTypeId}", bitResourceTypeId);
            return Results.Conflict("This resource type cannot be deleted because it is in use by one or more resources.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting resource type {BitResourceTypeId}", bitResourceTypeId);
            return Results.Problem("An error occurred while deleting the resource type.", statusCode: 500);
        }
    }
}
