using BitSchedulerCore;
using BitSchedulerCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using BitScheduleServices.Features.Clients;

namespace BitScheduleServices.Features.Schedule;

public sealed class ScheduleFeatureService(
    BitScheduleFactory scheduleFactory,
    ICurrentBitClientAccessor currentBitClientAccessor)
{
    public async Task<IResult> ReadScheduleAsync(BitScheduleConfiguration config, ILogger logger, CancellationToken cancellationToken)
    {
        if (config.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var schedule = scheduleFactory.Create(config, bitClientId);
            var request = CreateScheduleRequest(config);
            var response = schedule.ReadSchedule(request);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading the schedule for the current BitClient with config {@Config}", config);
            return Results.Problem("An error occurred while reading the schedule.", statusCode: 500);
        }
    }

    public async Task<IResult> ReadTestScheduleAsync(ILogger logger, CancellationToken cancellationToken)
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
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var schedule = scheduleFactory.Create(testConfig, bitClientId);
            var request = CreateScheduleRequest(testConfig);
            var response = schedule.ReadSchedule(request);

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading the test schedule for the current BitClient.");
            return Results.Problem("An error occurred while testing reading the schedule.", statusCode: 500);
        }
    }

    public async Task<IResult> WriteScheduleDayAsync(BitDayRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        if (request.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        var config = CreateSingleDayConfiguration(request.BitResourceId, request.Date);

        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var schedule = scheduleFactory.Create(config, bitClientId);
            var updatedDay = await schedule.WriteDayAsync(request);

            return Results.Ok(updatedDay);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while writing a schedule day for the current BitClient with request {@Request}", request);
            return Results.Problem("An error occurred while writing the schedule day.", statusCode: 500);
        }
    }

    public async Task<IResult> WriteScheduleAsync(BitScheduleRequest request, ILogger logger, CancellationToken cancellationToken)
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
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var schedule = scheduleFactory.Create(config, bitClientId);
            var updated = await schedule.WriteScheduleAsync(request);

            return Results.Ok(new { success = updated });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while writing a schedule for the current BitClient with request {@Request}", request);
            return Results.Problem("An error occurred while writing the schedule.", statusCode: 500);
        }
    }

    public async Task<IResult> ReadScheduleDayAsync(BitDayRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        if (request.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        var config = CreateSingleDayConfiguration(request.BitResourceId, request.Date);

        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var schedule = scheduleFactory.Create(config, bitClientId);
            var day = schedule.ReadDay(request.Date);

            return Results.Ok(day);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading a schedule day for the current BitClient with request {@Request}", request);
            return Results.Problem("An error occurred while reading the schedule day.", statusCode: 500);
        }
    }

    public async Task<IResult> ReadTestScheduleDayAsync(ILogger logger, CancellationToken cancellationToken)
    {
        var testDate = new DateTime(2025, 5, 10);
        var config = CreateSingleDayConfiguration(1, testDate);

        try
        {
            var bitClientId = await currentBitClientAccessor.GetCurrentClientIdAsync(cancellationToken);
            var schedule = scheduleFactory.Create(config, bitClientId);
            var day = schedule.ReadDay(testDate);

            return Results.Ok(day);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while reading a test schedule day for the current BitClient testing date {TestDate}", testDate);
            return Results.Problem("An error occurred while testing reading a schedule day.", statusCode: 500);
        }
    }

    private static BitScheduleRequest CreateScheduleRequest(BitScheduleConfiguration config)
    {
        return new BitScheduleRequest
        {
            BitResourceId = config.BitResourceId,
            DateRange = config.DateRange,
            ActiveDays = config.ActiveDays,
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
