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

    private static IResult ReadSchedule(
        BitScheduleConfiguration config,
        ScheduleFeatureService featureService,
        ILogger<Program> logger)
    {
        return featureService.ReadSchedule(config, logger);
    }

    private static IResult ReadTestSchedule(
        ScheduleFeatureService featureService,
        ILogger<Program> logger)
    {
        return featureService.ReadTestSchedule(logger);
    }

    private static Task<IResult> WriteScheduleDayAsync(
        BitDayRequest request,
        ScheduleFeatureService featureService,
        ILogger<Program> logger)
    {
        return featureService.WriteScheduleDayAsync(request, logger);
    }

    private static Task<IResult> WriteScheduleAsync(
        BitScheduleRequest request,
        ScheduleFeatureService featureService,
        ILogger<Program> logger)
    {
        return featureService.WriteScheduleAsync(request, logger);
    }

    private static IResult ReadScheduleDay(
        BitDayRequest request,
        ScheduleFeatureService featureService,
        ILogger<Program> logger)
    {
        return featureService.ReadScheduleDay(request, logger);
    }

    private static IResult ReadTestScheduleDay(
        ScheduleFeatureService featureService,
        ILogger<Program> logger)
    {
        return featureService.ReadTestScheduleDay(logger);
    }
}
