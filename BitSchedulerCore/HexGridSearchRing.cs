namespace BitSchedulerCore;

public sealed class HexGridSearchRing
{
    public long Id { get; set; }

    public int HexGridCellId { get; set; }
    public int NearbyHexGridCellId { get; set; }

    public int RingDistance { get; set; }

    public HexGridCell HexGridCell { get; set; } = null!;
    public HexGridCell NearbyHexGridCell { get; set; } = null!;
}
