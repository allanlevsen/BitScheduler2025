using BitSchedulerCore;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;

namespace AspireBitSchedule.Tests.HexGrid;

public sealed class HexGridSearchServiceTests
{
    [Fact]
    public void GetNeighborGridIds_ReturnsCachedNeighbors()
    {
        var service = CreateSearchServiceWithSevenCellCluster();

        var neighbors = service.GetNeighborGridIds(100);

        Assert.Equal(6, neighbors.Count);
        foreach (var expectedId in new[] { 93, 80, 85, 87, 89, 91 })
        {
            Assert.Contains(expectedId, neighbors);
        }
    }

    [Fact]
    public void GetGridIdsWithinRing_ReturnsRingZeroAndRingOne()
    {
        var service = CreateSearchServiceWithSevenCellCluster();

        var gridIds = service.GetGridIdsWithinRing(100, maxRingDistance: 1);

        Assert.Contains(100, gridIds);
        foreach (var expectedId in new[] { 93, 80, 85, 87, 89, 91 })
        {
            Assert.Contains(expectedId, gridIds);
        }
    }

    [Fact]
    public void ExpandSearch_CanReturnSingleOuterRing()
    {
        var service = CreateSearchServiceWithSevenCellCluster();

        var gridIds = service.ExpandSearch(100, startRing: 1, endRing: 1);

        Assert.DoesNotContain(100, gridIds);
        Assert.Equal(6, gridIds.Count);
    }

    [Fact]
    public void GetGridId_ReturnsNull_WhenPointIsOutsideGeneratedArea()
    {
        var service = CreateSearchServiceWithGeneratedEdmontonArea();

        var gridId = service.GetGridId(51.0447, -114.0719);

        Assert.Null(gridId);
    }

    [Fact]
    public void GetGridId_ReturnsGridId_WhenPointIsInsideGeneratedArea()
    {
        var service = CreateSearchServiceWithGeneratedEdmontonArea();

        var gridId = service.GetGridId(53.5461, -113.4938);

        Assert.NotNull(gridId);
    }

    [Fact]
    public void GetHexPolygon_ReturnsSixVertices()
    {
        var service = CreateSearchServiceWithGeneratedEdmontonArea();
        var gridId = service.GetGridId(53.5461, -113.4938);

        var polygon = service.GetHexPolygon(gridId!.Value);

        Assert.Equal(6, polygon.Count);
    }

    private static HexGridSearchService CreateSearchServiceWithSevenCellCluster()
    {
        var cells = HexGridTableBuilderTests.CreateSevenCellCluster();
        var lookup = CreateLookup(cells, maxRingDistance: 1);

        return new HexGridSearchService(new StubHexGridLookupProvider(lookup));
    }

    private static HexGridSearchService CreateSearchServiceWithGeneratedEdmontonArea()
    {
        var options = new HexGridGenerationOptions
        {
            AreaName = "TestEdmonton",
            OriginLatitude = 53.5461,
            OriginLongitude = -113.4938,
            HexRadiusMeters = 500,
            MinLatitude = 53.52,
            MaxLatitude = 53.57,
            MinLongitude = -113.54,
            MaxLongitude = -113.45,
            IncludePolygonVertices = false,
            MaxPrecomputedRingDistance = 1
        };

        var cells = HexGridGenerationEngine.GenerateCells(options).ToList();
        for (var index = 0; index < cells.Count; index++)
        {
            cells[index].Id = index + 1;
            cells[index].HexGridVersionId = 1;
        }

        var lookup = CreateLookup(cells, maxRingDistance: 1, options);
        return new HexGridSearchService(new StubHexGridLookupProvider(lookup));
    }

    private static HexGridLookup CreateLookup(
        IReadOnlyCollection<HexGridCell> cells,
        int maxRingDistance,
        HexGridGenerationOptions? options = null)
    {
        options ??= HexGridServiceAreas.EdmontonMetro;
        var builder = new HexGridTableBuilder();
        var neighbors = builder.BuildNeighbors(cells);
        var rings = builder.BuildSearchRings(cells, maxRingDistance);

        return new HexGridLookup
        {
            HexGridVersionId = 1,
            AreaName = options.AreaName,
            OriginLatitude = options.OriginLatitude,
            OriginLongitude = options.OriginLongitude,
            HexRadiusMeters = options.HexRadiusMeters,
            MinLatitude = options.MinLatitude,
            MaxLatitude = options.MaxLatitude,
            MinLongitude = options.MinLongitude,
            MaxLongitude = options.MaxLongitude,
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
    }

    private sealed class StubHexGridLookupProvider : IHexGridLookupProvider
    {
        private readonly HexGridLookup _lookup;

        public StubHexGridLookupProvider(HexGridLookup lookup)
        {
            _lookup = lookup;
            Current = lookup;
        }

        public HexGridLookup Current { get; private set; }

        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            Current = _lookup;
            return Task.CompletedTask;
        }
    }
}
