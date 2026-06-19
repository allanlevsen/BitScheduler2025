using BitScheduleApi.Services;
using BitSchedulerCore;
using BitSchedulerCore.HexGrid;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;

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
        endpoints.MapPost("/Events", CreateEventAsync);
        endpoints.MapGet("/Events/{bitEventId:int}", GetEventAsync);
        endpoints.MapGet("/TestReadScheduleDay", ReadTestScheduleDay);
        endpoints.MapPost("/HexGrid/GenerateEdmontonMetro", GenerateEdmontonMetroHexGridAsync);
        endpoints.MapPost("/HexGrid/{versionId:int}/BuildTables", BuildHexGridTablesAsync);
        endpoints.MapPost("/HexGrid/Reload", ReloadHexGridLookupAsync);
        endpoints.MapGet("/HexGrid/Cell", GetHexGridCell);
        endpoints.MapGet("/HexGrid/{gridId:int}/Neighbors", GetHexGridNeighbors);
        endpoints.MapGet("/HexGrid/{gridId:int}/Ring/{maxRingDistance:int}", GetHexGridRing);

        return endpoints;
    }

    private static async Task<IResult> GenerateEdmontonMetroHexGridAsync(
        IHexGridGenerationService generationService,
        IHexGridTableService tableService,
        IHexGridLookupProvider lookupProvider,
        CancellationToken cancellationToken)
    {
        var options = HexGridServiceAreas.EdmontonMetro;
        var result = await generationService.GenerateGridAsync(options, cancellationToken);

        await tableService.BuildNeighborTableAsync(result.HexGridVersionId, cancellationToken);
        await tableService.BuildSearchRingTableAsync(
            result.HexGridVersionId,
            options.MaxPrecomputedRingDistance,
            cancellationToken);
        await lookupProvider.ReloadAsync(cancellationToken);

        return Results.Ok(new
        {
            result.HexGridVersionId,
            result.AreaName,
            result.Name,
            result.CellCount,
            result.VertexCount,
            options.MaxPrecomputedRingDistance
        });
    }

    private static async Task<IResult> BuildHexGridTablesAsync(
        int versionId,
        int? maxRingDistance,
        IHexGridTableService tableService,
        IHexGridLookupProvider lookupProvider,
        CancellationToken cancellationToken)
    {
        var ringDistance = maxRingDistance ?? HexGridServiceAreas.EdmontonMetro.MaxPrecomputedRingDistance;

        await tableService.BuildNeighborTableAsync(versionId, cancellationToken);
        await tableService.BuildSearchRingTableAsync(versionId, ringDistance, cancellationToken);
        await lookupProvider.ReloadAsync(cancellationToken);

        return Results.Ok(new
        {
            HexGridVersionId = versionId,
            MaxRingDistance = ringDistance
        });
    }

    private static async Task<IResult> ReloadHexGridLookupAsync(
        IHexGridLookupProvider lookupProvider,
        CancellationToken cancellationToken)
    {
        await lookupProvider.ReloadAsync(cancellationToken);
        return Results.Ok(new
        {
            lookupProvider.Current.HexGridVersionId,
            lookupProvider.Current.AreaName,
            CellCount = lookupProvider.Current.CellsById.Count
        });
    }

    private static IResult GetHexGridCell(double latitude, double longitude, IHexGridSearchService searchService)
    {
        var gridId = searchService.GetGridId(latitude, longitude);
        if (!gridId.HasValue)
        {
            return Results.NotFound();
        }

        return Results.Ok(searchService.GetCell(gridId.Value));
    }

    private static IResult GetHexGridNeighbors(int gridId, IHexGridSearchService searchService)
    {
        return Results.Ok(searchService.GetNeighborGridIds(gridId));
    }

    private static IResult GetHexGridRing(int gridId, int maxRingDistance, IHexGridSearchService searchService)
    {
        return Results.Ok(searchService.GetGridIdsWithinRing(gridId, maxRingDistance));
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

    private static async Task<IResult> CreateEventAsync(
        BitEventRequest request,
        IBitEventService eventService,
        BitScheduleFactory scheduleFactory,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        if (request.BitResourceId <= 0)
        {
            return Results.BadRequest("A valid BitResourceId is required.");
        }

        try
        {
            var bitEvent = await eventService.CreateEventAsync(
                scheduleFactory.DefaultClient,
                request,
                cancellationToken);

            return Results.Ok(bitEvent);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid event request received: {@Request}", request);
            return Results.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Event creation conflict for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Conflict(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /Events endpoint for ClientId {ClientId} with request {@Request}", scheduleFactory.DefaultClient, request);
            return Results.Problem("An error occurred while creating the event.", statusCode: 500);
        }
    }

    private static async Task<IResult> GetEventAsync(
        int bitEventId,
        IBitEventService eventService,
        BitScheduleFactory scheduleFactory,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var bitEvent = await eventService.GetEventAsync(scheduleFactory.DefaultClient, bitEventId, cancellationToken);
            return bitEvent is null ? Results.NotFound() : Results.Ok(bitEvent);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred in /Events/{BitEventId} endpoint for ClientId {ClientId}", bitEventId, scheduleFactory.DefaultClient);
            return Results.Problem("An error occurred while reading the event.", statusCode: 500);
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
