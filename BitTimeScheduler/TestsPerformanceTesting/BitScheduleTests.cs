using System;
using System.Collections.Generic;

namespace BitTimeScheduler.TestsPerformanceTesting
{
    using System;
    using System.Diagnostics;
    using BitTimeScheduler.Models;

    public class BitScheduleTests
    {
        /// <summary>
        /// Runs functional tests for BitSchedule.
        /// Creates a BitScheduleConfiguration and uses it to construct a BitSchedule.
        /// Then, a BitScheduleRequest (which may match the configuration) is used to read and write
        /// the internal schedule data.
        /// </summary>
        public void RunFunctionalTests()
        {
            Console.WriteLine("=== BitSchedule Functional Tests ===");

            // Create a configuration that describes the internal schedule data.
            var config = new BitScheduleConfiguration
            {
                DateRange = new BitDateRange
                {
                    StartDate = new DateTime(2025, 8, 1),
                    EndDate = new DateTime(2025, 8, 31)
                },
                ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
            };

            // Construct a BitSchedule using the configuration.
            BitSchedule schedule = new BitSchedule(config);

            // Create a request to read and write schedule data.
            // (In this example, we use the same values as the configuration.)
            var request = new BitScheduleRequest
            {
                DateRange = new BitDateRange
                {
                    StartDate = config.DateRange.StartDate,
                    EndDate = config.DateRange.EndDate
                },
                ActiveDays = config.ActiveDays,
                TimeBlock = config.TimeBlock
            };

            // Read the schedule using the request.
            BitScheduleResponse response = schedule.ReadSchedule(request);
            Console.WriteLine("Functional Test: ReadSchedule returned {0} days", response.ScheduledDays.Count);

            // Attempt to write the schedule (i.e. reserve the time block on all matching days).
            bool writeResult = schedule.WriteSchedule(request);
            Console.WriteLine("Functional Test: WriteSchedule result = {0}", writeResult);

            // Re-read the schedule after writing.
            BitScheduleResponse responseAfter = schedule.ReadSchedule(request);
            Console.WriteLine("Functional Test: After WriteSchedule, ReadSchedule returned {0} days", responseAfter.ScheduledDays.Count);

            Console.WriteLine("=== End Functional Tests ===\n");
        }

        /// <summary>
        /// Runs performance tests for BitSchedule by calling ReadSchedule and WriteSchedule repeatedly.
        /// </summary>
        public void RunPerformanceTests()
        {
            Console.WriteLine("=== BitSchedule Performance Tests ===");

            // Create a configuration.
            var config = new BitScheduleConfiguration
            {
                DateRange = new BitDateRange
                {
                    StartDate = new DateTime(2025, 8, 1),
                    EndDate = new DateTime(2025, 8, 31)
                },
                ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
            };

            // Construct a BitSchedule with the configuration.
            BitSchedule schedule = new BitSchedule(config);

            // Create a BitScheduleRequest matching the configuration.
            var request = new BitScheduleRequest
            {
                DateRange = new BitDateRange
                {
                    StartDate = config.DateRange.StartDate,
                    EndDate = config.DateRange.EndDate
                },
                ActiveDays = config.ActiveDays,
                TimeBlock = config.TimeBlock
            };

            const int iterations = 100_000;
            Stopwatch sw = new Stopwatch();

            // Performance test for ReadSchedule.
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                BitScheduleResponse resp = schedule.ReadSchedule(request);
            }
            sw.Stop();
            Console.WriteLine("Performance Test: ReadSchedule {0:N0} iterations took {1} ms", iterations, sw.ElapsedMilliseconds);

            // Performance test for WriteSchedule.
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                bool result = schedule.WriteSchedule(request);
            }
            sw.Stop();
            Console.WriteLine("Performance Test: WriteSchedule {0:N0} iterations took {1} ms", iterations, sw.ElapsedMilliseconds);

            Console.WriteLine("=== End Performance Tests ===\n");
        }

    }

}
