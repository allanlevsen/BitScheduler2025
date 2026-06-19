namespace BitSchedulerCore.Services;

public interface IHexGridTableService
{
    Task BuildNeighborTableAsync(
        int hexGridVersionId,
        CancellationToken cancellationToken = default);

    Task BuildSearchRingTableAsync(
        int hexGridVersionId,
        int maxRingDistance,
        CancellationToken cancellationToken = default);
}
