namespace BitSchedulerCore.Models;

public sealed class HexGridLookup
{
    public static HexGridLookup Empty { get; } = new()
    {
        HexGridVersionId = 0,
        AreaName = string.Empty,
        OriginLatitude = 0,
        OriginLongitude = 0,
        HexRadiusMeters = 1,
        MinLatitude = 0,
        MaxLatitude = 0,
        MinLongitude = 0,
        MaxLongitude = 0,
        CellsById = new Dictionary<int, HexCellDto>(),
        CellIdsByCoordinate = new Dictionary<(int Q, int R), int>(),
        NeighborIdsByCellId = new Dictionary<int, int[]>(),
        RingIdsByCellId = new Dictionary<int, IReadOnlyDictionary<int, int[]>>()
    };

    public required int HexGridVersionId { get; init; }
    public required string AreaName { get; init; }

    public required double OriginLatitude { get; init; }
    public required double OriginLongitude { get; init; }
    public required double HexRadiusMeters { get; init; }

    public required double MinLatitude { get; init; }
    public required double MaxLatitude { get; init; }
    public required double MinLongitude { get; init; }
    public required double MaxLongitude { get; init; }

    public required IReadOnlyDictionary<int, HexCellDto> CellsById { get; init; }
    public required IReadOnlyDictionary<(int Q, int R), int> CellIdsByCoordinate { get; init; }
    public required IReadOnlyDictionary<int, int[]> NeighborIdsByCellId { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyDictionary<int, int[]>> RingIdsByCellId { get; init; }

    public bool IsEmpty => HexGridVersionId <= 0 || CellsById.Count == 0;
}

public sealed class HexCellDto
{
    public int Id { get; init; }
    public int Q { get; init; }
    public int R { get; init; }
    public double CenterLatitude { get; init; }
    public double CenterLongitude { get; init; }
    public double HexRadiusMeters { get; init; }
}
