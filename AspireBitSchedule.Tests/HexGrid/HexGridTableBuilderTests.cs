using BitSchedulerCore.HexGrid;
using BitSchedulerCore.Services;

namespace AspireBitSchedule.Tests.HexGrid;

public sealed class HexGridTableBuilderTests
{
    [Fact]
    public void BuildNeighbors_CenterCell_HasSixNeighbors()
    {
        var cells = CreateSevenCellCluster();
        var builder = new HexGridTableBuilder();

        var neighbors = builder.BuildNeighbors(cells)
            .Where(neighbor => neighbor.HexGridCellId == 100)
            .Select(neighbor => neighbor.NeighborHexGridCellId)
            .ToArray();

        Assert.Equal(6, neighbors.Length);
        foreach (var expectedId in new[] { 93, 80, 85, 87, 89, 91 })
        {
            Assert.Contains(expectedId, neighbors);
        }
    }

    [Fact]
    public void BuildNeighbors_EdgeCell_CanHaveFewerThanSixNeighbors()
    {
        var cells = CreateSevenCellCluster();
        var builder = new HexGridTableBuilder();

        var neighbors = builder.BuildNeighbors(cells)
            .Where(neighbor => neighbor.HexGridCellId == 93)
            .ToArray();

        Assert.InRange(neighbors.Length, 1, 5);
    }

    [Fact]
    public void BuildSearchRings_IncludesCenterAsRingZero()
    {
        var cells = CreateSevenCellCluster();
        var builder = new HexGridTableBuilder();

        var rings = builder.BuildSearchRings(cells, maxRingDistance: 1);

        Assert.Contains(rings, ring =>
            ring.HexGridCellId == 100 &&
            ring.NearbyHexGridCellId == 100 &&
            ring.RingDistance == 0);
    }

    [Fact]
    public void BuildSearchRings_RingOne_HasSixCellsForInteriorCell()
    {
        var cells = CreateSevenCellCluster();
        var builder = new HexGridTableBuilder();

        var rings = builder.BuildSearchRings(cells, maxRingDistance: 1)
            .Where(ring => ring.HexGridCellId == 100 && ring.RingDistance == 1)
            .ToArray();

        Assert.Equal(6, rings.Length);
    }

    internal static IReadOnlyList<HexGridCell> CreateSevenCellCluster()
    {
        return
        [
            new HexGridCell { Id = 100, HexGridVersionId = 1, Q = 0, R = 0, CenterLatitude = 53.5461, CenterLongitude = -113.4938, HexRadiusMeters = 500, IsActive = true, AreaName = "Test" },
            new HexGridCell { Id = 93, HexGridVersionId = 1, Q = -1, R = 0, CenterLatitude = 53.5461, CenterLongitude = -113.5013, HexRadiusMeters = 500, IsActive = true, AreaName = "Test" },
            new HexGridCell { Id = 80, HexGridVersionId = 1, Q = 0, R = -1, CenterLatitude = 53.5394, CenterLongitude = -113.4976, HexRadiusMeters = 500, IsActive = true, AreaName = "Test" },
            new HexGridCell { Id = 85, HexGridVersionId = 1, Q = 1, R = -1, CenterLatitude = 53.5394, CenterLongitude = -113.4900, HexRadiusMeters = 500, IsActive = true, AreaName = "Test" },
            new HexGridCell { Id = 87, HexGridVersionId = 1, Q = 1, R = 0, CenterLatitude = 53.5461, CenterLongitude = -113.4863, HexRadiusMeters = 500, IsActive = true, AreaName = "Test" },
            new HexGridCell { Id = 89, HexGridVersionId = 1, Q = 0, R = 1, CenterLatitude = 53.5528, CenterLongitude = -113.4900, HexRadiusMeters = 500, IsActive = true, AreaName = "Test" },
            new HexGridCell { Id = 91, HexGridVersionId = 1, Q = -1, R = 1, CenterLatitude = 53.5528, CenterLongitude = -113.4976, HexRadiusMeters = 500, IsActive = true, AreaName = "Test" }
        ];
    }
}
