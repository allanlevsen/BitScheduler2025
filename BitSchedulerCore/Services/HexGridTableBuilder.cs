using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public sealed class HexGridTableBuilder
{
    public IReadOnlyList<HexGridNeighbor> BuildNeighbors(IReadOnlyCollection<HexGridCell> cells)
    {
        var cellsByCoordinate = cells.ToDictionary(cell => (cell.Q, cell.R));
        var neighbors = new List<HexGridNeighbor>();

        foreach (var cell in cells.OrderBy(cell => cell.Id))
        {
            foreach (var (direction, deltaQ, deltaR) in HexGridGeometry.DirectionOffsets)
            {
                if (!cellsByCoordinate.TryGetValue((cell.Q + deltaQ, cell.R + deltaR), out var neighborCell))
                {
                    continue;
                }

                neighbors.Add(new HexGridNeighbor
                {
                    HexGridCellId = cell.Id,
                    NeighborHexGridCellId = neighborCell.Id,
                    Direction = direction
                });
            }
        }

        return neighbors;
    }

    public IReadOnlyList<HexGridSearchRing> BuildSearchRings(IReadOnlyCollection<HexGridCell> cells, int maxRingDistance)
    {
        if (maxRingDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRingDistance), "Max ring distance must be zero or greater.");
        }

        var cellsByCoordinate = cells.ToDictionary(cell => (cell.Q, cell.R));
        var rings = new List<HexGridSearchRing>();

        foreach (var cell in cells.OrderBy(cell => cell.Id))
        {
            foreach (var coordinate in HexGridGeometry.GetCoordinatesWithinDistance(cell.Q, cell.R, maxRingDistance))
            {
                if (!cellsByCoordinate.TryGetValue((coordinate.Q, coordinate.R), out var nearbyCell))
                {
                    continue;
                }

                rings.Add(new HexGridSearchRing
                {
                    HexGridCellId = cell.Id,
                    NearbyHexGridCellId = nearbyCell.Id,
                    RingDistance = coordinate.RingDistance
                });
            }
        }

        return rings
            .OrderBy(ring => ring.HexGridCellId)
            .ThenBy(ring => ring.RingDistance)
            .ThenBy(ring => ring.NearbyHexGridCellId)
            .ToArray();
    }
}
