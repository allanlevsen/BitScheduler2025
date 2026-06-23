using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitSchedulerCore.Services;

public sealed class BitResourceService(
    BitScheduleDbContext dbContext,
    ILogger<BitResourceService> logger) : IBitResourceService
{
    public async Task<BitResourceListItem> CreateResourceAsync(int clientId, BitResourceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureResourceTypeExistsAsync(clientId, request.BitResourceTypeId, cancellationToken);

        var resource = new BitResource
        {
            BitClientId = clientId,
            BitResourceTypeId = request.BitResourceTypeId,
            FirstName = NormalizeRequired(request.FirstName, nameof(request.FirstName)),
            LastName = NormalizeRequired(request.LastName, nameof(request.LastName)),
            EmailAddress = NormalizeRequired(request.EmailAddress, nameof(request.EmailAddress))
        };

        dbContext.BitResources.Add(resource);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created BitResource {BitResourceId} for ClientId {ClientId}.",
            resource.BitResourceId,
            clientId);

        return await GetResourceOrThrowAsync(clientId, resource.BitResourceId, cancellationToken);
    }

    public Task<BitResourceListItem?> GetResourceAsync(int clientId, int bitResourceId, CancellationToken cancellationToken = default)
    {
        return BuildListItemQuery(clientId)
            .SingleOrDefaultAsync(resource => resource.BitResourceId == bitResourceId, cancellationToken);
    }

    public async Task<IReadOnlyList<BitResourceListItem>> ListResourcesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await BuildListItemQuery(clientId)
            .OrderBy(resource => resource.FirstName)
            .ThenBy(resource => resource.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BitResourceTypeListItem>> ListResourceTypesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.BitResourceTypes
            .AsNoTracking()
            .Where(resourceType => resourceType.BitClientId == clientId)
            .OrderBy(resourceType => resourceType.Name)
            .Select(resourceType => new BitResourceTypeListItem
            {
                BitResourceTypeId = resourceType.BitResourceTypeId,
                BitClientId = resourceType.BitClientId,
                Name = resourceType.Name
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BitResourceListItem?> UpdateResourceAsync(int clientId, int bitResourceId, BitResourceRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureResourceTypeExistsAsync(clientId, request.BitResourceTypeId, cancellationToken);

        var resource = await dbContext.BitResources
            .SingleOrDefaultAsync(
                item => item.BitResourceId == bitResourceId && item.BitClientId == clientId,
                cancellationToken);

        if (resource is null)
        {
            return null;
        }

        resource.BitResourceTypeId = request.BitResourceTypeId;
        resource.FirstName = NormalizeRequired(request.FirstName, nameof(request.FirstName));
        resource.LastName = NormalizeRequired(request.LastName, nameof(request.LastName));
        resource.EmailAddress = NormalizeRequired(request.EmailAddress, nameof(request.EmailAddress));

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated BitResource {BitResourceId} for ClientId {ClientId}.",
            bitResourceId,
            clientId);

        return await GetResourceOrThrowAsync(clientId, bitResourceId, cancellationToken);
    }

    public async Task<bool> DeleteResourceAsync(int clientId, int bitResourceId, CancellationToken cancellationToken = default)
    {
        var resource = await dbContext.BitResources
            .SingleOrDefaultAsync(
                item => item.BitResourceId == bitResourceId && item.BitClientId == clientId,
                cancellationToken);

        if (resource is null)
        {
            return false;
        }

        dbContext.BitResources.Remove(resource);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted BitResource {BitResourceId} for ClientId {ClientId}.",
            bitResourceId,
            clientId);

        return true;
    }

    private IQueryable<BitResourceListItem> BuildListItemQuery(int clientId)
    {
        return dbContext.BitResources
            .AsNoTracking()
            .Where(resource => resource.BitClientId == clientId)
            .Select(resource => new BitResourceListItem
            {
                BitResourceId = resource.BitResourceId,
                BitResourceTypeId = resource.BitResourceTypeId,
                ResourceTypeName = resource.BitResourceType.Name,
                FirstName = resource.FirstName,
                LastName = resource.LastName,
                EmailAddress = resource.EmailAddress,
                DisplayName = $"{resource.FirstName} {resource.LastName}".Trim()
            });
    }

    private async Task EnsureResourceTypeExistsAsync(int clientId, int bitResourceTypeId, CancellationToken cancellationToken)
    {
        var resourceTypeExists = await dbContext.BitResourceTypes
            .AnyAsync(
                resourceType => resourceType.BitResourceTypeId == bitResourceTypeId &&
                                resourceType.BitClientId == clientId,
                cancellationToken);

        if (!resourceTypeExists)
        {
            throw new InvalidOperationException($"Resource type {bitResourceTypeId} does not exist for client {clientId}.");
        }
    }

    private async Task<BitResourceListItem> GetResourceOrThrowAsync(int clientId, int bitResourceId, CancellationToken cancellationToken)
    {
        return await GetResourceAsync(clientId, bitResourceId, cancellationToken)
            ?? throw new InvalidOperationException($"Resource {bitResourceId} was not found after it was saved.");
    }

    private static void ValidateRequest(BitResourceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.BitResourceTypeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.BitResourceTypeId), "A valid resource type id is required.");
        }

        _ = NormalizeRequired(request.FirstName, nameof(request.FirstName));
        _ = NormalizeRequired(request.LastName, nameof(request.LastName));
        _ = NormalizeRequired(request.EmailAddress, nameof(request.EmailAddress));
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} is required.", fieldName);
        }

        return value.Trim();
    }
}
