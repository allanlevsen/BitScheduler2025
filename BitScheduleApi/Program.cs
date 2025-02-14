
// Schema updates
//
// Add-Migration InitialCreate -Project BitSchedulerCore -StartupProject BitScheduleApi
// Update-Database -Project BitSchedulerCore -StartupProject BitScheduleApi


using BitTimeScheduler.Models;
using BitTimeScheduler;
using BitScheduleApi.Utility;
using BitSchedulerCore.Models;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace BitScheduleApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Add configurations
            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new ULongConverter());
            });

            // Retrieve the connection string from appsettings.json.
            string connectionString = builder.Configuration.GetConnectionString("BitScheduleConnection");

            // Register the DbContext with SQL Server.
            builder.Services.AddDbContext<BitScheduleDbContext>(options =>
                options.UseSqlServer(connectionString));

            var app = builder.Build();

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapGet("/", (HttpContext httpContext) =>
            {
                return "BitScheduler Api is running...";
            });


            /// <summary>
            /// POST https://localhost:44385/WriteScheduleDay
            /// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
            /// It creates a BitScheduleConfiguration using the request (with DateRange covering that single day,
            /// and a TimeBlock from StartTime to EndTime), instantiates a BitSchedule,
            /// calls WriteDay to update the day, and returns the updated BitDay.
            /// </summary>
            app.MapPost("/WriteScheduleDay", (BitDayRequest request) =>
            {
                // Build a BitScheduleConfiguration from the BitDayRequest.
                var config = new BitScheduleConfiguration
                {
                    DateRange = new BitDateRange
                    {
                        StartDate = request.Date.Date,
                        EndDate = request.Date.Date  // Single day
                    },
                    // For a single day operation, ActiveDays is not required.
                    ActiveDays = null,
                    TimeBlock = BitDay.CreateRangeFromTimes(request.StartTime, request.EndTime)
                };

                // Create a BitSchedule using this configuration.
                BitSchedule schedule = new BitSchedule(config);

                // Call WriteDay to update (reserve) the specified time block on the given day.
                BitDay updatedDay = schedule.WriteDay(request);

                return Results.Ok(updatedDay);
            });

            /// <summary>
            /// POST https://localhost:44385/ReadScheduleDay
            /// Accepts a BitDayRequest JSON object (with Date, StartTime, EndTime).
            /// It creates a BitScheduleConfiguration using the request (with DateRange covering that single day,
            /// and a TimeBlock from StartTime to EndTime), instantiates a BitSchedule,
            /// calls ReadDay to retrieve the BitDay for the given date, and returns that BitDay.
            /// If the day is not present in the internal data, a new free BitDay is returned.
            /// </summary>
            app.MapPost("/ReadScheduleDay", (BitDayRequest request) =>
            {
                var config = new BitScheduleConfiguration
                {
                    DateRange = new BitDateRange
                    {
                        StartDate = request.Date.Date,
                        EndDate = request.Date.Date  // Single day
                    },
                    ActiveDays = null,
                    TimeBlock = BitDay.CreateRangeFromBlocks(0, BitDay.TotalSlots)
                };

                BitSchedule schedule = new BitSchedule(config);
                BitDay day = schedule.ReadDay(request.Date);
                return Results.Ok(day);
            });

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

            // POST endpoint to write/update a specific day's schedule.
            // The endpoint accepts a BitDayRequest in the request body.
            app.MapPost("/WriteDay", (BitDayRequest request, BitSchedule schedule) =>
            {
                // Call the WriteDay method on the BitSchedule instance.
                BitDay updatedDay = schedule.WriteDay(request);
                return Results.Ok(updatedDay);
            });

            // GET endpoint to read a specific day's schedule.
            // The date is passed as a query parameter.
            app.MapGet("/ReadDay", (DateTime date, BitSchedule schedule) =>
            {
                // Call the ReadDay method on the BitSchedule instance.
                BitDay day = schedule.ReadDay(date);
                return Results.Ok(day);
            });

            app.Run();
        }
    }
}




