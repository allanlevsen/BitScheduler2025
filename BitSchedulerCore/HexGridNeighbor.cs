namespace BitSchedulerCore.HexGrid;

public sealed class HexGridNeighbor
{
    public long Id { get; set; }

    public int HexGridCellId { get; set; }
    public int NeighborHexGridCellId { get; set; }

    public HexDirection Direction { get; set; }

    public HexGridCell HexGridCell { get; set; } = null!;
    public HexGridCell NeighborHexGridCell { get; set; } = null!;
}
