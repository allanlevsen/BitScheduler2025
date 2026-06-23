using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitSchedulerCore.Services;

public sealed class BitClientService(
    BitScheduleDbContext dbContext,
    ILogger<BitClientService> logger) : IBitClientService
{
    public async Task<BitClientListItem> CreateClientAsync(BitClientRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var normalizedName = NormalizeRequired(request.Name, nameof(request.Name));
        await EnsureUniqueNameAsync(normalizedName, null, cancellationToken);

        var client = new BitClient
        {
            Name = normalizedName
        };

        dbContext.BitClients.Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created BitClient {BitClientId}.", client.BitClientId);

        return await GetClientOrThrowAsync(client.BitClientId, cancellationToken);
    }

    public Task<BitClientListItem?> GetClientAsync(int bitClientId, CancellationToken cancellationToken = default)
    {
        return BuildClientQuery()
            .SingleOrDefaultAsync(client => client.BitClientId == bitClientId, cancellationToken);
    }

    public Task<BitClientListItem?> GetFirstClientAsync(CancellationToken cancellationToken = default)
    {
        return BuildClientQuery()
            .OrderBy(client => client.BitClientId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BitClientListItem>> ListClientsAsync(CancellationToken cancellationToken = default)
    {
        return await BuildClientQuery()
            .OrderBy(client => client.Name)
            .ThenBy(client => client.BitClientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<BitClientListItem?> UpdateClientAsync(int bitClientId, BitClientRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var client = await dbContext.BitClients
            .SingleOrDefaultAsync(item => item.BitClientId == bitClientId, cancellationToken);

        if (client is null)
        {
            return null;
        }

        var normalizedName = NormalizeRequired(request.Name, nameof(request.Name));
        await EnsureUniqueNameAsync(normalizedName, bitClientId, cancellationToken);

        client.Name = normalizedName;
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated BitClient {BitClientId}.", bitClientId);

        return await GetClientOrThrowAsync(bitClientId, cancellationToken);
    }

    public async Task<bool> DeleteClientAsync(int bitClientId, CancellationToken cancellationToken = default)
    {
        var client = await dbContext.BitClients
            .SingleOrDefaultAsync(item => item.BitClientId == bitClientId, cancellationToken);

        if (client is null)
        {
            return false;
        }

        var clientCount = await dbContext.BitClients.CountAsync(cancellationToken);
        if (clientCount <= 1)
        {
            throw new InvalidOperationException("At least one client must remain in the system.");
        }

        if (await ClientHasRelatedDataAsync(bitClientId, cancellationToken))
        {
            throw new InvalidOperationException("This client cannot be deleted because it still has related schedule or administration data.");
        }

        dbContext.BitClients.Remove(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted BitClient {BitClientId}.", bitClientId);

        return true;
    }

    private IQueryable<BitClientListItem> BuildClientQuery()
    {
        return dbContext.BitClients
            .AsNoTracking()
            .Select(client => new BitClientListItem
            {
                BitClientId = client.BitClientId,
                Name = client.Name
            });
    }

    private async Task<bool> ClientHasRelatedDataAsync(int bitClientId, CancellationToken cancellationToken)
    {
        return await dbContext.BitResourceTypes.AnyAsync(item => item.BitClientId == bitClientId, cancellationToken) ||
               await dbContext.BitResources.AnyAsync(item => item.BitClientId == bitClientId, cancellationToken) ||
               await dbContext.BitEvents.AnyAsync(item => item.BitClientId == bitClientId, cancellationToken) ||
               await dbContext.BitResourceScheduleRanges.AnyAsync(item => item.BitClientId == bitClientId, cancellationToken) ||
               await dbContext.BitDays.AnyAsync(item => item.ClientId == bitClientId, cancellationToken);
    }

    private async Task EnsureUniqueNameAsync(string normalizedName, int? excludedBitClientId, CancellationToken cancellationToken)
    {
        var duplicateExists = await dbContext.BitClients.AnyAsync(
            client => client.Name.ToLower() == normalizedName.ToLower() &&
                      (!excludedBitClientId.HasValue || client.BitClientId != excludedBitClientId.Value),
            cancellationToken);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"A client named '{normalizedName}' already exists.");
        }
    }

    private async Task<BitClientListItem> GetClientOrThrowAsync(int bitClientId, CancellationToken cancellationToken)
    {
        return await GetClientAsync(bitClientId, cancellationToken)
            ?? throw new InvalidOperationException($"BitClient {bitClientId} was not found after it was saved.");
    }

    private static void ValidateRequest(BitClientRequest request)
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
