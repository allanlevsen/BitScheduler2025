using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;

namespace BitSchedulerCore.Services;

public sealed class HexGridGenerationService(BitScheduleDbContext dbContext) : IHexGridGenerationService
{
    public async Task<HexGridGenerationResult> GenerateGridAsync(
        HexGridGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        var cells = HexGridGenerationEngine.GenerateCells(options).ToList();
        var createdUtc = DateTime.UtcNow;
        var version = new HexGridVersion
        {
            AreaName = options.AreaName,
            Name = $"{options.AreaName}-{createdUtc:yyyyMMddHHmmss}",
            OriginLatitude = options.OriginLatitude,
            OriginLongitude = options.OriginLongitude,
            HexRadiusMeters = options.HexRadiusMeters,
            MinLatitude = options.MinLatitude,
            MaxLatitude = options.MaxLatitude,
            MinLongitude = options.MinLongitude,
            MaxLongitude = options.MaxLongitude,
            MaxPrecomputedRingDistance = options.MaxPrecomputedRingDistance,
            IsActive = true,
            CreatedUtc = createdUtc
        };

        foreach (var cell in cells)
        {
            cell.HexGridVersion = version;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var activeVersions = await dbContext.HexGridVersions
            .Where(existing => existing.AreaName == options.AreaName && existing.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var activeVersion in activeVersions)
        {
            activeVersion.IsActive = false;
        }

        dbContext.HexGridVersions.Add(version);
        dbContext.HexGridCells.AddRange(cells);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new HexGridGenerationResult
        {
            HexGridVersionId = version.Id,
            AreaName = version.AreaName,
            Name = version.Name,
            CellCount = cells.Count,
            VertexCount = cells.Sum(cell => cell.Vertices.Count),
            Cells = cells
        };
    }
}
