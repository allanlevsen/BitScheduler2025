using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTimeScheduler.Models;

namespace BitTimeScheduler.TestsPerformanceTesting
{

    public class BitDayMonthTest
    {
        /// <summary>
        /// Tests the correctness of day slot operations and metadata flag operations.
        /// </summary>
        public void RunBitDayCorrectnessTests()
        {
            Console.WriteLine("=== Correctness Tests ===");

            // Create an instance.
            BitDay bt = new BitDay();

            // --- Test Day Slot Operations ---
            // Initially, all day slots should be available.
            if (!bt.IsRangeAvailable(0, BitDay.TotalSlots))
                Console.WriteLine("Error: Full day should be available initially.");

            // Reserve a range (slots 10–14) and verify it becomes unavailable.
            bool reserved = bt.ReserveRange(10, 5);
            if (!reserved)
                Console.WriteLine("Error: ReserveRange(10,5) should succeed.");
            if (bt.IsRangeAvailable(10, 5))
                Console.WriteLine("Error: Reserved range should not be available.");
            // Free the reserved range.
            bt.FreeRange(10, 5);
            if (!bt.IsRangeAvailable(10, 5))
                Console.WriteLine("Error: Freed range should be available.");

            // --- Test Metadata Flag Operations ---
            // Using the IsFree property.
            bt.IsFree = true;
            if (!bt.IsFree)
                Console.WriteLine("Error: IsFree flag should be true after setting via property.");
            bt.IsFree = false;
            if (bt.IsFree)
                Console.WriteLine("Error: IsFree flag should be false after clearing via property.");

            // Using the enum–based methods.
            bt.SetMetadataFlag(BitTimeMetadataFlags.IsFree, true);
            if (!bt.GetMetadataFlag(BitTimeMetadataFlags.IsFree))
                Console.WriteLine("Error: Enum method: IsFree flag should be true after setting.");
            bt.SetMetadataFlag(BitTimeMetadataFlags.IsFree, false);
            if (bt.GetMetadataFlag(BitTimeMetadataFlags.IsFree))
                Console.WriteLine("Error: Enum method: IsFree flag should be false after clearing.");

            Console.WriteLine("Correctness tests passed.\n");
        }

        /// <summary>
        /// Runs performance tests for day slot operations and metadata operations.
        /// </summary>
        public void RunBitDayPerformanceTests()
        {
            const int iterations = 1_000_000;
            Console.WriteLine("=== Performance Tests ===");
            Console.WriteLine($"Performing {iterations:N0} iterations for day slot operations...");

            BitDay bt = new BitDay();
            Stopwatch swDay = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                int startSlot = i % (BitDay.TotalSlots - 4);
                int length = 4;
                // Toggle: if the range is available, reserve it; otherwise, free it.
                if (bt.IsRangeAvailable(startSlot, length))
                    bt.ReserveRange(startSlot, length);
                else
                    bt.FreeRange(startSlot, length);
            }
            swDay.Stop();
            Console.WriteLine($"Day slot operations elapsed time: {swDay.ElapsedMilliseconds} ms");

            Console.WriteLine($"Performing {iterations:N0} iterations for metadata operations...");

            Stopwatch swMeta = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                // Toggle the IsFree flag: true on even iterations, false on odd.
                bool expected = i % 2 == 0;
                bt.SetMetadataFlag(BitTimeMetadataFlags.IsFree, expected);
                bool actual = bt.GetMetadataFlag(BitTimeMetadataFlags.IsFree);
                if (actual != expected)
                    Console.WriteLine("Error: Mismatch in metadata flag value.");
            }
            swMeta.Stop();
            Console.WriteLine($"Metadata operations elapsed time: {swMeta.ElapsedMilliseconds} ms");
        }


        /// <summary>
        /// Test Scenario 1: When the BitSearchCriteria.Days array is empty,
        /// the method returns every day in the month where the specified time block is available.
        /// </summary>
        public void RunEmptyWeekdayAvailabilityTest()
        {
            Console.WriteLine("=== Empty Weekday Availability Test ===");

            // Create a BitMonth for a fixed month (August 2025)
            BitMonth bm = new BitMonth(2025, 8);
            int daysInMonth = DateTime.DaysInMonth(2025, 8);

            // Define a search time block – for example, 10:00 AM to 11:00 AM.
            TimeSpan searchStart = new TimeSpan(10, 0, 0);
            TimeSpan searchEnd = new TimeSpan(11, 0, 0);

            BitSearchCriteria criteria = new BitSearchCriteria
            {
                StartTime = searchStart,
                EndTime = searchEnd,
                Days = new DayOfWeek[] { }  // Empty weekday list.
            };

            // Scenario A: No reservations made.
            var availableBefore = bm.GetAvailableDays(criteria);
            Console.WriteLine($"[No Reservations] Available days count (expected: {daysInMonth}): {availableBefore.Count}");

            // Scenario B: Reserve the time block on one day (say, day 15).
            int startSlot = (int)(searchStart.TotalMinutes / 15);
            int length = (int)((searchEnd - searchStart).TotalMinutes / 15);
            bm[15].ReserveRange(startSlot, length);
            var availableAfter = bm.GetAvailableDays(criteria);
            Console.WriteLine($"[After Reserving Day 15] Available days count (expected: {daysInMonth - 1}): {availableAfter.Count}");

            if (availableAfter.Any(day => day.Date.Day == 15))
                Console.WriteLine("Error: Day 15 should not be available.");
            else
                Console.WriteLine("Day 15 is correctly not available.");

            Console.WriteLine("Empty Weekday Availability Test completed.\n");
        }

        /// <summary>
        /// Test Scenario 2: When a couple of weekdays (e.g., Tuesday and Thursday) are specified,
        /// the search returns only days from weeks where the entire time block is available on ALL required weekdays.
        /// </summary>
        public void RunMultipleWeekdayAvailabilityTest()
        {
            Console.WriteLine("=== Multiple Weekday Availability Test ===");

            // Create a BitMonth for a fixed month (August 2025)
            BitMonth bm = new BitMonth(2025, 8);

            // Define a search time block – for example, 9:00 AM to 11:00 AM.
            TimeSpan searchStart = new TimeSpan(9, 0, 0);
            TimeSpan searchEnd = new TimeSpan(11, 0, 0);

            // Specify two weekdays, e.g., Tuesday and Thursday.
            BitSearchCriteria criteria = new BitSearchCriteria
            {
                StartTime = searchStart,
                EndTime = searchEnd,
                Days = new DayOfWeek[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }
            };

            // Initially, with no reservations, list available days.
            var availableInitial = bm.GetAvailableDays(criteria);
            Console.WriteLine("Initial available days for Tuesday and Thursday with 9:00-11:00 block:");
            foreach (var day in availableInitial)
            {
                Console.WriteLine($"  {day.Date.ToShortDateString()} - {day.Date.DayOfWeek}");
            }

            // Now, simulate a reservation in one week.
            // Find the first Tuesday in the month and reserve the search time block.
            int startSlot = (int)(searchStart.TotalMinutes / 15);
            int length = (int)((searchEnd - searchStart).TotalMinutes / 15);
            BitDay reservedTuesday = null;
            foreach (int d in Enumerable.Range(1, DateTime.DaysInMonth(2025, 8)))
            {
                if (bm[d].Date.DayOfWeek == DayOfWeek.Tuesday)
                {
                    reservedTuesday = bm[d];
                    reservedTuesday.ReserveRange(startSlot, length);
                    Console.WriteLine($"Reserved time block on Tuesday, {reservedTuesday.Date.ToShortDateString()}");
                    break;
                }
            }

            var availableAfter = bm.GetAvailableDays(criteria);
            Console.WriteLine("Available days after reserving one Tuesday:");
            foreach (var day in availableAfter)
            {
                Console.WriteLine($"  {day.Date.ToShortDateString()} - {day.Date.DayOfWeek}");
            }

            // Validate: The week that contains the reserved Tuesday should not appear in the results.
            DateTime weekKey = reservedTuesday.Date.AddDays(-(int)reservedTuesday.Date.DayOfWeek);
            bool weekExists = availableAfter.Any(day => day.Date.AddDays(-(int)day.Date.DayOfWeek) == weekKey);
            if (weekExists)
                Console.WriteLine("Error: The week containing the reserved Tuesday should not be in the available days list.");
            else
                Console.WriteLine("The week with the reserved Tuesday is correctly excluded.");

            Console.WriteLine("Multiple Weekday Availability Test completed.\n");
        }

        /// <summary>
        /// Performance Test for Empty Weekday Scenario:
        /// Calls GetAvailableDays (with an empty Days array) 10,000 times.
        /// </summary>
        public void RunEmptyWeekdayAvailabilityPerformanceTest()
        {
            Console.WriteLine("=== Empty Weekday Availability Performance Test ===");

            const int iterations = 100_000;
            BitMonth bm = new BitMonth(2025, 8);
            // Use a time block, for example, 10:00 AM to 11:00 AM.
            BitSearchCriteria criteria = new BitSearchCriteria
            {
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Days = new DayOfWeek[] { }  // Empty weekday list.
            };

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var available = bm.GetAvailableDays(criteria);
            }
            sw.Stop();
            Console.WriteLine($"Empty Weekday Performance Test: {iterations:N0} iterations took {sw.ElapsedMilliseconds} ms\n");
        }

        /// <summary>
        /// Performance Test for Multiple Weekday Scenario:
        /// Calls GetAvailableDays (with multiple weekdays specified) 10,000 times.
        /// </summary>
        public void RunMultipleWeekdayAvailabilityPerformanceTest()
        {
            Console.WriteLine("=== Multiple Weekday Availability Performance Test ===");

            const int iterations = 100_000;
            BitMonth bm = new BitMonth(2025, 8);
            // Use a time block, for example, 9:00 AM to 11:00 AM.
            BitSearchCriteria criteria = new BitSearchCriteria
            {
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Days = new DayOfWeek[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }
            };

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var available = bm.GetAvailableDays(criteria);
            }
            sw.Stop();
            Console.WriteLine($"Multiple Weekday Performance Test: {iterations:N0} iterations took {sw.ElapsedMilliseconds} ms\n");
        }
    }

}
