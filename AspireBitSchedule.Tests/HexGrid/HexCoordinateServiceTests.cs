using BitSchedulerCore.HexGrid;

namespace AspireBitSchedule.Tests.HexGrid;

public sealed class HexCoordinateServiceTests
{
    [Fact]
    public void LatLongToGridCoordinate_ReturnsSameCoordinate_ForSamePoint()
    {
        var service = CreateEdmontonCoordinateService();
        var firstLocal = service.LatLongToLocalMeters(53.5461, -113.4938);
        var secondLocal = service.LatLongToLocalMeters(53.5461, -113.4938);

        var first = service.LocalMetersToAxial(firstLocal.X, firstLocal.Y);
        var second = service.LocalMetersToAxial(secondLocal.X, secondLocal.Y);

        Assert.Equal(first, second);
    }

    [Fact]
    public void LatLongAndLocalMeters_RoundTripNearOrigin()
    {
        var service = CreateEdmontonCoordinateService();

        var local = service.LatLongToLocalMeters(53.60, -113.60);
        var latLong = service.LocalMetersToLatLong(local.X, local.Y);

        Assert.Equal(53.60, latLong.Latitude, precision: 6);
        Assert.Equal(-113.60, latLong.Longitude, precision: 6);
    }

    [Fact]
    public void GetNeighbors_ReturnsSixExpectedAxialCoordinates()
    {
        var neighbors = HexGridGeometry.GetNeighbors(10, 20);

        Assert.Equal(6, neighbors.Count);
        Assert.Equal((Q: 11, R: 20), neighbors[0]);
        Assert.Equal((Q: 11, R: 19), neighbors[1]);
        Assert.Equal((Q: 10, R: 19), neighbors[2]);
        Assert.Equal((Q: 9, R: 20), neighbors[3]);
        Assert.Equal((Q: 9, R: 21), neighbors[4]);
        Assert.Equal((Q: 10, R: 21), neighbors[5]);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(0, 0, 1, 0, 1)]
    [InlineData(0, 0, 1, -1, 1)]
    [InlineData(0, 0, 2, -1, 2)]
    public void GetHexDistance_ReturnsExpectedDistance(
        int q1,
        int r1,
        int q2,
        int r2,
        int expected)
    {
        var actual = HexGridGeometry.GetHexDistance(q1, r1, q2, r2);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetHexPolygon_ReturnsSixVertices()
    {
        var service = CreateEdmontonCoordinateService();

        var polygon = service.GetHexPolygon(53.5461, -113.4938);

        Assert.Equal(6, polygon.Count);
        Assert.Equal(6, polygon.Distinct().Count());
    }

    private static HexCoordinateService CreateEdmontonCoordinateService()
    {
        return new HexCoordinateService(HexGridServiceAreas.EdmontonMetro);
    }
}
