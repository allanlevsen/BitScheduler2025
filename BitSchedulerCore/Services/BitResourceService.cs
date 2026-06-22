using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;

namespace BitSchedulerCore.Services;

public sealed class BitResourceService(BitScheduleDbContext dbContext) : IBitResourceService
{
    public async Task<IReadOnlyList<BitResourceListItem>> ListResourcesAsync(int clientId, CancellationToken cancellationToken = default)
    {
        return await dbContext.BitResources
            .AsNoTracking()
            .Where(resource => resource.BitClientId == clientId)
            .OrderBy(resource => resource.FirstName)
            .ThenBy(resource => resource.LastName)
            .Select(resource => new BitResourceListItem
            {
                BitResourceId = resource.BitResourceId,
                BitResourceTypeId = resource.BitResourceTypeId,
                ResourceTypeName = resource.BitResourceType.Name,
                FirstName = resource.FirstName,
                LastName = resource.LastName,
                EmailAddress = resource.EmailAddress,
                DisplayName = $"{resource.FirstName} {resource.LastName}".Trim()
            })
            .ToListAsync(cancellationToken);
    }
}
