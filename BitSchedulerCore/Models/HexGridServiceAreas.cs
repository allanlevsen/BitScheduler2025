namespace BitSchedulerCore.Models;

public static class HexGridServiceAreas
{
    public static readonly HexGridGenerationOptions EdmontonMetro = new()
    {
        AreaName = "EdmontonMetro",
        OriginLatitude = 53.5461,
        OriginLongitude = -113.4938,
        HexRadiusMeters = 500,
        MinLatitude = 53.20,
        MaxLatitude = 53.85,
        MinLongitude = -114.25,
        MaxLongitude = -112.75,
        IncludePolygonVertices = true,
        MaxPrecomputedRingDistance = 8
    };
}
