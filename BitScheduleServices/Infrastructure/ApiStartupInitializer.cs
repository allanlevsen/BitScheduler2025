using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Infrastructure;

public sealed class ApiStartupInitializer(
    SeedingService seedingService,
    BitScheduleDbContext dbContext,
    IHexGridLookupProvider hexGridLookupProvider,
    IHexGridGenerationService hexGridGenerationService,
    IHexGridTableService hexGridTableService,
    IHostEnvironment hostEnvironment,
    ILogger<ApiStartupInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting database seeding on startup...");

        await EnsureDatabaseAsync(cancellationToken);

        logger.LogInformation("Seeding initial ResourceTypes and Clients...");
        await seedingService.SeedAsync();

        logger.LogInformation("Seeding initial Schedule Data (BitDays)...");
        await seedingService.SeedScheduleDataAsync();

        logger.LogInformation("Database seeding completed successfully.");
        await EnsureHexGridAsync(cancellationToken);

        logger.LogInformation("Loading active hex grid lookup...");
        await hexGridLookupProvider.ReloadAsync(cancellationToken);
        logger.LogInformation("Hex grid lookup loading completed.");
    }

    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Applying migrations completed.");
        }
        catch (InvalidOperationException ex) when (
            hostEnvironment.IsDevelopment() &&
            ex.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
        {
            logger.LogWarning(
                ex,
                "Migrations were blocked by pending model changes. Recreating the development database from the current EF model.");

            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            logger.LogInformation("Database created from the current EF model.");
        }
    }

    private async Task EnsureHexGridAsync(CancellationToken cancellationToken)
    {
        var activeVersion = await dbContext.HexGridVersions
            .AsNoTracking()
            .Where(version => version.IsActive)
            .OrderByDescending(version => version.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeVersion is null)
        {
            logger.LogInformation("No active hex grid version was found. Generating the Edmonton metro hex grid.");

            var generatedGrid = await hexGridGenerationService.GenerateGridAsync(
                HexGridServiceAreas.EdmontonMetro,
                cancellationToken);

            await hexGridTableService.BuildNeighborTableAsync(generatedGrid.HexGridVersionId, cancellationToken);
            await hexGridTableService.BuildSearchRingTableAsync(
                generatedGrid.HexGridVersionId,
                HexGridServiceAreas.EdmontonMetro.MaxPrecomputedRingDistance,
                cancellationToken);

            logger.LogInformation(
                "Generated hex grid version {HexGridVersionId} with {CellCount} cells for {AreaName}.",
                generatedGrid.HexGridVersionId,
                generatedGrid.CellCount,
                generatedGrid.AreaName);

            return;
        }

        var activeCellIds = await dbContext.HexGridCells
            .AsNoTracking()
            .Where(cell => cell.HexGridVersionId == activeVersion.Id && cell.IsActive)
            .Select(cell => cell.Id)
            .ToListAsync(cancellationToken);

        if (activeCellIds.Count == 0)
        {
            logger.LogWarning(
                "Active hex grid version {HexGridVersionId} contains no active cells. Regenerating the Edmonton metro hex grid.",
                activeVersion.Id);

            var generatedGrid = await hexGridGenerationService.GenerateGridAsync(
                HexGridServiceAreas.EdmontonMetro,
                cancellationToken);

            await hexGridTableService.BuildNeighborTableAsync(generatedGrid.HexGridVersionId, cancellationToken);
            await hexGridTableService.BuildSearchRingTableAsync(
                generatedGrid.HexGridVersionId,
                HexGridServiceAreas.EdmontonMetro.MaxPrecomputedRingDistance,
                cancellationToken);

            logger.LogInformation(
                "Regenerated hex grid version {HexGridVersionId} with {CellCount} cells for {AreaName}.",
                generatedGrid.HexGridVersionId,
                generatedGrid.CellCount,
                generatedGrid.AreaName);

            return;
        }

        var hasNeighbors = await dbContext.HexGridNeighbors
            .AsNoTracking()
            .AnyAsync(neighbor => activeCellIds.Contains(neighbor.HexGridCellId), cancellationToken);

        if (!hasNeighbors)
        {
            logger.LogInformation(
                "No neighbor rows were found for active hex grid version {HexGridVersionId}. Building neighbor table.",
                activeVersion.Id);

            await hexGridTableService.BuildNeighborTableAsync(activeVersion.Id, cancellationToken);
        }

        var hasSearchRings = await dbContext.HexGridSearchRings
            .AsNoTracking()
            .AnyAsync(ring => activeCellIds.Contains(ring.HexGridCellId), cancellationToken);

        if (!hasSearchRings)
        {
            logger.LogInformation(
                "No search ring rows were found for active hex grid version {HexGridVersionId}. Building search ring table.",
                activeVersion.Id);

            await hexGridTableService.BuildSearchRingTableAsync(
                activeVersion.Id,
                activeVersion.MaxPrecomputedRingDistance,
                cancellationToken);
        }
    }
}
