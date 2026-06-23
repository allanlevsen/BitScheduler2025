using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;

namespace BitSchedulerCore.Services;

public sealed class BitClientService(BitScheduleDbContext dbContext) : IBitClientService
{
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
}
