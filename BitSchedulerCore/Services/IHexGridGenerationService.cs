using BitSchedulerCore.HexGrid;

namespace BitSchedulerCore.Services;

public interface IHexGridGenerationService
{
    Task<HexGridGenerationResult> GenerateGridAsync(
        HexGridGenerationOptions options,
        CancellationToken cancellationToken = default);
}
