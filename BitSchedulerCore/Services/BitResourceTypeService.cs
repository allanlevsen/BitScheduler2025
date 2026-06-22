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

        var normalizedName = NormalizeRequired(request.Name, nameof(request.Name));
        await EnsureUniqueNameAsync(normalizedName, null, cancellationToken);

        var resourceType = new BitResourceType
        {
            Name = normalizedName
        };

        dbContext.BitResourceTypes.Add(resourceType);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Created BitResourceType {BitResourceTypeId}.",
            resourceType.BitResourceTypeId);

        return await GetResourceTypeOrThrowAsync(resourceType.BitResourceTypeId, cancellationToken);
    }

    public Task<BitResourceTypeListItem?> GetResourceTypeAsync(int bitResourceTypeId, CancellationToken cancellationToken = default)
    {
        return BuildListItemQuery()
            .SingleOrDefaultAsync(resourceType => resourceType.BitResourceTypeId == bitResourceTypeId, cancellationToken);
    }

    public async Task<IReadOnlyList<BitResourceTypeListItem>> ListResourceTypesAsync(CancellationToken cancellationToken = default)
    {
        return await BuildListItemQuery()
            .OrderBy(resourceType => resourceType.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<BitResourceTypeListItem?> UpdateResourceTypeAsync(int bitResourceTypeId, BitResourceTypeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var resourceType = await dbContext.BitResourceTypes
            .SingleOrDefaultAsync(item => item.BitResourceTypeId == bitResourceTypeId, cancellationToken);

        if (resourceType is null)
        {
            return null;
        }

        var normalizedName = NormalizeRequired(request.Name, nameof(request.Name));
        await EnsureUniqueNameAsync(normalizedName, bitResourceTypeId, cancellationToken);

        resourceType.Name = normalizedName;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated BitResourceType {BitResourceTypeId}.",
            bitResourceTypeId);

        return await GetResourceTypeOrThrowAsync(bitResourceTypeId, cancellationToken);
    }

    public async Task<bool> DeleteResourceTypeAsync(int bitResourceTypeId, CancellationToken cancellationToken = default)
    {
        var resourceType = await dbContext.BitResourceTypes
            .SingleOrDefaultAsync(item => item.BitResourceTypeId == bitResourceTypeId, cancellationToken);

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
                Name = resourceType.Name
            });
    }

    private async Task EnsureUniqueNameAsync(string normalizedName, int? excludedBitResourceTypeId, CancellationToken cancellationToken)
    {
        var duplicateExists = await dbContext.BitResourceTypes
            .AnyAsync(
                resourceType => resourceType.Name.ToLower() == normalizedName.ToLower() &&
                                (!excludedBitResourceTypeId.HasValue || resourceType.BitResourceTypeId != excludedBitResourceTypeId.Value),
                cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A resource type named '{normalizedName}' already exists.");
        }
    }

    private async Task<BitResourceTypeListItem> GetResourceTypeOrThrowAsync(int bitResourceTypeId, CancellationToken cancellationToken)
    {
        return await GetResourceTypeAsync(bitResourceTypeId, cancellationToken)
            ?? throw new InvalidOperationException($"Resource type {bitResourceTypeId} was not found after it was saved.");
    }

    private static void ValidateRequest(BitResourceTypeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
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
