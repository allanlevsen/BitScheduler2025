using BitSchedulerCore.HexGrid;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;

namespace BitScheduleServices.Features.HexGrid;

public sealed class HexGridFeatureService(
    IHexGridGenerationService generationService,
    IHexGridTableService tableService,
    IHexGridLookupProvider lookupProvider,
    IHexGridSearchService searchService)
{
    public async Task<IResult> GenerateEdmontonMetroHexGridAsync(CancellationToken cancellationToken)
    {
        var options = HexGridServiceAreas.EdmontonMetro;
        var result = await generationService.GenerateGridAsync(options, cancellationToken);

        await tableService.BuildNeighborTableAsync(result.HexGridVersionId, cancellationToken);
        await tableService.BuildSearchRingTableAsync(
            result.HexGridVersionId,
            options.MaxPrecomputedRingDistance,
            cancellationToken);
        await lookupProvider.ReloadAsync(cancellationToken);

        return Results.Ok(new
        {
            result.HexGridVersionId,
            result.AreaName,
            result.Name,
            result.CellCount,
            result.VertexCount,
            options.MaxPrecomputedRingDistance
        });
    }

    public async Task<IResult> BuildHexGridTablesAsync(
        int versionId,
        int? maxRingDistance,
        CancellationToken cancellationToken)
    {
        var ringDistance = maxRingDistance ?? HexGridServiceAreas.EdmontonMetro.MaxPrecomputedRingDistance;

        await tableService.BuildNeighborTableAsync(versionId, cancellationToken);
        await tableService.BuildSearchRingTableAsync(versionId, ringDistance, cancellationToken);
        await lookupProvider.ReloadAsync(cancellationToken);

        return Results.Ok(new
        {
            HexGridVersionId = versionId,
            MaxRingDistance = ringDistance
        });
    }

    public async Task<IResult> ReloadHexGridLookupAsync(CancellationToken cancellationToken)
    {
        await lookupProvider.ReloadAsync(cancellationToken);
        return Results.Ok(new
        {
            lookupProvider.Current.HexGridVersionId,
            lookupProvider.Current.AreaName,
            CellCount = lookupProvider.Current.CellsById.Count
        });
    }

    public IResult GetHexGridCell(double latitude, double longitude)
    {
        var gridId = searchService.GetGridId(latitude, longitude);
        if (!gridId.HasValue)
        {
            return Results.NotFound();
        }

        return Results.Ok(searchService.GetCell(gridId.Value));
    }

    public IResult GetHexGridNeighbors(int gridId)
    {
        return Results.Ok(searchService.GetNeighborGridIds(gridId));
    }

    public IResult GetHexGridRing(int gridId, int maxRingDistance)
    {
        return Results.Ok(searchService.GetGridIdsWithinRing(gridId, maxRingDistance));
    }
}
