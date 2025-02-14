using BitTimeScheduler.Models;
using System.Diagnostics;

namespace BitTimeScheduler.TestsPerformanceTesting
{
    public class BitDayUtilityTests
    {
        /// <summary>
        /// Runs a series of correctness tests for the BitDay utility methods:
        /// - TimeToBlockIndex, BlockIndexToTime,
        /// - CreateRangeFromBlocks, and CreateRangeFromTimes.
        /// </summary>
        public void RunUtilityMethodsTests()
        {
            Console.WriteLine("Running Utility Methods Tests...");

            // Test 1: TimeToBlockIndex
            TimeSpan t9 = TimeSpan.FromHours(9); // 9:00 AM
            int blockFor9 = BitDay.TimeToBlockIndex(t9);  // Expected: 9 * 60 / 15 = 36
            if (blockFor9 != 36)
                Console.WriteLine($"Error: TimeToBlockIndex(9:00) returned {blockFor9}, expected 36");
            else
                Console.WriteLine("TimeToBlockIndex(9:00) passed.");

            // Test 2: BlockIndexToTime
            TimeSpan timeFromBlock36 = BitDay.BlockIndexToTime(36);
            if (timeFromBlock36 != TimeSpan.FromHours(9))
                Console.WriteLine($"Error: BlockIndexToTime(36) returned {timeFromBlock36}, expected 09:00:00");
            else
                Console.WriteLine("BlockIndexToTime(36) passed.");

            // Test 3: CreateRangeFromBlocks
            // For blocks 36 to 39, we expect a range from 9:00 to 10:00.
            BitTimeRange rangeFromBlocks = BitDay.CreateRangeFromBlocks(36, 39);
            if (rangeFromBlocks.StartTime != TimeSpan.FromHours(9) ||
                rangeFromBlocks.EndTime != TimeSpan.FromHours(10))
            {
                Console.WriteLine($"Error: CreateRangeFromBlocks(36,39) returned {rangeFromBlocks}, expected 09:00 to 10:00");
            }
            else
                Console.WriteLine("CreateRangeFromBlocks(36,39) passed.");

            // Test 4: CreateRangeFromTimes for an exact 15-minute-boundary range.
            // For times 9:00 to 10:00, we expect startBlock 36 and endBlock 39.
            BitTimeRange rangeFromTimes1 = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10));
            if (rangeFromTimes1.StartBlock != 36 || rangeFromTimes1.EndBlock != 39)
            {
                Console.WriteLine($"Error: CreateRangeFromTimes(09:00,10:00) returned {rangeFromTimes1}, expected blocks 36 to 39");
            }
            else
                Console.WriteLine("CreateRangeFromTimes(09:00,10:00) passed.");

            // Test 5: CreateRangeFromTimes for a non-zero start offset.
            // For times 9:15 to 10:15, we expect startBlock = 37 (since 9:15 -> 555 minutes / 15 = 37) 
            // and endBlock = 40 (because 10:15 -> 615/15 = 41, minus one equals 40).
            BitTimeRange rangeFromTimes2 = BitDay.CreateRangeFromTimes(TimeSpan.FromMinutes(555), TimeSpan.FromMinutes(615));
            if (rangeFromTimes2.StartBlock != 37 || rangeFromTimes2.EndBlock != 40)
            {
                Console.WriteLine($"Error: CreateRangeFromTimes(9:15,10:15) returned {rangeFromTimes2}, expected blocks 37 to 40");
            }
            else
                Console.WriteLine("CreateRangeFromTimes(9:15,10:15) passed.");

            Console.WriteLine("Utility Methods Tests Completed.\n");
        }

        /// <summary>
        /// Runs performance tests for the BitDay utility methods by calling each method 1,000,000 times.
        /// </summary>
        public void RunUtilityMethodsPerformanceTests()
        {
            Console.WriteLine("Running Utility Methods Performance Tests...");
            const int iterations = 1_000_000;
            Stopwatch sw = new Stopwatch();

            // Performance test for TimeToBlockIndex
            sw.Start();
            for (int i = 0; i < iterations; i++)
            {
                int b = BitDay.TimeToBlockIndex(TimeSpan.FromHours(9));
            }
            sw.Stop();
            Console.WriteLine($"TimeToBlockIndex: {iterations:N0} iterations took {sw.ElapsedMilliseconds} ms");

            // Performance test for BlockIndexToTime
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                TimeSpan t = BitDay.BlockIndexToTime(36);
            }
            sw.Stop();
            Console.WriteLine($"BlockIndexToTime: {iterations:N0} iterations took {sw.ElapsedMilliseconds} ms");

            // Performance test for CreateRangeFromBlocks
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                BitTimeRange r = BitDay.CreateRangeFromBlocks(36, 39);
            }
            sw.Stop();
            Console.WriteLine($"CreateRangeFromBlocks: {iterations:N0} iterations took {sw.ElapsedMilliseconds} ms");

            // Performance test for CreateRangeFromTimes
            sw.Restart();
            for (int i = 0; i < iterations; i++)
            {
                BitTimeRange r = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10));
            }
            sw.Stop();
            Console.WriteLine($"CreateRangeFromTimes: {iterations:N0} iterations took {sw.ElapsedMilliseconds} ms");

            Console.WriteLine("Utility Methods Performance Tests Completed.\n");
        }

        public void TestCreateRangeFromBlocks()
        {
            Console.WriteLine("=== Testing CreateRangeFromBlocks ===");

            // Define test cases as tuples:
            // (startBlock, endBlock, expectedStartTime, expectedEndTime)
            // Note: BlockIndexToTime(blockIndex) returns TimeSpan.FromMinutes(blockIndex * 15)
            var testCases = new[]
            {
        (startBlock: 0, endBlock: 0, expectedStart: TimeSpan.FromMinutes(0), expectedEnd: TimeSpan.FromMinutes(15)),
        (startBlock: 0, endBlock: 1, expectedStart: TimeSpan.FromMinutes(0), expectedEnd: TimeSpan.FromMinutes(30)),
        (startBlock: 10, endBlock: 10, expectedStart: TimeSpan.FromMinutes(10 * 15), expectedEnd: TimeSpan.FromMinutes(11 * 15)),
        (startBlock: 95, endBlock: 95, expectedStart: TimeSpan.FromMinutes(95 * 15), expectedEnd: TimeSpan.FromMinutes(96 * 15))
    };

            foreach (var test in testCases)
            {
                try
                {
                    BitTimeRange range = BitDay.CreateRangeFromBlocks(test.startBlock, test.endBlock);
                    if (range.StartTime == test.expectedStart && range.EndTime == test.expectedEnd)
                    {
                        Console.WriteLine($"PASS: CreateRangeFromBlocks({test.startBlock}, {test.endBlock}) returned {range.StartTime} to {range.EndTime}");
                    }
                    else
                    {
                        Console.WriteLine($"FAIL: CreateRangeFromBlocks({test.startBlock}, {test.endBlock}) returned {range.StartTime} to {range.EndTime} (expected {test.expectedStart} to {test.expectedEnd})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FAIL: CreateRangeFromBlocks({test.startBlock}, {test.endBlock}) threw an exception: {ex.Message}");
                }
            }

            // Test invalid case: startBlock > endBlock. For example, (5, 3) should throw an exception.
            try
            {
                var invalidRange = BitDay.CreateRangeFromBlocks(5, 3);
                Console.WriteLine("FAIL: Expected exception for CreateRangeFromBlocks(5, 3), but no exception was thrown.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("PASS: CreateRangeFromBlocks(5, 3) correctly threw an exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: CreateRangeFromBlocks(5, 3) threw the wrong type of exception: " + ex.Message);
            }

            // Test invalid case: startBlock > endBlock. For example, (-1, 3) should throw an exception.
            try
            {
                var invalidRange = BitDay.CreateRangeFromBlocks(-1, 3);
                Console.WriteLine("FAIL: Expected exception for CreateRangeFromBlocks(-1, 3), but no exception was thrown.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("PASS: CreateRangeFromBlocks(-1, 3) correctly threw an exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: CreateRangeFromBlocks(-1, 3) threw the wrong type of exception: " + ex.Message);
            }

            // Test invalid case: startBlock > endBlock. For example, (5, 96) should throw an exception.
            try
            {
                var invalidRange = BitDay.CreateRangeFromBlocks(5, 96);
                Console.WriteLine("FAIL: Expected exception for CreateRangeFromBlocks(5, 96), but no exception was thrown.");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("PASS: CreateRangeFromBlocks(5, 96) correctly threw an exception: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: CreateRangeFromBlocks(5, 96) threw the wrong type of exception: " + ex.Message);
            }

            Console.WriteLine("=== End Testing CreateRangeFromBlocks ===\n");
        }
    }

}
