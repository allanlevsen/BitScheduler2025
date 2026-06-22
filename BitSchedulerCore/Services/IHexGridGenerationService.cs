using BitSchedulerCore.Models;

namespace BitSchedulerCore.Services;

public interface IHexGridGenerationService
{
    Task<HexGridGenerationResult> GenerateGridAsync(
        HexGridGenerationOptions options,
        CancellationToken cancellationToken = default);
}
