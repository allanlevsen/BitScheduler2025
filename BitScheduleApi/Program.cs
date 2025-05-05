
using Microsoft.EntityFrameworkCore;
using BitSchedulerCore.Services; // Contains BitScheduleDataService
using BitSchedulerCore.Data.BitTimeScheduler.Data; // Contains BitScheduleDbContext
using BitTimeScheduler.Models; // Contains BitScheduleConfiguration, BitDateRange, etc.
using BitTimeScheduler; // Contains BitSchedule, BitDay, etc.
using BitSchedulerCore.Models; // Contains BitDayRequest
using Microsoft.Extensions.DependencyInjection; // Required for GetRequiredService
using Microsoft.Extensions.Logging;
using BitTimeScheduler.Services; // Required for ILogger

var builder = WebApplication.CreateBuilder(args);

// --- Configure Services ---

// Read connection string from appsettings.json.
string connectionString = builder.Configuration.GetConnectionString("BitScheduleConnection");
builder.Services.AddSingleton(provider => connectionString); // Make connection string available if needed elsewhere


// Register the DbContext with the connection string. Scoped lifetime is default and appropriate.
builder.Services.AddDbContext<BitScheduleDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the seeding service (usually Transient or Scoped).
builder.Services.AddScoped<SeedingService>();

// Register the data service (Scoped is often suitable).
builder.Services.AddScoped<BitScheduleDataService>();

// Logging is typically configured by default by WebApplication.CreateBuilder.

// --- Build the App ---
var app = builder.Build();

// --- Middleware & Seeding ---

// Seed the database on startup.
// This is often done conditionally (e.g., only in Development).
// Consider moving seeding logic behind a specific endpoint or command for production.
app.Logger.LogInformation("Attempting database seeding on startup...");
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seeder = services.GetRequiredService<SeedingService>();
        var dbContext = services.GetRequiredService<BitScheduleDbContext>(); // Get context for logging
        app.Logger.LogInformation("Applying migrations (if any)...");
        // Apply migrations - uncomment if needed automatically on startup
        // await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("Seeding initial ResourceTypes and Clients...");
        await seeder.SeedAsync(); // Seed ResourceTypes, Clients, Resources
        app.Logger.LogInformation("Seeding initial Schedule Data (BitDays)...");
        await seeder.SeedScheduleDataAsync(); // Seed BitDays
        app.Logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred during database seeding.");
        // Decide if the application should stop if seeding fails.
    }
}

// --- Minimal API Endpoints ---

app.MapGet("/", () => Results.Ok(new { message = "BitScheduleApi is running!" }));

/// <summary>
/// Reads schedule data based on the provided configuration.
/// POST /ReadSchedule
/// Body: BitScheduleConfiguration object
/// </summary>
app.MapPost("/ReadSchedule", (BitScheduleConfiguration config, HttpContext httpContext) =>
{
    // Resolve necessary services from the request's scope
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<BitSchedule>>();
    var dataService = httpContext.RequestServices.GetRequiredService<BitScheduleDataService>();
    var dbContext = httpContext.RequestServices.GetRequiredService<BitScheduleDbContext>();

    // TODO: Determine the correct ClientId dynamically (e.g., from user claims, request headers, or config)
    int clientId = 1; // Using hardcoded ClientId 1 for now

    try
    {
        // Create a BitSchedule instance using the provided configuration and injected services
        // The constructor now handles the initial data load.
        var schedule = new BitSchedule(clientId, config, dataService, dbContext, logger);

        // Create a BitScheduleRequest from the configuration to pass to ReadSchedule method.
        // Note: ReadSchedule filters the already loaded data based on this request.
        var request = new BitScheduleRequest
        {
            DateRange = config.DateRange,
            ActiveDays = config.ActiveDays,
            TimeBlock = config.TimeBlock // TimeBlock might not be strictly needed for ReadSchedule filtering logic
        };

        // Read the schedule based on the request (filters in-memory data)
        BitScheduleResponse response = schedule.ReadSchedule(request);

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred in /ReadSchedule endpoint for ClientId {ClientId} with config {@Config}", clientId, config);
        return Results.Problem("An error occurred while reading the schedule.", statusCode: 500);
    }
});

/// <summary>
/// Reads schedule data using a predefined test configuration.
/// GET /TestReadSchedule
/// </summary>
app.MapGet("/TestReadSchedule", (HttpContext httpContext) =>
{
    // Resolve necessary services
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<BitSchedule>>();
    var dataService = httpContext.RequestServices.GetRequiredService<BitScheduleDataService>();
    var dbContext = httpContext.RequestServices.GetRequiredService<BitScheduleDbContext>();

    // TODO: Determine the correct ClientId
    int clientId = 1; // Using hardcoded ClientId 1 for now

    // Create a test configuration
    var testConfig = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange
        {
            StartDate = new DateTime(2025, 4, 1), // Adjusted test range
            EndDate = new DateTime(2025, 6, 30)  // Adjusted test range
        },
        ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
        TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
        AutoRefreshOnConfigurationChange = false // Explicitly set if needed
    };

    try
    {
        // Create BitSchedule instance - constructor loads data
        var schedule = new BitSchedule(clientId, testConfig, dataService, dbContext, logger);

        // Build a BitScheduleRequest from the test configuration
        var request = new BitScheduleRequest
        {
            DateRange = testConfig.DateRange,
            ActiveDays = testConfig.ActiveDays,
            TimeBlock = testConfig.TimeBlock
        };

        // Read the schedule (filters already loaded data)
        BitScheduleResponse response = schedule.ReadSchedule(request);

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred in /TestReadSchedule endpoint for ClientId {ClientId}", clientId);
        return Results.Problem("An error occurred while testing reading the schedule.", statusCode: 500);
    }
});

/// <summary>
/// Writes (reserves) a time slot for a specific day.
/// POST /WriteScheduleDay
/// Body: BitDayRequest object (Date, StartTime, EndTime)
/// </summary>
app.MapPost("/WriteScheduleDay", async (BitDayRequest request, HttpContext httpContext) => // Made async
{
    // Resolve necessary services
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<BitSchedule>>();
    var dataService = httpContext.RequestServices.GetRequiredService<BitScheduleDataService>();
    var dbContext = httpContext.RequestServices.GetRequiredService<BitScheduleDbContext>();

    // TODO: Determine the correct ClientId
    int clientId = 1; // Using hardcoded ClientId 1 for now

    // Build a minimal configuration needed to instantiate BitSchedule
    // The DateRange should ideally cover the day being written to,
    // ensuring the relevant BitDay might be loaded by the constructor.
    // If BitSchedule is intended to load data on demand, this might change.
    var config = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange // Configuration needs a valid range
        {
            StartDate = request.Date.Date.AddDays(-1), // Example: Load a small range around the target date
            EndDate = request.Date.Date.AddDays(1)
        },
        ActiveDays = null, // Not strictly needed for WriteDay
        TimeBlock = null   // Not strictly needed for WriteDay
    };

    try
    {
        // Create BitSchedule instance - constructor loads data for the defined range
        var schedule = new BitSchedule(clientId, config, dataService, dbContext, logger);

        // Write (update/create) the day using the async method
        BitDay updatedDay = await schedule.WriteDayAsync(request); // Use await and Async method

        // Return the updated/created BitDay object
        return Results.Ok(updatedDay);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred in /WriteScheduleDay endpoint for ClientId {ClientId} with request {@Request}", clientId, request);
        return Results.Problem("An error occurred while writing the schedule day.", statusCode: 500);
    }
});

/// <summary>
/// Reads the schedule information for a specific day.
/// POST /ReadScheduleDay
/// Body: BitDayRequest object (only Date is strictly needed)
/// </summary>
app.MapPost("/ReadScheduleDay", (BitDayRequest request, HttpContext httpContext) =>
{
    // Resolve necessary services
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<BitSchedule>>();
    var dataService = httpContext.RequestServices.GetRequiredService<BitScheduleDataService>();
    var dbContext = httpContext.RequestServices.GetRequiredService<BitScheduleDbContext>();

    // TODO: Determine the correct ClientId
    int clientId = 1; // Using hardcoded ClientId 1 for now

    // Build a minimal configuration to load data around the requested date
    var config = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange
        {
            StartDate = request.Date.Date.AddDays(-1),
            EndDate = request.Date.Date.AddDays(1)
        },
        ActiveDays = null,
        TimeBlock = null
    };

    try
    {
        // Create BitSchedule instance - constructor loads relevant data
        var schedule = new BitSchedule(clientId, config, dataService, dbContext, logger);

        // Read the day's schedule from the (potentially loaded) in-memory data
        BitDay day = schedule.ReadDay(request.Date);

        return Results.Ok(day);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred in /ReadScheduleDay endpoint for ClientId {ClientId} with request {@Request}", clientId, request);
        return Results.Problem("An error occurred while reading the schedule day.", statusCode: 500);
    }
});


/// <summary>
/// Reads schedule information for a predefined test day.
/// GET /TestReadScheduleDay
/// </summary>
app.MapGet("/TestReadScheduleDay", (HttpContext httpContext) =>
{
    // Resolve necessary services
    var logger = httpContext.RequestServices.GetRequiredService<ILogger<BitSchedule>>();
    var dataService = httpContext.RequestServices.GetRequiredService<BitScheduleDataService>();
    var dbContext = httpContext.RequestServices.GetRequiredService<BitScheduleDbContext>();

    // TODO: Determine the correct ClientId
    int clientId = 1; // Using hardcoded ClientId 1 for now

    // Define the test date
    var testDate = new DateTime(2025, 5, 10); // Using a date in May 2025

    // Build a configuration to load data around the test date
    var config = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange
        {
            StartDate = testDate.Date.AddDays(-1),
            EndDate = testDate.Date.AddDays(1)
        },
        ActiveDays = null,
        TimeBlock = null
    };

    try
    {
        // Create BitSchedule instance - constructor loads data
        var schedule = new BitSchedule(clientId, config, dataService, dbContext, logger);

        // **Removed:** schedule.RefreshScheduleData(); // This method no longer exists / data loaded by constructor

        // Retrieve the BitDay for the test date.
        BitDay day = schedule.ReadDay(testDate);

        return Results.Ok(day);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error occurred in /TestReadScheduleDay endpoint for ClientId {ClientId} testing date {TestDate}", clientId, testDate);
        return Results.Problem("An error occurred while testing reading a schedule day.", statusCode: 500);
    }
});

// --- Run the App ---
app.Run();

////// Schema updates
//////
////// Add-Migration InitialCreate -Project BitSchedulerCore -StartupProject BitScheduleApi
////// Update-Database -Project BitSchedulerCore -StartupProject BitScheduleApi


//using Microsoft.EntityFrameworkCore;
//using BitTimeScheduler.Services;
//using BitSchedulerCore.Data.BitTimeScheduler.Data;
//using BitTimeScheduler.Models;
//using BitTimeScheduler;
//using BitSchedulerCore.Models; // Namespace for BitScheduleDbContext and SeedingService

//var builder = WebApplication.CreateBuilder(args);

//// Read connection string from appsettings.json.
//string connectionString = builder.Configuration.GetConnectionString("BitScheduleConnection");

//// Register the DbContext.
//builder.Services.AddDbContext<BitScheduleDbContext>(options =>
//    options.UseSqlServer(connectionString));

//// Register the seeding service.
//builder.Services.AddTransient<SeedingService>();

//var app = builder.Build();

//// Seed the database on startup.
//using (var scope = app.Services.CreateScope())
//{
//    var seeder = scope.ServiceProvider.GetRequiredService<SeedingService>();
//    await seeder.SeedAsync();
//    await seeder.SeedScheduleDataAsync();
//}


//app.MapGet("/", () => "BitScheduleApi is running!");

///// <summary>
///// POST https://localhost:44385/ReadSchedule
///// Accepts a BitScheduleConfiguration JSON in the body, converts it to a BitScheduleRequest,
///// creates a BitSchedule using the configuration, and returns the schedule response.
///// </summary>
//app.MapPost("/ReadSchedule", (BitScheduleConfiguration config) =>
//{
//    // Create a BitScheduleRequest from the configuration.
//    var request = new BitScheduleRequest
//    {
//        DateRange = config.DateRange,
//        ActiveDays = config.ActiveDays,
//        TimeBlock = config.TimeBlock
//    };

//    // Create a BitSchedule instance using the provided configuration.
//    var schedule = new BitSchedule(config);

//    // Read the schedule based on the request.
//    BitScheduleResponse response = schedule.ReadSchedule(request);

//    return Results.Ok(response);
//});

///// <summary>
///// GET https://localhost:44385/TestReadSchedule
///// Creates a test BitScheduleConfiguration with predefined values, builds a matching BitScheduleRequest,
///// creates a BitSchedule instance with mock data, and returns the schedule response.
///// </summary>
//app.MapGet("/TestReadSchedule", () =>
//{
//    // Create a test configuration with predefined values.
//    var testConfig = new BitScheduleConfiguration
//    {
//        DateRange = new BitDateRange
//        {
//            StartDate = new DateTime(2025, 1, 1),
//            EndDate = new DateTime(2025, 4, 30)
//        },
//        ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
//        // Create a time block from 09:00 to 10:00 using the BitDay utility method.
//        TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
//    };

//    // Build a BitScheduleRequest from the test configuration.
//    var request = new BitScheduleRequest
//    {
//        DateRange = testConfig.DateRange,
//        ActiveDays = testConfig.ActiveDays,
//        TimeBlock = testConfig.TimeBlock
//    };

//    // Create a BitSchedule instance using the test configuration.
//    var schedule = new BitSchedule(testConfig);

//    // Read the schedule.
//    BitScheduleResponse response = schedule.ReadSchedule(request);

//    return Results.Ok(response);
//});

///// <summary>
///// POST https://localhost:44385/WriteScheduleDay
///// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
///// It builds a BitScheduleConfiguration from the request (using the Date as the DateRange and TimeBlock),
///// instantiates a BitSchedule, calls WriteDay, and returns the updated BitDay.
///// </summary>
//app.MapPost("/WriteScheduleDay", (BitDayRequest request) =>
//{
//    // Build a configuration for a single day using the request.
//    var config = new BitScheduleConfiguration
//    {
//        DateRange = new BitDateRange
//        {
//            StartDate = request.Date.Date,
//            EndDate = request.Date.Date  // Single-day range.
//        },
//        // For a single day operation, ActiveDays isn't needed.
//        ActiveDays = null,
//        TimeBlock = BitDay.CreateRangeFromTimes(request.StartTime, request.EndTime)
//    };

//    // Create a new BitSchedule instance using the configuration.
//    var schedule = new BitSchedule(config);

//    // Write (update) the day using the request.
//    BitDay updatedDay = schedule.WriteDay(request);

//    return Results.Ok(updatedDay);
//});

///// <summary>
///// POST https://localhost:44385/ReadScheduleDay
///// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
///// It builds a configuration from the request and instantiates a BitSchedule,
///// then calls ReadDay to retrieve the BitDay for the given date (or a new free BitDay if not present).
///// </summary>
//app.MapPost("/ReadScheduleDay", (BitDayRequest request) =>
//{
//    // Build a configuration for the target day.
//    var config = new BitScheduleConfiguration
//    {
//        DateRange = new BitDateRange
//        {
//            StartDate = request.Date.Date,
//            EndDate = request.Date.Date
//        },
//        ActiveDays = null,
//        TimeBlock = BitDay.CreateRangeFromTimes(request.StartTime, request.EndTime)
//    };

//    // Create a new BitSchedule instance using the configuration.
//    var schedule = new BitSchedule(config);

//    // Read the day's schedule.
//    BitDay day = schedule.ReadDay(request.Date);

//    return Results.Ok(day);
//});


///// <summary>
///// GET https://localhost:44385/TestReadScheduleDay
///// This endpoint has no parameters. It creates a test BitDayRequest internally (with test Date, StartTime, EndTime),
///// builds a BitScheduleConfiguration from it, instantiates a BitSchedule, calls ReadDay for that date,
///// and returns the BitDay. If the day does not exist in the internal schedule data, a new free BitDay is returned.
///// </summary>
//app.MapGet("/TestReadScheduleDay", () =>
//{
//    // Create a test BitDayRequest with predefined values.
//    var testRequest = new BitDayRequest
//    {
//        Date = new DateTime(2025, 2, 10),
//        StartTime = TimeSpan.FromHours(9),
//        EndTime = TimeSpan.FromHours(10)
//    };

//    // Build a BitScheduleConfiguration for a single day using the test request.
//    var config = new BitScheduleConfiguration
//    {
//        DateRange = new BitDateRange
//        {
//            StartDate = testRequest.Date.Date,
//            EndDate = testRequest.Date.Date // Single day
//        },
//        // ActiveDays not needed for a single day operation.
//        ActiveDays = new DayOfWeek[] {
//                        DayOfWeek.Monday,
//                        DayOfWeek.Wednesday,
//                        DayOfWeek.Friday
//        },
//        // Create a TimeBlock from the test StartTime and EndTime.
//        TimeBlock = BitDay.CreateRangeFromTimes(testRequest.StartTime, testRequest.EndTime)
//    };

//    // Create a BitSchedule instance using the configuration.
//    var schedule = new BitSchedule(config);
//    schedule.RefreshScheduleData();

//    // Retrieve the BitDay for the test date.
//    BitDay day = schedule.ReadDay(testRequest.Date);

//    return Results.Ok(day);
//});


//app.Run();
