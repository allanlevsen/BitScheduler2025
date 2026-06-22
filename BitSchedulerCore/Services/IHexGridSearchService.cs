using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IHexGridSearchService
{
    Task ReloadAsync(CancellationToken cancellationToken = default);

    int? GetGridId(double latitude, double longitude);

    HexCellDto? GetCell(int gridId);

    IReadOnlyList<int> GetNeighborGridIds(int gridId);

    IReadOnlyList<int> GetGridIdsWithinRing(int gridId, int maxRingDistance);

    IReadOnlyList<int> GetGridIdsWithinRing(
        double latitude,
        double longitude,
        int maxRingDistance);

    IReadOnlyList<(double Latitude, double Longitude)> GetHexPolygon(int gridId);

    IReadOnlyList<int> ExpandSearch(
        int gridId,
        int startRing,
        int endRing);
}
