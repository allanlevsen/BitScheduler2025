using BitSchedulerCore.HexGrid;

namespace BitSchedulerCore.Services;

public sealed class HexGridSearchService(IHexGridLookupProvider lookupProvider) : IHexGridSearchService
{
    public Task ReloadAsync(CancellationToken cancellationToken = default)
    {
        return lookupProvider.ReloadAsync(cancellationToken);
    }

    public int? GetGridId(double latitude, double longitude)
    {
        var lookup = lookupProvider.Current;
        if (lookup.IsEmpty || !IsInsideBoundingBox(latitude, longitude, lookup))
        {
            return null;
        }

        var coordinateService = CreateCoordinateService(lookup);
        var local = coordinateService.LatLongToLocalMeters(latitude, longitude);
        var coordinate = coordinateService.LocalMetersToAxial(local.X, local.Y);

        return lookup.CellIdsByCoordinate.TryGetValue(coordinate, out var gridId)
            ? gridId
            : null;
    }

    public HexCellDto? GetCell(int gridId)
    {
        return lookupProvider.Current.CellsById.TryGetValue(gridId, out var cell)
            ? cell
            : null;
    }

    public IReadOnlyList<int> GetNeighborGridIds(int gridId)
    {
        return lookupProvider.Current.NeighborIdsByCellId.TryGetValue(gridId, out var neighbors)
            ? neighbors
            : [];
    }

    public IReadOnlyList<int> GetGridIdsWithinRing(int gridId, int maxRingDistance)
    {
        return ExpandSearch(gridId, 0, maxRingDistance);
    }

    public IReadOnlyList<int> GetGridIdsWithinRing(double latitude, double longitude, int maxRingDistance)
    {
        var gridId = GetGridId(latitude, longitude);
        return gridId.HasValue
            ? GetGridIdsWithinRing(gridId.Value, maxRingDistance)
            : [];
    }

    public IReadOnlyList<(double Latitude, double Longitude)> GetHexPolygon(int gridId)
    {
        var lookup = lookupProvider.Current;
        if (!lookup.CellsById.TryGetValue(gridId, out var cell))
        {
            return [];
        }

        return CreateCoordinateService(lookup).GetHexPolygon(cell.CenterLatitude, cell.CenterLongitude);
    }

    public IReadOnlyList<int> ExpandSearch(int gridId, int startRing, int endRing)
    {
        if (startRing < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(startRing), "Start ring must be zero or greater.");
        }

        if (endRing < startRing)
        {
            throw new ArgumentOutOfRangeException(nameof(endRing), "End ring must be greater than or equal to start ring.");
        }

        var lookup = lookupProvider.Current;
        if (!lookup.RingIdsByCellId.TryGetValue(gridId, out var ringsByDistance))
        {
            return [];
        }

        return ringsByDistance
            .Where(pair => pair.Key >= startRing && pair.Key <= endRing)
            .OrderBy(pair => pair.Key)
            .SelectMany(pair => pair.Value)
            .Distinct()
            .ToArray();
    }

    private static HexCoordinateService CreateCoordinateService(HexGridLookup lookup)
    {
        return new HexCoordinateService(lookup.OriginLatitude, lookup.OriginLongitude, lookup.HexRadiusMeters);
    }

    private static bool IsInsideBoundingBox(double latitude, double longitude, HexGridLookup lookup)
    {
        return latitude >= lookup.MinLatitude &&
               latitude <= lookup.MaxLatitude &&
               longitude >= lookup.MinLongitude &&
               longitude <= lookup.MaxLongitude;
    }
}
