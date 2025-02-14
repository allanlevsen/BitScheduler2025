
using BitTimeScheduler.Models;
using BitTimeScheduler;
using BitScheduleApi.Utility;

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

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            //builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI();
            //}

            app.UseHttpsRedirection();
            app.UseAuthorization();


            app.MapGet("/", (HttpContext httpContext) =>
            {
                return "BitScheduler Api is running...";
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
                        StartDate = new DateTime(2025, 8, 1),
                        EndDate = new DateTime(2025, 8, 31)
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


            app.Run();
        }
    }
}
