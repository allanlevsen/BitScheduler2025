using BitSchedulerCore.HexGrid;

namespace AspireBitSchedule.Tests.HexGrid;

public sealed class HexGridGenerationEngineTests
{
    [Fact]
    public void EdmontonMetro_ServiceArea_UsesExpectedFirstPassConstants()
    {
        var options = HexGridServiceAreas.EdmontonMetro;

        Assert.Equal("EdmontonMetro", options.AreaName);
        Assert.Equal(53.5461, options.OriginLatitude);
        Assert.Equal(-113.4938, options.OriginLongitude);
        Assert.Equal(500, options.HexRadiusMeters);
        Assert.Equal(8, options.MaxPrecomputedRingDistance);
    }

    [Fact]
    public void GenerateCells_CreatesUniqueAxialCoordinates()
    {
        var options = CreateSmallEdmontonTestArea(includeVertices: false);

        var cells = HexGridGenerationEngine.GenerateCells(options);

        Assert.NotEmpty(cells);
        Assert.Equal(cells.Count, cells.Select(cell => (cell.Q, cell.R)).Distinct().Count());
    }

    [Fact]
    public void GenerateCells_CreatesCentersInsideBoundingBox()
    {
        var options = CreateSmallEdmontonTestArea(includeVertices: false);

        var cells = HexGridGenerationEngine.GenerateCells(options);

        Assert.All(cells, cell =>
        {
            Assert.InRange(cell.CenterLatitude, options.MinLatitude, options.MaxLatitude);
            Assert.InRange(cell.CenterLongitude, options.MinLongitude, options.MaxLongitude);
        });
    }

    [Fact]
    public void GenerateCells_CreatesPolygonVertices_WhenRequested()
    {
        var options = CreateSmallEdmontonTestArea(includeVertices: true);

        var cells = HexGridGenerationEngine.GenerateCells(options);

        Assert.NotEmpty(cells);
        Assert.All(cells, cell => Assert.Equal(6, cell.Vertices.Count));
    }

    private static HexGridGenerationOptions CreateSmallEdmontonTestArea(bool includeVertices)
    {
        return new HexGridGenerationOptions
        {
            AreaName = "TestEdmonton",
            OriginLatitude = 53.5461,
            OriginLongitude = -113.4938,
            HexRadiusMeters = 500,
            MinLatitude = 53.52,
            MaxLatitude = 53.57,
            MinLongitude = -113.54,
            MaxLongitude = -113.45,
            IncludePolygonVertices = includeVertices,
            MaxPrecomputedRingDistance = 2
        };
    }
}
