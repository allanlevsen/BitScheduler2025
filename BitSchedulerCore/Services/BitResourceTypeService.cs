using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitSchedulerCore.Services;

public sealed class BitResourceTypeService(
    BitScheduleDbContext dbContext,
    ILogger<BitResourceTypeService> logger) : IBitResourceTypeService
{
    public async Task<BitResourceTypeListItem> CreateResourceTypeAsync(BitResourceTypeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        await EnsureClientExistsAsync(request.BitClientId, cancellationToken);
        var normalizedName = NormalizeRequired(request.Name, nameof(request.Name));
        await EnsureUniqueNameAsync(request.BitClientId, normalizedName, null, cancellationToken);

        var resourceType = new BitResourceType
        {
            BitClientId = request.BitClientId,
            Name = normalizedName
        };

        dbContext.BitResourceTypes.Add(resourceType);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created BitResourceType {BitResourceTypeId}.",
            resourceType.BitResourceTypeId);

        return await GetResourceTypeOrThrowAsync(resourceType.BitClientId, resourceType.BitResourceTypeId, cancellationToken);
    }

    public Task<BitResourceTypeListItem?> GetResourceTypeAsync(int clientId, int bitResourceTypeId, CancellationToken cancellationToken = default)
    {
        return BuildListItemQuery()
            .SingleOrDefaultAsync(
                resourceType => resourceType.BitClientId == clientId && resourceType.BitResourceTypeId == bitResourceTypeId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<BitResourceTypeListItem>> ListResourceTypesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await BuildListItemQuery()
            .Where(resourceType => resourceType.BitClientId == clientId)
            .OrderBy(resourceType => resourceType.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<BitResourceTypeListItem?> UpdateResourceTypeAsync(int clientId, int bitResourceTypeId, BitResourceTypeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var resourceType = await dbContext.BitResourceTypes
            .SingleOrDefaultAsync(
                item => item.BitResourceTypeId == bitResourceTypeId && item.BitClientId == clientId,
                cancellationToken);

        if (resourceType is null)
        {
            return null;
        }

        var normalizedName = NormalizeRequired(request.Name, nameof(request.Name));
        await EnsureClientExistsAsync(request.BitClientId, cancellationToken);
        await EnsureUniqueNameAsync(request.BitClientId, normalizedName, bitResourceTypeId, cancellationToken);

        resourceType.BitClientId = request.BitClientId;
        resourceType.Name = normalizedName;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated BitResourceType {BitResourceTypeId}.",
            bitResourceTypeId);

        return await GetResourceTypeOrThrowAsync(clientId, bitResourceTypeId, cancellationToken);
    }

    public async Task<bool> DeleteResourceTypeAsync(int clientId, int bitResourceTypeId, CancellationToken cancellationToken = default)
    {
        var resourceType = await dbContext.BitResourceTypes
            .SingleOrDefaultAsync(
                item => item.BitResourceTypeId == bitResourceTypeId && item.BitClientId == clientId,
                cancellationToken);

        if (resourceType is null)
        {
            return false;
        }

        dbContext.BitResourceTypes.Remove(resourceType);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Deleted BitResourceType {BitResourceTypeId}.",
            bitResourceTypeId);

        return true;
    }

    private IQueryable<BitResourceTypeListItem> BuildListItemQuery()
    {
        return dbContext.BitResourceTypes
            .AsNoTracking()
            .Select(resourceType => new BitResourceTypeListItem
            {
                BitResourceTypeId = resourceType.BitResourceTypeId,
                BitClientId = resourceType.BitClientId,
                Name = resourceType.Name
            });
    }

    private async Task EnsureUniqueNameAsync(int clientId, string normalizedName, int? excludedBitResourceTypeId, CancellationToken cancellationToken)
    {
        var duplicateExists = await dbContext.BitResourceTypes
            .AnyAsync(
                resourceType => resourceType.BitClientId == clientId &&
                                resourceType.Name.ToLower() == normalizedName.ToLower() &&
                                (!excludedBitResourceTypeId.HasValue || resourceType.BitResourceTypeId != excludedBitResourceTypeId.Value),
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A resource type named '{normalizedName}' already exists.");
        }
    }

    private async Task EnsureClientExistsAsync(int bitClientId, CancellationToken cancellationToken)
    {
        var clientExists = await dbContext.BitClients
            .AnyAsync(client => client.BitClientId == bitClientId, cancellationToken);

        if (!clientExists)
        {
            throw new InvalidOperationException($"BitClient {bitClientId} does not exist.");
        }
    }

    private async Task<BitResourceTypeListItem> GetResourceTypeOrThrowAsync(int clientId, int bitResourceTypeId, CancellationToken cancellationToken)
    {
        return await GetResourceTypeAsync(clientId, bitResourceTypeId, cancellationToken)
            ?? throw new InvalidOperationException($"Resource type {bitResourceTypeId} was not found after it was saved.");
    }

    private static void ValidateRequest(BitResourceTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.BitClientId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.BitClientId), "A valid BitClientId is required.");
        }

        _ = NormalizeRequired(request.Name, nameof(request.Name));
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
