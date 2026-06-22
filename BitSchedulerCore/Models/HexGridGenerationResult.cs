namespace BitSchedulerCore.Models;

public sealed class HexGridGenerationResult
{
    public required int HexGridVersionId { get; init; }
    public required string AreaName { get; init; }
    public required string Name { get; init; }
    public required int CellCount { get; init; }
    public required int VertexCount { get; init; }
    public required IReadOnlyList<HexGridCell> Cells { get; init; }
}
