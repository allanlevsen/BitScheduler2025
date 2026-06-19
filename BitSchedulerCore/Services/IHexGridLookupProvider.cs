using BitSchedulerCore.HexGrid;

namespace BitSchedulerCore.Services;

public interface IHexGridLookupProvider
{
    HexGridLookup Current { get; }
    Task ReloadAsync(CancellationToken cancellationToken = default);
}
