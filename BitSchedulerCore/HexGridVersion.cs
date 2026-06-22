using System.ComponentModel.DataAnnotations;

namespace BitSchedulerCore;

public sealed class HexGridVersion
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string AreaName { get; set; } = null!;
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    public double OriginLatitude { get; set; }
    public double OriginLongitude { get; set; }
    public double HexRadiusMeters { get; set; }

    public double MinLatitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLongitude { get; set; }

    public int MaxPrecomputedRingDistance { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }

    public ICollection<HexGridCell> Cells { get; set; } = new List<HexGridCell>();
}
