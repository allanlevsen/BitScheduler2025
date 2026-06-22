namespace BitSchedulerCore;

public sealed class HexGridCellVertex
{
    public long Id { get; set; }

    public int HexGridCellId { get; set; }
    public int VertexOrder { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public HexGridCell HexGridCell { get; set; } = null!;
}
