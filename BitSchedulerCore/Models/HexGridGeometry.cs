namespace BitSchedulerCore.HexGrid;

public static class HexGridGeometry
{
    public static readonly IReadOnlyList<(HexDirection Direction, int DeltaQ, int DeltaR)> DirectionOffsets =
    [
        (HexDirection.East, 1, 0),
        (HexDirection.NorthEast, 1, -1),
        (HexDirection.NorthWest, 0, -1),
        (HexDirection.West, -1, 0),
        (HexDirection.SouthWest, -1, 1),
        (HexDirection.SouthEast, 0, 1)
    ];

    public static IReadOnlyList<(int Q, int R)> GetNeighbors(int q, int r)
    {
        return DirectionOffsets
            .Select(offset => (Q: q + offset.DeltaQ, R: r + offset.DeltaR))
            .ToArray();
    }

    public static int GetHexDistance(int q1, int r1, int q2, int r2)
    {
        var s1 = -q1 - r1;
        var s2 = -q2 - r2;

        return Math.Max(
            Math.Max(Math.Abs(q1 - q2), Math.Abs(r1 - r2)),
            Math.Abs(s1 - s2));
    }

    public static IEnumerable<(int Q, int R, int RingDistance)> GetCoordinatesWithinDistance(int centerQ, int centerR, int radius)
    {
        if (radius < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be zero or greater.");
        }

        for (var dq = -radius; dq <= radius; dq++)
        {
            var r1 = Math.Max(-radius, -dq - radius);
            var r2 = Math.Min(radius, -dq + radius);

            for (var dr = r1; dr <= r2; dr++)
            {
                var q = centerQ + dq;
                var r = centerR + dr;
                yield return (q, r, GetHexDistance(centerQ, centerR, q, r));
            }
        }
    }
}
