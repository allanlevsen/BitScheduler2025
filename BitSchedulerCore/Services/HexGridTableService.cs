using BitSchedulerCore.Data.BitTimeScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace BitSchedulerCore.Services;

public sealed class HexGridTableService(
    BitScheduleDbContext dbContext,
    HexGridTableBuilder tableBuilder) : IHexGridTableService
{
    public async Task BuildNeighborTableAsync(
        int hexGridVersionId,
        CancellationToken cancellationToken = default)
    {
        var cells = await LoadActiveCellsAsync(hexGridVersionId, cancellationToken);
        if (cells.Count == 0)
        {
            return;
        }

        var cellIds = cells.Select(cell => cell.Id).ToArray();
        var existingNeighbors = await dbContext.HexGridNeighbors
            .Where(neighbor => cellIds.Contains(neighbor.HexGridCellId))
            .ToListAsync(cancellationToken);

        dbContext.HexGridNeighbors.RemoveRange(existingNeighbors);
        dbContext.HexGridNeighbors.AddRange(tableBuilder.BuildNeighbors(cells));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BuildSearchRingTableAsync(
        int hexGridVersionId,
        int maxRingDistance,
        CancellationToken cancellationToken = default)
    {
        var cells = await LoadActiveCellsAsync(hexGridVersionId, cancellationToken);
        if (cells.Count == 0)
        {
            return;
        }

        var cellIds = cells.Select(cell => cell.Id).ToArray();
        var existingRings = await dbContext.HexGridSearchRings
            .Where(ring => cellIds.Contains(ring.HexGridCellId))
            .ToListAsync(cancellationToken);

        dbContext.HexGridSearchRings.RemoveRange(existingRings);
        dbContext.HexGridSearchRings.AddRange(tableBuilder.BuildSearchRings(cells, maxRingDistance));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<HexGridCell>> LoadActiveCellsAsync(int hexGridVersionId, CancellationToken cancellationToken)
    {
        return await dbContext.HexGridCells
            .Where(cell => cell.HexGridVersionId == hexGridVersionId && cell.IsActive)
            .OrderBy(cell => cell.Id)
            .ToListAsync(cancellationToken);
    }
}
