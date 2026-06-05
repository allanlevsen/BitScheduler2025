using BitScheduleApi.Services;
using BitSchedulerCore;
using BitSchedulerCore.Models;
using BitTimeScheduler.Models;

namespace BitScheduleApi.Extensions;

internal static class ScheduleApiEndpoints
{
    public static IEndpointRouteBuilder MapScheduleApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Ok(new { message = "BitScheduleApi is running!" }));

        endpoints.MapPost("/ReadSchedule", ReadSchedule);
        endpoints.MapGet("/TestReadSchedule", ReadTestSchedule);
        endpoints.MapPost("/WriteScheduleDay", WriteScheduleDayAsync);
        endpoints.MapPost("/WriteSchedule", WriteScheduleAsync);
        endpoints.MapPost("/ReadScheduleDay", ReadScheduleDay);
        endpoints.MapGet("/TestReadScheduleDay", ReadTestScheduleDay);

        return endpoints;
    }

    private static IResult ReadSchedule(BitScheduleConfiguration config, BitScheduleFactory scheduleFactory, ILogger<Program> logger)
    {
        if (config.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        try
        {
            var schedule = scheduleFactory.Create(config);
            var request = CreateScheduleRequest(config);
            var response = schedule.ReadSchedule(request);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /ReadSchedule endpoint for ClientId {ClientId} with config {@Config}", scheduleFactory.DefaultClient, config);
            return Results.Problem("An error occurred while reading the schedule.", statusCode: 500);
        }
    }

    private static IResult ReadTestSchedule(BitScheduleFactory scheduleFactory, ILogger<Program> logger)
    {
        var testConfig = new BitScheduleConfiguration
        {
            BitResourceId = 1,
            DateRange = new BitDateRange
            {
                StartDate = new DateTime(2025, 4, 1),
                EndDate = new DateTime(2025, 6, 30)
            },
            ActiveDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9d), TimeSpan.FromHours(10d)),
            AutoRefreshOnConfigurationChange = false
        };

        try
        {
            var schedule = scheduleFactory.Create(testConfig);
            var request = CreateScheduleRequest(testConfig);
            var response = schedule.ReadSchedule(request);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /TestReadSchedule endpoint for ClientId {ClientId}", scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while testing reading the schedule.", statusCode: 500);
        }
    }

    private static async Task<IResult> WriteScheduleDayAsync(BitDayRequest request, BitScheduleFactory scheduleFactory, ILogger<Program> logger)
    {
        if (request.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        var config = CreateSingleDayConfiguration(request.BitResourceId, request.Date);

        try
        {
            var schedule = scheduleFactory.Create(config);
            var updatedDay = await schedule.WriteDayAsync(request);

            return Results.Ok(updatedDay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /WriteScheduleDay endpoint for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Problem("An error occurred while writing the schedule day.", statusCode: 500);
        }
    }

    private static async Task<IResult> WriteScheduleAsync(BitScheduleRequest request, BitScheduleFactory scheduleFactory, ILogger<Program> logger)
    {
        if (request.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        var config = new BitScheduleConfiguration
        {
            BitResourceId = request.BitResourceId,
            DateRange = request.DateRange,
            ActiveDays = request.ActiveDays,
            TimeBlock = request.TimeBlock
        };

        try
        {
            var schedule = scheduleFactory.Create(config);
            var updated = await schedule.WriteScheduleAsync(request);

            return Results.Ok(new { success = updated });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /WriteSchedule endpoint for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Problem("An error occurred while writing the schedule.", statusCode: 500);
        }
    }

    private static IResult ReadScheduleDay(BitDayRequest request, BitScheduleFactory scheduleFactory, ILogger<Program> logger)
    {
        if (request.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        var config = CreateSingleDayConfiguration(request.BitResourceId, request.Date);

        try
        {
            var schedule = scheduleFactory.Create(config);
            var day = schedule.ReadDay(request.Date);

            return Results.Ok(day);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /ReadScheduleDay endpoint for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Problem("An error occurred while reading the schedule day.", statusCode: 500);
        }
    }

    private static IResult ReadTestScheduleDay(BitScheduleFactory scheduleFactory, ILogger<Program> logger)
    {
        var testDate = new DateTime(2025, 5, 10);
        var config = CreateSingleDayConfiguration(1, testDate);

        try
        {
            var schedule = scheduleFactory.Create(config);
            var day = schedule.ReadDay(testDate);

            return Results.Ok(day);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /TestReadScheduleDay endpoint for ClientId {ClientId} testing date {TestDate}", scheduleFactory.DefaultClient, testDate);
            return Results.Problem("An error occurred while testing reading a schedule day.", statusCode: 500);
        }
    }

    private static BitScheduleRequest CreateScheduleRequest(BitScheduleConfiguration config)
    {
        return new BitScheduleRequest
        {
            BitResourceId = config.BitResourceId,
            DateRange = config.DateRange,
            ActiveDays = config.ActiveDays!,
            TimeBlock = config.TimeBlock
        };
    }

    private static BitScheduleConfiguration CreateSingleDayConfiguration(int bitResourceId, DateTime date)
    {
        return new BitScheduleConfiguration
        {
            BitResourceId = bitResourceId,
            DateRange = new BitDateRange
            {
                StartDate = date.Date.AddDays(-1),
                EndDate = date.Date.AddDays(1)
            }
        };
    }
}
