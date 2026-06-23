using BitScheduleServices.Features.Schedule;
using BitSchedulerCore.Models;

namespace AspireBitSchedule.ApiService.Features.Schedule;

internal static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/schedule");
        group.MapPost("/read", ReadSchedule);
        group.MapGet("/read/test", ReadTestSchedule);
        group.MapPost("/write/day", WriteScheduleDayAsync);
        group.MapPost("/write", WriteScheduleAsync);
        group.MapPost("/day/read", ReadScheduleDay);
        group.MapGet("/day/read/test", ReadTestScheduleDay);

        endpoints.MapGet("/", () => Results.Ok(new { message = "AspireBitSchedule.ApiService is running!" }));
        endpoints.MapPost("/ReadSchedule", ReadSchedule);
        endpoints.MapGet("/TestReadSchedule", ReadTestSchedule);
        endpoints.MapPost("/WriteScheduleDay", WriteScheduleDayAsync);
        endpoints.MapPost("/WriteSchedule", WriteScheduleAsync);
        endpoints.MapPost("/ReadScheduleDay", ReadScheduleDay);
        endpoints.MapGet("/TestReadScheduleDay", ReadTestScheduleDay);

        return endpoints;
    }

    private static Task<IResult> ReadSchedule(
        BitScheduleConfiguration config,
        ScheduleFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ReadScheduleAsync(config, logger, cancellationToken);
    }

    private static Task<IResult> ReadTestSchedule(
        ScheduleFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ReadTestScheduleAsync(logger, cancellationToken);
    }

    private static Task<IResult> WriteScheduleDayAsync(
        BitDayRequest request,
        ScheduleFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.WriteScheduleDayAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> WriteScheduleAsync(
        BitScheduleRequest request,
        ScheduleFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.WriteScheduleAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> ReadScheduleDay(
        BitDayRequest request,
        ScheduleFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ReadScheduleDayAsync(request, logger, cancellationToken);
    }

    private static Task<IResult> ReadTestScheduleDay(
        ScheduleFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ReadTestScheduleDayAsync(logger, cancellationToken);
    }
}
