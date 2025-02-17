
//// Schema updates
////
//// Add-Migration InitialCreate -Project BitSchedulerCore -StartupProject BitScheduleApi
//// Update-Database -Project BitSchedulerCore -StartupProject BitScheduleApi


using Microsoft.EntityFrameworkCore;
using BitTimeScheduler.Services;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitTimeScheduler.Models;
using BitTimeScheduler;
using BitSchedulerCore.Models; // Namespace for BitScheduleDbContext and SeedingService

var builder = WebApplication.CreateBuilder(args);

// Read connection string from appsettings.json.
string connectionString = builder.Configuration.GetConnectionString("BitScheduleConnection");

// Register the DbContext.
builder.Services.AddDbContext<BitScheduleDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the seeding service.
builder.Services.AddTransient<SeedingService>();

var app = builder.Build();

// Seed the database on startup.
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SeedingService>();
    await seeder.SeedAsync();
    await seeder.SeedScheduleDataAsync();
}


app.MapGet("/", () => "BitScheduleApi is running!");

/// <summary>
/// POST https://localhost:44385/ReadSchedule
/// Accepts a BitScheduleConfiguration JSON in the body, converts it to a BitScheduleRequest,
/// creates a BitSchedule using the configuration, and returns the schedule response.
/// </summary>
app.MapPost("/ReadSchedule", (BitScheduleConfiguration config) =>
{
    // Create a BitScheduleRequest from the configuration.
    var request = new BitScheduleRequest
    {
        DateRange = config.DateRange,
        ActiveDays = config.ActiveDays,
        TimeBlock = config.TimeBlock
    };

    // Create a BitSchedule instance using the provided configuration.
    var schedule = new BitSchedule(config);

    // Read the schedule based on the request.
    BitScheduleResponse response = schedule.ReadSchedule(request);

    return Results.Ok(response);
});

/// <summary>
/// GET https://localhost:44385/TestReadSchedule
/// Creates a test BitScheduleConfiguration with predefined values, builds a matching BitScheduleRequest,
/// creates a BitSchedule instance with mock data, and returns the schedule response.
/// </summary>
app.MapGet("/TestReadSchedule", () =>
{
    // Create a test configuration with predefined values.
    var testConfig = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange
        {
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 4, 30)
        },
        ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
        // Create a time block from 09:00 to 10:00 using the BitDay utility method.
        TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
    };

    // Build a BitScheduleRequest from the test configuration.
    var request = new BitScheduleRequest
    {
        DateRange = testConfig.DateRange,
        ActiveDays = testConfig.ActiveDays,
        TimeBlock = testConfig.TimeBlock
    };

    // Create a BitSchedule instance using the test configuration.
    var schedule = new BitSchedule(testConfig);

    // Read the schedule.
    BitScheduleResponse response = schedule.ReadSchedule(request);

    return Results.Ok(response);
});

/// <summary>
/// POST https://localhost:44385/WriteScheduleDay
/// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
/// It builds a BitScheduleConfiguration from the request (using the Date as the DateRange and TimeBlock),
/// instantiates a BitSchedule, calls WriteDay, and returns the updated BitDay.
/// </summary>
app.MapPost("/WriteScheduleDay", (BitDayRequest request) =>
{
    // Build a configuration for a single day using the request.
    var config = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange
        {
            StartDate = request.Date.Date,
            EndDate = request.Date.Date  // Single-day range.
        },
        // For a single day operation, ActiveDays isn't needed.
        ActiveDays = null,
        TimeBlock = BitDay.CreateRangeFromTimes(request.StartTime, request.EndTime)
    };

    // Create a new BitSchedule instance using the configuration.
    var schedule = new BitSchedule(config);

    // Write (update) the day using the request.
    BitDay updatedDay = schedule.WriteDay(request);

    return Results.Ok(updatedDay);
});

/// <summary>
/// POST https://localhost:44385/ReadScheduleDay
/// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
/// It builds a configuration from the request and instantiates a BitSchedule,
/// then calls ReadDay to retrieve the BitDay for the given date (or a new free BitDay if not present).
/// </summary>
app.MapPost("/ReadScheduleDay", (BitDayRequest request) =>
{
    // Build a configuration for the target day.
    var config = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange
        {
            StartDate = request.Date.Date,
            EndDate = request.Date.Date
        },
        ActiveDays = null,
        TimeBlock = BitDay.CreateRangeFromTimes(request.StartTime, request.EndTime)
    };

    // Create a new BitSchedule instance using the configuration.
    var schedule = new BitSchedule(config);

    // Read the day's schedule.
    BitDay day = schedule.ReadDay(request.Date);

    return Results.Ok(day);
});


/// <summary>
/// GET https://localhost:44385/TestReadScheduleDay
/// This endpoint has no parameters. It creates a test BitDayRequest internally (with test Date, StartTime, EndTime),
/// builds a BitScheduleConfiguration from it, instantiates a BitSchedule, calls ReadDay for that date,
/// and returns the BitDay. If the day does not exist in the internal schedule data, a new free BitDay is returned.
/// </summary>
app.MapGet("/TestReadScheduleDay", () =>
{
    // Create a test BitDayRequest with predefined values.
    var testRequest = new BitDayRequest
    {
        Date = new DateTime(2025, 2, 10),
        StartTime = TimeSpan.FromHours(9),
        EndTime = TimeSpan.FromHours(10)
    };

    // Build a BitScheduleConfiguration for a single day using the test request.
    var config = new BitScheduleConfiguration
    {
        DateRange = new BitDateRange
        {
            StartDate = testRequest.Date.Date,
            EndDate = testRequest.Date.Date // Single day
        },
        // ActiveDays not needed for a single day operation.
        ActiveDays = new DayOfWeek[] {
                        DayOfWeek.Monday,
                        DayOfWeek.Wednesday,
                        DayOfWeek.Friday
        },
        // Create a TimeBlock from the test StartTime and EndTime.
        TimeBlock = BitDay.CreateRangeFromTimes(testRequest.StartTime, testRequest.EndTime)
    };

    // Create a BitSchedule instance using the configuration.
    var schedule = new BitSchedule(config);
    schedule.RefreshScheduleData();

    // Retrieve the BitDay for the test date.
    BitDay day = schedule.ReadDay(testRequest.Date);

    return Results.Ok(day);
});


app.Run();





//using BitTimeScheduler.Models;
//using BitTimeScheduler;
//using BitScheduleApi.Utility;
//using BitSchedulerCore.Models;
//using BitSchedulerCore.Data.BitTimeScheduler.Data;
//using Microsoft.EntityFrameworkCore;
//using BitTimeScheduler.Services;

//namespace BitScheduleApi
//{
//    public class Program
//    {
//        public async static void Main(string[] args)
//        {
//            var builder = WebApplication.CreateBuilder(args);

//            // Add services to the container.
//            builder.Services.AddAuthorization();

//            // Add configurations
//            builder.Services.ConfigureHttpJsonOptions(options =>
//            {
//                options.SerializerOptions.Converters.Add(new ULongConverter());
//            });

//            // Retrieve the connection string from appsettings.json.
//            string connectionString = builder.Configuration.GetConnectionString("BitScheduleConnection");

//            // Register the DbContext with SQL Server.
//            builder.Services.AddDbContext<BitScheduleDbContext>(options =>
//                options.UseSqlServer(connectionString));

//            // Register the seeding service.
//            builder.Services.AddTransient<SeedingService>();

//            var app = builder.Build();

//            // Seed the database on startup.
//            using (var scope = app.Services.CreateScope())
//            {
//                var seeder = scope.ServiceProvider.GetRequiredService<SeedingService>();
//                await seeder.SeedAsync();
//            }

//            app.UseHttpsRedirection();
//            app.UseAuthorization();

//            app.MapGet("/", (HttpContext httpContext) =>
//            {
//                return "BitScheduler Api is running...";
//            });


//            /// <summary>
//            /// POST https://localhost:44385/ReadSchedule
//            /// Accepts a BitScheduleConfiguration JSON in the body, converts it to a BitScheduleRequest,
//            /// creates a BitSchedule using the configuration, and returns the schedule response.
//            /// </summary>
//            app.MapPost("/ReadSchedule", (BitScheduleConfiguration config) =>
//            {
//                // Create a BitScheduleRequest from the configuration.
//                var request = new BitScheduleRequest
//                {
//                    DateRange = config.DateRange,
//                    ActiveDays = config.ActiveDays,
//                    TimeBlock = config.TimeBlock
//                };

//                // Create a BitSchedule instance using the provided configuration.
//                var schedule = new BitSchedule(config);

//                // Read the schedule based on the request.
//                BitScheduleResponse response = schedule.ReadSchedule(request);

//                return Results.Ok(response);
//            });

//            /// <summary>
//            /// GET https://localhost:44385/TestReadSchedule
//            /// Creates a test BitScheduleConfiguration with predefined values, builds a matching BitScheduleRequest,
//            /// creates a BitSchedule instance with mock data, and returns the schedule response.
//            /// </summary>
//            app.MapGet("/TestReadSchedule", () =>
//            {
//                // Create a test configuration with predefined values.
//                var testConfig = new BitScheduleConfiguration
//                {
//                    DateRange = new BitDateRange
//                    {
//                        StartDate = new DateTime(2025, 1, 1),
//                        EndDate = new DateTime(2025, 4, 30)
//                    },
//                    ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
//                    // Create a time block from 09:00 to 10:00 using the BitDay utility method.
//                    TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
//                };

//                // Build a BitScheduleRequest from the test configuration.
//                var request = new BitScheduleRequest
//                {
//                    DateRange = testConfig.DateRange,
//                    ActiveDays = testConfig.ActiveDays,
//                    TimeBlock = testConfig.TimeBlock
//                };

//                // Create a BitSchedule instance using the test configuration.
//                var schedule = new BitSchedule(testConfig);

//                // Read the schedule.
//                BitScheduleResponse response = schedule.ReadSchedule(request);

//                return Results.Ok(response);
//            });

//            /// <summary>
//            /// POST https://localhost:44385/WriteScheduleDay
//            /// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
//            /// It builds a BitScheduleConfiguration from the request (using the Date as the DateRange and TimeBlock),
//            /// instantiates a BitSchedule, calls WriteDay, and returns the updated BitDay.
//            /// </summary>
//            app.MapPost("/WriteScheduleDay", (BitDayRequest request) =>
//            {
//                // Build a configuration for a single day using the request.
//                var config = new BitScheduleConfiguration
//                {
//                    DateRange = new BitDateRange
//                    {
//                        StartDate = request.Date.Date,
//                        EndDate = request.Date.Date  // Single-day range.
//                    },
//                    // For a single day operation, ActiveDays isn't needed.
//                    ActiveDays = null,
//                    TimeBlock = BitDay.CreateRangeFromTimes(request.StartTime, request.EndTime)
//                };

//                // Create a new BitSchedule instance using the configuration.
//                var schedule = new BitSchedule(config);

//                // Write (update) the day using the request.
//                BitDay updatedDay = schedule.WriteDay(request);

//                return Results.Ok(updatedDay);
//            });

//            /// <summary>
//            /// POST https://localhost:44385/ReadScheduleDay
//            /// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
//            /// It builds a configuration from the request and instantiates a BitSchedule,
//            /// then calls ReadDay to retrieve the BitDay for the given date (or a new free BitDay if not present).
//            /// </summary>
//            app.MapPost("/ReadScheduleDay", (BitDayRequest request) =>
//            {
//                // Build a configuration for the target day.
//                var config = new BitScheduleConfiguration
//                {
//                    DateRange = new BitDateRange
//                    {
//                        StartDate = request.Date.Date,
//                        EndDate = request.Date.Date
//                    },
//                    ActiveDays = null,
//                    TimeBlock = BitDay.CreateRangeFromTimes(request.StartTime, request.EndTime)
//                };

//                // Create a new BitSchedule instance using the configuration.
//                var schedule = new BitSchedule(config);

//                // Read the day's schedule.
//                BitDay day = schedule.ReadDay(request.Date);

//                return Results.Ok(day);
//            });


//            /// <summary>
//            /// GET https://localhost:44385/TestReadScheduleDay
//            /// This endpoint has no parameters. It creates a test BitDayRequest internally (with test Date, StartTime, EndTime),
//            /// builds a BitScheduleConfiguration from it, instantiates a BitSchedule, calls ReadDay for that date,
//            /// and returns the BitDay. If the day does not exist in the internal schedule data, a new free BitDay is returned.
//            /// </summary>
//            app.MapGet("/TestReadScheduleDay", () =>
//            {
//                // Create a test BitDayRequest with predefined values.
//                var testRequest = new BitDayRequest
//                {
//                    Date = new DateTime(2025, 2, 10),
//                    StartTime = TimeSpan.FromHours(9),
//                    EndTime = TimeSpan.FromHours(10)
//                };

//                // Build a BitScheduleConfiguration for a single day using the test request.
//                var config = new BitScheduleConfiguration
//                {
//                    DateRange = new BitDateRange
//                    {
//                        StartDate = testRequest.Date.Date,
//                        EndDate = testRequest.Date.Date // Single day
//                    },
//                    // ActiveDays not needed for a single day operation.
//                    ActiveDays = new DayOfWeek[] {
//                        DayOfWeek.Monday,
//                        DayOfWeek.Wednesday,
//                        DayOfWeek.Friday
//                    },
//                    // Create a TimeBlock from the test StartTime and EndTime.
//                    TimeBlock = BitDay.CreateRangeFromTimes(testRequest.StartTime, testRequest.EndTime)
//                };

//                // Create a BitSchedule instance using the configuration.
//                var schedule = new BitSchedule(config);
//                schedule.RefreshScheduleData();

//                // Retrieve the BitDay for the test date.
//                BitDay day = schedule.ReadDay(testRequest.Date);

//                return Results.Ok(day);
//            });

//            app.Run();
//        }
//    }
//}




