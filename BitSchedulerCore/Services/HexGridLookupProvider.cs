using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BitSchedulerCore.Services;

public sealed class HexGridLookupProvider(IServiceScopeFactory scopeFactory) : IHexGridLookupProvider
{
    private HexGridLookup _current = HexGridLookup.Empty;

    public HexGridLookup Current => Volatile.Read(ref _current);

    public async Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BitScheduleDbContext>();

        var activeVersion = await dbContext.HexGridVersions
            .AsNoTracking()
            .Where(version => version.IsActive)
            .OrderByDescending(version => version.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeVersion is null)
        {
            Volatile.Write(ref _current, HexGridLookup.Empty);
            return;
        }

        var cells = await dbContext.HexGridCells
            .AsNoTracking()
            .Where(cell => cell.HexGridVersionId == activeVersion.Id && cell.IsActive)
            .OrderBy(cell => cell.Id)
            .ToListAsync(cancellationToken);

        if (cells.Count == 0)
        {
            Volatile.Write(ref _current, HexGridLookup.Empty);
            return;
        }

        var cellIds = cells.Select(cell => cell.Id).ToArray();

        var neighbors = await dbContext.HexGridNeighbors
            .AsNoTracking()
            .Where(neighbor => cellIds.Contains(neighbor.HexGridCellId))
            .OrderBy(neighbor => neighbor.HexGridCellId)
            .ThenBy(neighbor => neighbor.Direction)
            .ToListAsync(cancellationToken);

        var rings = await dbContext.HexGridSearchRings
            .AsNoTracking()
            .Where(ring => cellIds.Contains(ring.HexGridCellId))
            .OrderBy(ring => ring.HexGridCellId)
            .ThenBy(ring => ring.RingDistance)
            .ThenBy(ring => ring.NearbyHexGridCellId)
            .ToListAsync(cancellationToken);

        var lookup = new HexGridLookup
        {
            HexGridVersionId = activeVersion.Id,
            AreaName = activeVersion.AreaName,
            OriginLatitude = activeVersion.OriginLatitude,
            OriginLongitude = activeVersion.OriginLongitude,
            HexRadiusMeters = activeVersion.HexRadiusMeters,
            MinLatitude = activeVersion.MinLatitude,
            MaxLatitude = activeVersion.MaxLatitude,
            MinLongitude = activeVersion.MinLongitude,
            MaxLongitude = activeVersion.MaxLongitude,
            CellsById = cells.ToDictionary(
                cell => cell.Id,
                cell => new HexCellDto
                {
                    Id = cell.Id,
                    Q = cell.Q,
                    R = cell.R,
                    CenterLatitude = cell.CenterLatitude,
                    CenterLongitude = cell.CenterLongitude,
                    HexRadiusMeters = cell.HexRadiusMeters
                }),
            CellIdsByCoordinate = cells.ToDictionary(cell => (cell.Q, cell.R), cell => cell.Id),
            NeighborIdsByCellId = neighbors
                .GroupBy(neighbor => neighbor.HexGridCellId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(neighbor => neighbor.NeighborHexGridCellId).ToArray()),
            RingIdsByCellId = rings
                .GroupBy(ring => ring.HexGridCellId)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyDictionary<int, int[]>)group
                        .GroupBy(ring => ring.RingDistance)
                        .ToDictionary(
                            ringGroup => ringGroup.Key,
                            ringGroup => ringGroup.Select(ring => ring.NearbyHexGridCellId).ToArray()))
        };

        Volatile.Write(ref _current, lookup);
    }
}
