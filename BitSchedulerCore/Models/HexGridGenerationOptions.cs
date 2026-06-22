namespace BitSchedulerCore.Models;

public sealed class HexGridGenerationOptions
{
    public string AreaName { get; set; } = null!;

    public double OriginLatitude { get; set; }
    public double OriginLongitude { get; set; }
    public double HexRadiusMeters { get; set; }

    public double MinLatitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLongitude { get; set; }

    public bool IncludePolygonVertices { get; set; }
    public int MaxPrecomputedRingDistance { get; set; } = 8;
}
