using BitScheduleServices.Features.Schedule;
using BitSchedulerCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Features.Resources;

public sealed class ResourceFeatureService(
    IBitResourceService resourceService,
    BitScheduleFactory scheduleFactory)
{
    public async Task<IResult> ListResourcesAsync(ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            var resources = await resourceService.ListResourcesAsync(scheduleFactory.DefaultClient, cancellationToken);
            return Results.Ok(resources);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while listing resources for ClientId {ClientId}", scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while listing resources.", statusCode: 500);
        }
    }
}
