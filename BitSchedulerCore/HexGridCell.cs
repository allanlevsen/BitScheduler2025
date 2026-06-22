using System.ComponentModel.DataAnnotations;

namespace BitSchedulerCore;

public sealed class HexGridCell
{
    public int Id { get; set; }

    public int HexGridVersionId { get; set; }

    public int Q { get; set; }
    public int R { get; set; }

    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }

    public double HexRadiusMeters { get; set; }

    public bool IsActive { get; set; }
    [MaxLength(100)]
    public string? AreaName { get; set; }

    public DateTime CreatedUtc { get; set; }

    public HexGridVersion HexGridVersion { get; set; } = null!;
    public ICollection<HexGridCellVertex> Vertices { get; set; } = new List<HexGridCellVertex>();
    public ICollection<HexGridNeighbor> Neighbors { get; set; } = new List<HexGridNeighbor>();
    public ICollection<HexGridNeighbor> NeighborOf { get; set; } = new List<HexGridNeighbor>();
    public ICollection<HexGridSearchRing> SearchRings { get; set; } = new List<HexGridSearchRing>();
    public ICollection<HexGridSearchRing> NearbySearchRings { get; set; } = new List<HexGridSearchRing>();
}
