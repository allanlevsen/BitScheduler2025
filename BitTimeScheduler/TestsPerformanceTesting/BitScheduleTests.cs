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
                ActiveDays = new DayOfWeek[]
                {
                DayOfWeek.Monday,
                DayOfWeek.Wednesday,
                DayOfWeek.Friday
                },
                TimeBlock = BitDay.CreateRangeFromTimes(
                    TimeSpan.FromHours(9),
                    TimeSpan.FromHours(10)
                )
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

            // Aggregate the total number of days across all returned months.
            int totalDays = response.ScheduledMonths.Sum(m => m.Days.Count);
            Console.WriteLine("Functional Test: ReadSchedule returned {0} days", totalDays);

            // Attempt to write the schedule (i.e. reserve the time block on all matching days).
            bool writeResult = schedule.WriteSchedule(request);
            Console.WriteLine("Functional Test: WriteSchedule result = {0}", writeResult);

            // Re-read the schedule after writing.
            BitScheduleResponse responseAfter = schedule.ReadSchedule(request);
            int totalDaysAfter = responseAfter.ScheduledMonths.Sum(m => m.Days.Count);
            Console.WriteLine("Functional Test: After WriteSchedule, ReadSchedule returned {0} days", totalDaysAfter);

            Console.WriteLine("=== End Functional Tests ===\n");
        }

        /// <summary>
        /// Tests the configuration change behavior of BitSchedule.
        /// When setting the Configuration property:
        /// - If the new configuration is identical, no refresh should occur.
        /// - If any property has changed, a refresh should occur.
        /// This test uses reflection to check the internal schedule data reference.
        /// </summary>
        public void TestConfigurationChangeRefresh()
        {
            Console.WriteLine("=== Test Configuration Change Refresh ===");

            // Create initial configuration (configA).
            var configA = new BitScheduleConfiguration
            {
                DateRange = new BitDateRange
                {
                    StartDate = new DateTime(2025, 8, 1),
                    EndDate = new DateTime(2025, 8, 31)
                },
                ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
            };

            // Instantiate BitSchedule with configuration A.
            BitSchedule schedule = new BitSchedule(configA);

            // Create a new configuration (configB) identical to configA.
            var configB = new BitScheduleConfiguration
            {
                DateRange = new BitDateRange
                {
                    StartDate = new DateTime(2025, 8, 1),
                    EndDate = new DateTime(2025, 8, 31)
                },
                ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
                TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
            };

            // Set the configuration to configB.
            schedule.Configuration = configB;

            if (!schedule.IsDirty)
                Console.WriteLine("PASS: Identical configuration did not refresh schedule data.");
            else
                Console.WriteLine("FAIL: Identical configuration unexpectedly refreshed schedule data.");

            // Create a new configuration (configC) that differs in one property (change TimeBlock EndTime).
            var configC = new BitScheduleConfiguration
            {
                DateRange = new BitDateRange
                {
                    StartDate = new DateTime(2025, 8, 1),
                    EndDate = new DateTime(2025, 8, 31)
                },
                ActiveDays = new DayOfWeek[] { DayOfWeek.Wednesday },
                TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
            };

            // Set the configuration to configC.
            schedule.Configuration = configC;

            if (schedule.IsDirty)
                Console.WriteLine("PASS: Different configuration.");
            else
                Console.WriteLine("FAIL: Different configuration but not flagged as dirty.");

            // Create a new configuration (configC) that differs in one property (change TimeBlock EndTime).
            long rl = schedule.LastRefreshed.Millisecond;
            Console.WriteLine($"Last Refreshed {rl}");

            var configD = new BitScheduleConfiguration
            {
                DateRange = new BitDateRange
                {
                    StartDate = new DateTime(2025, 8, 1),
                    EndDate = new DateTime(2025, 8, 31)
                },
                ActiveDays = new DayOfWeek[] { DayOfWeek.Wednesday, DayOfWeek.Thursday },
                TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
                AutoRefreshOnConfigurationChange = true
            };
            schedule.Configuration = configD;

            long rr = schedule.LastRefreshed.Millisecond;
            Console.WriteLine($"Last Refreshed {rr}");

            if (rl != rr)
                Console.WriteLine("PASS: Different data as config change should refresh data...\n");
            else
                Console.WriteLine("FAIL: Different configuration but data was not refreshed...\n");
            
            if (!schedule.IsDirty)
                Console.WriteLine("PASS: Different configuration causes a refresh which resets the dirty flag.\n");
            else
                Console.WriteLine("FAIL: Different configuration and a refresh should have happened - but dirty flag is still true\n");

            Console.WriteLine("\n=== Testing the OnConfiguration Change...\n");
            Console.WriteLine("Changing the ActiveDays - with the autorefresh on - data should change...");
            rl = schedule.LastRefreshed.Millisecond;
            schedule.Configuration.ActiveDays = new DayOfWeek[] { DayOfWeek.Monday, DayOfWeek.Thursday };
            rr = schedule.LastRefreshed.Millisecond;

            if (rl != rr)
                Console.WriteLine("PASS: Data was reloaded after Active days changed...\n");
            else
                Console.WriteLine("FAIL: Data was NOT reloaded after Active days changed...\n");


            schedule.Configuration.AutoRefreshOnConfigurationChange = false;
            Console.WriteLine("Changing the ActiveDays - with the autorefresh off - data should NOT change...");
            rl = schedule.LastRefreshed.Millisecond;
            schedule.Configuration.ActiveDays = new DayOfWeek[] { DayOfWeek.Tuesday, DayOfWeek.Friday };
            rr = schedule.LastRefreshed.Millisecond;

            if (rl == rr)
                Console.WriteLine("PASS: Data was NOT reloaded after Active days changed - as expected...\n");
            else
                Console.WriteLine("FAIL: Data was reloaded after Active days changed - should not reload data...\n");


            Console.WriteLine("=== End Test Configuration Change Refresh ===\n");

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
                ActiveDays = new DayOfWeek[]
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Friday
                },
                TimeBlock = BitDay.CreateRangeFromTimes(
                    TimeSpan.FromHours(9),
                    TimeSpan.FromHours(10)
                )
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
            Console.WriteLine(
                "Performance Test: ReadSchedule {0:N0} iterations took {1} ms", 
                iterations, 
                sw.ElapsedMilliseconds
            );

            // Performance test for WriteSchedule.
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                bool result = schedule.WriteSchedule(request);
            }
            sw.Stop();
            Console.WriteLine(
                "Performance Test: WriteSchedule {0:N0} iterations took {1} ms", 
                iterations, 
                sw.ElapsedMilliseconds
            );

            Console.WriteLine("=== End Performance Tests ===\n");
        }

    }

}
