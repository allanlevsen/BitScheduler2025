using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.Clients;

public sealed class ClientFeatureService(
    IBitClientService bitClientService,
    ICurrentBitClientAccessor currentBitClientAccessor)
{
    public async Task<IResult> GetClientAsync(int bitClientId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var client = await bitClientService.GetClientAsync(bitClientId, cancellationToken);
            return client is null ? Results.NotFound() : Results.Ok(client);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading BitClient {BitClientId}", bitClientId);
            return Results.Problem("An error occurred while reading the client.", statusCode: 500);
        }
    }

    public async Task<IResult> ListClientsAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var clients = await bitClientService.ListClientsAsync(cancellationToken);
            return Results.Ok(clients);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing BitClients.");
            return Results.Problem("An error occurred while listing clients.", statusCode: 500);
        }
    }

    public async Task<IResult> CreateClientAsync(BitClientRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var client = await bitClientService.CreateClientAsync(request, cancellationToken);
            return Results.Ok(client);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid BitClient create request {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "BitClient creation conflict for request {@Request}", request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while creating a BitClient with request {@Request}", request);
            return Results.Problem("An error occurred while creating the client.", statusCode: 500);
        }
    }

    public async Task<IResult> GetCurrentClientAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var client = await currentBitClientAccessor.GetCurrentClientAsync(cancellationToken);
            return Results.Ok(client);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "No current BitClient could be resolved.");
            return Results.Problem(ex.Message, statusCode: 404);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while resolving the current BitClient.");
            return Results.Problem("An error occurred while loading the current client.", statusCode: 500);
        }
    }

    public async Task<IResult> SetCurrentClientAsync(
        BitClientSelectionRequest request,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (request.BitClientId <= 0)
        {
            return Results.BadRequest("A valid BitClientId is required.");
        }

        try
        {
            var client = await currentBitClientAccessor.SetCurrentClientAsync(request.BitClientId, cancellationToken);
            return Results.Ok(client);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            logger.LogWarning(ex, "Invalid BitClient selection request {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "BitClient selection failed for request {@Request}", request);
            return Results.NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while setting the current BitClient with request {@Request}", request);
            return Results.Problem("An error occurred while saving the current client.", statusCode: 500);
        }
    }

    public async Task<IResult> UpdateClientAsync(int bitClientId, BitClientRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var client = await bitClientService.UpdateClientAsync(bitClientId, request, cancellationToken);
            return client is null ? Results.NotFound() : Results.Ok(client);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid BitClient update request for BitClientId {BitClientId}: {@Request}", bitClientId, request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "BitClient update conflict for BitClientId {BitClientId}: {@Request}", bitClientId, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while updating BitClient {BitClientId} with request {@Request}", bitClientId, request);
            return Results.Problem("An error occurred while updating the client.", statusCode: 500);
        }
    }

    public async Task<IResult> DeleteClientAsync(int bitClientId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await bitClientService.DeleteClientAsync(bitClientId, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "BitClient delete blocked for BitClientId {BitClientId}", bitClientId);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting BitClient {BitClientId}", bitClientId);
            return Results.Problem("An error occurred while deleting the client.", statusCode: 500);
        }
    }
}
