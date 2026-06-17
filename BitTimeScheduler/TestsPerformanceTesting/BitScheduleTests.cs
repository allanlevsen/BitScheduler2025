using System.Diagnostics;
using BitSchedulerCore.Data.BitTimeScheduler.Data; // Required for BitScheduleDbContext
using BitSchedulerCore.Services; // Required for BitScheduleDataService
using Microsoft.EntityFrameworkCore; // Required for DbContextOptionsBuilder, UseNpgsql
using Microsoft.Extensions.Configuration; // Required for ConfigurationBuilder
using Microsoft.Extensions.Logging.Abstractions;
using BitSchedulerCore;
using BitSchedulerCore.Models; // Required for NullLogger

namespace BitTimeScheduler.TestsPerformanceTesting
{
    /// <summary>
    /// Integration tests for BitSchedule.
    /// IMPORTANT: These tests connect to the ACTUAL SQL Server database configured
    /// via the connection string. Ensure the database exists and is accessible.
    /// Tests might modify data in the database. Consider using a dedicated test database
    /// or implementing data cleanup strategies.
    /// </summary>
    public class BitScheduleTests
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor to load configuration (like connection string).
        /// </summary>
        public BitScheduleTests()
        {
            var configurationRoot =
                // Build configuration to read appsettings.json from the test project's output directory
                new ConfigurationBuilder()
                // SetBasePath is an extension method from Microsoft.Extensions.Configuration.FileExtensions
                .SetBasePath(Directory.GetCurrentDirectory()) // Assumes appsettings is in bin/Debug...
                // AddJsonFile is an extension method from Microsoft.Extensions.Configuration.Json
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                // Add User Secrets, Environment Variables, etc., as needed
                .Build();

            // Read the connection string
            _connectionString = configurationRoot.GetConnectionString("BitScheduleConnection")
                                ?? throw new InvalidOperationException("Could not find 'BitScheduleConnection' in configuration. Ensure appsettings.json exists in the test output directory and contains the connection string.");

            Console.WriteLine("--- Using Connection String (ensure this is your TEST database!) ---");
            // Avoid logging the full connection string in real scenarios due to secrets
            Console.WriteLine($"--- Connection String Hint: Starts with '{_connectionString.Split(';').FirstOrDefault()}' ---");
        }

        // --- Helper Methods for Test Setup ---

        /// <summary>
        /// Creates DbContext options configured to use PostgreSQL with the loaded connection string.
        /// </summary>
        private DbContextOptions<BitScheduleDbContext> CreatePostgreSqlDbContextOptions()
        {
            return new DbContextOptionsBuilder<BitScheduleDbContext>()
                .UseNpgsql(_connectionString)
                .Options;
        }

        /// <summary>
        /// Creates a functional BitSchedule instance with dependencies for integration testing.
        /// Uses a real DbContext connected to SQL Server and the real DataService.
        /// </summary>
        /// <param name="clientId">The client ID for the test.</param>
        /// <param name="config">The configuration for the BitSchedule instance.</param>
        /// <param name="dbContext">Output parameter for the created DbContext instance.</param>
        /// <returns>A configured BitSchedule instance.</returns>
        private BitSchedule CreateTestScheduleInstance(int clientId, BitScheduleConfiguration config, out BitScheduleDbContext dbContext)
        {
            var dbContextOptions = CreatePostgreSqlDbContextOptions();
            // Create a new DbContext instance for each test setup to ensure isolation
            dbContext = new BitScheduleDbContext(dbContextOptions);
            var logger = NullLogger<BitSchedule>.Instance; // Use NullLogger for tests
            var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter()); // Use the REAL DataService

            // Ensure configuration is valid before creating BitSchedule
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Configuration cannot be null for creating BitSchedule.");
            }
            if (config.DateRange == null)
            {
                config.DateRange = new BitDateRange { StartDate = DateTime.Today, EndDate = DateTime.Today.AddMonths(1) };
            }

            // Instantiate BitSchedule with real DbContext and DataService
            var schedule = new BitSchedule(clientId, config, dataService, dbContext, logger);
            return schedule;
        }

        private static BitScheduleRequest CreateRequest(BitScheduleConfiguration config)
        {
            return new BitScheduleRequest
            {
                BitResourceId = config.BitResourceId,
                DateRange = config.DateRange ?? new BitDateRange { StartDate = DateTime.Today, EndDate = DateTime.Today },
                ActiveDays = config.ActiveDays,
                TimeBlock = config.TimeBlock ?? BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9d), TimeSpan.FromHours(10d))
            };
        }

        // --- Test Methods ---

        /// <summary>
        /// Runs functional tests for BitSchedule against the actual database.
        /// Creates data, reads it, writes (reserves slots), reads again.
        /// Requires the database to be seeded or assumes data exists.
        /// </summary>
        public async Task RunFunctionalTests() // Changed to async Task
        {
            Console.WriteLine("=== BitSchedule Functional Tests (Integration) ===");

            int testClientId = 1; // Ensure this client exists in your test DB
            BitScheduleDbContext? dbContext = null; // To capture the context used

            try
            {
                // Configuration for the test scope
                var config = new BitScheduleConfiguration
                {
                    DateRange = new BitDateRange { StartDate = new DateTime(2025, 8, 1), EndDate = new DateTime(2025, 8, 31) },
                    ActiveDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                    TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9d), TimeSpan.FromHours(10d)),
                    AutoRefreshOnConfigurationChange = false
                };

                // Construct BitSchedule - this WILL load data from the SQL Server DB
                Console.WriteLine($"Creating BitSchedule instance for Client {testClientId} and DateRange {config.DateRange.StartDate:d} - {config.DateRange.EndDate:d}");
                var schedule = CreateTestScheduleInstance(testClientId, config, out dbContext);
                Console.WriteLine($"Initial data loaded. {schedule.LastRefreshed}");


                // Create a request to read and write schedule data.
                var request = CreateRequest(config);

                // Read the schedule using the request (reads from in-memory cache after constructor load).
                Console.WriteLine("Reading schedule before write...");
                var response = schedule.ReadSchedule(request);
                var totalDaysBefore = response.ScheduledMonths.Sum(m => m.Days.Count);

                // Calculate available slots based on the loaded data
                if (request.TimeBlock != null)
                {
                    var startBlock = request.TimeBlock.StartBlock;
                    var length = request.TimeBlock.EndBlock - startBlock + 1; // Assuming EndBlock is inclusive for range calculation
                    var availableSlotsBefore = response.ScheduledMonths
                        .SelectMany(m => m.Days)
                        .Count(d => d.IsRangeAvailable(startBlock, length));

                    Console.WriteLine($"Functional Test: ReadSchedule initially returned {totalDaysBefore} days in range. Available slots for TimeBlock: {availableSlotsBefore}");
                    if (totalDaysBefore == 0) Console.WriteLine("WARN: No data loaded. Ensure client/dates exist in the database or seeding ran.");

                    // Attempt to write the schedule (reserve the time block). This modifies DB.
                    Console.WriteLine("Attempting WriteScheduleAsync...");
                    var writeResult = await schedule.WriteScheduleAsync(request); // Use await and Async method
                    Console.WriteLine($"Functional Test: WriteScheduleAsync result = {writeResult}");

                    // Re-load data *from the database* to verify persistence
                    Console.WriteLine("Re-loading schedule data from DB after write...");
                    schedule.LoadScheduleData(); // Force reload from DB
                    var responseAfter = schedule.ReadSchedule(request); // Read again from memory
                    var totalDaysAfter = responseAfter.ScheduledMonths.Sum(m => m.Days.Count);
                    var availableSlotsAfter = responseAfter.ScheduledMonths
                        .SelectMany(m => m.Days)
                        .Count(d => d.IsRangeAvailable(startBlock, length));

                    Console.WriteLine($"Functional Test: After WriteScheduleAsync & Reload, ReadSchedule returned {totalDaysAfter} days. Available slots for TimeBlock: {availableSlotsAfter}");

                    // --- Basic Assertions ---
                    // Add proper assertions using your test framework (xUnit, MSTest, NUnit)
                    // Assert.Equal(totalDaysBefore, totalDaysAfter); // Day count shouldn't change
                    // Assert.True(writeResult); // Assuming the write should succeed
                    // Assert.True(availableSlotsAfter < availableSlotsBefore); // Slots should decrease if write succeeded and they were initially available
                    if (totalDaysBefore == totalDaysAfter) Console.WriteLine("PASS: Day count remained consistent.");
                    else Console.WriteLine($"FAIL: Day count changed ({totalDaysBefore} -> {totalDaysAfter}).");

                    if (writeResult && availableSlotsAfter < availableSlotsBefore) Console.WriteLine("PASS: Write succeeded and available slots decreased.");
                    else if (writeResult && availableSlotsAfter == availableSlotsBefore) Console.WriteLine("WARN: Write succeeded but available slots count unchanged (were slots already booked?).");
                    else if (!writeResult) Console.WriteLine("INFO: Write failed (result=false), possibly due to conflicts or missing data.");
                    else Console.WriteLine("FAIL: Unexpected state after write.");
                }
            }
            finally
            {
                // Dispose context if created within the test
                dbContext?.Dispose();
            }
            Console.WriteLine("=== End Functional Tests ===\n");
        }

        /// <summary>
        /// Tests the configuration change behavior of BitSchedule.
        /// This test still primarily checks in-memory flags (IsDirty) and timestamp (LastRefreshed)
        /// but uses a real DbContext connection setup.
        /// </summary>
        public void TestConfigurationChangeRefresh()
        {
            Console.WriteLine("=== Test Configuration Change Refresh (Integration Setup) ===");
            const int testClientId = 2;
            BitScheduleDbContext? dbContext = null;

            try
            {
                // Config A: AutoRefresh OFF
                var configA = new BitScheduleConfiguration
                {
                    DateRange = new BitDateRange { StartDate = new DateTime(2025, 8, 1), EndDate = new DateTime(2025, 8, 31) },
                    ActiveDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                    TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9d), TimeSpan.FromHours(10d)),
                    AutoRefreshOnConfigurationChange = false
                };
                var schedule = CreateTestScheduleInstance(testClientId, configA, out dbContext);
                var initialRefreshTime = schedule.LastRefreshed;
                schedule.IsDirty = false; // Start clean


                // --- Test 1: Assigning Identical Config (AutoRefresh=false) ---
                Console.WriteLine("\n--- Test 1: Assigning Identical Configuration (AutoRefresh=false) ---");
                var configB = new BitScheduleConfiguration // Identical
                {
                    DateRange = new BitDateRange { StartDate = new DateTime(2025, 8, 1), EndDate = new DateTime(2025, 8, 31) },
                    ActiveDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
                    TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9d), TimeSpan.FromHours(10d)),
                    AutoRefreshOnConfigurationChange = false
                };
                schedule.Configuration = configB;
                if (!schedule.IsDirty && schedule.LastRefreshed == initialRefreshTime) Console.WriteLine("PASS: Identical config (AutoRefresh=false) -> IsDirty=false, Refreshed=false.");
                else Console.WriteLine($"FAIL: Identical config (AutoRefresh=false) -> IsDirty={schedule.IsDirty}, Refreshed={schedule.LastRefreshed != initialRefreshTime}. Expected IsDirty=false, Refreshed=false.");


                // --- Test 2: Different TimeBlock (AutoRefresh=false) ---
                Console.WriteLine("\n--- Test 2: Different TimeBlock (AutoRefresh=false) ---");
                var configC = new BitScheduleConfiguration
                {
                    DateRange = configA.DateRange,
                    ActiveDays = configA.ActiveDays,
                    TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(10d), TimeSpan.FromHours(11d)), // Diff TimeBlock
                    AutoRefreshOnConfigurationChange = false
                };
                schedule.Configuration = configC;
                // TimeBlock doesn't affect data scope -> ConfigurationHasChanged = false
                if (!schedule.IsDirty && schedule.LastRefreshed == initialRefreshTime) Console.WriteLine("PASS: Different TimeBlock (AutoRefresh=false) -> IsDirty=false, Refreshed=false.");
                else Console.WriteLine($"FAIL: Different TimeBlock (AutoRefresh=false) -> IsDirty={schedule.IsDirty}, Refreshed={schedule.LastRefreshed != initialRefreshTime}. Expected IsDirty=false, Refreshed=false.");


                // --- Test 3: Different ActiveDays (AutoRefresh=false) ---
                Console.WriteLine("\n--- Test 3: Different ActiveDays (AutoRefresh=false) ---");
                var configD = new BitScheduleConfiguration
                {
                    DateRange = configC.DateRange,
                    TimeBlock = configC.TimeBlock,
                    ActiveDays = [DayOfWeek.Tuesday, DayOfWeek.Thursday], // Diff ActiveDays
                    AutoRefreshOnConfigurationChange = false
                };
                schedule.Configuration = configD;
                // ActiveDays affects data scope -> ConfigurationHasChanged = true -> IsDirty = true
                if (schedule.IsDirty && schedule.LastRefreshed == initialRefreshTime) Console.WriteLine("PASS: Different ActiveDays (AutoRefresh=false) -> IsDirty=true, Refreshed=false.");
                else Console.WriteLine($"FAIL: Different ActiveDays (AutoRefresh=false) -> IsDirty={schedule.IsDirty}, Refreshed={schedule.LastRefreshed != initialRefreshTime}. Expected IsDirty=true, Refreshed=false.");


                // --- Test 4: Different DateRange (AutoRefresh=true) ---
                Console.WriteLine("\n--- Test 4: Different DateRange (AutoRefresh=true) ---");
                schedule.IsDirty = false; // Reset for test
                var configE = new BitScheduleConfiguration
                {
                    ActiveDays = configD.ActiveDays,
                    TimeBlock = configD.TimeBlock,
                    DateRange = new BitDateRange { StartDate = new DateTime(2025, 9, 1), EndDate = new DateTime(2025, 9, 30) }, // Diff DateRange
                    AutoRefreshOnConfigurationChange = true // AutoRefresh ON
                };
                schedule.Configuration = configE;
                // DateRange affects scope, AutoRefresh is true -> Refresh occurs -> IsDirty=false, LastRefreshed changes
                if (!schedule.IsDirty && schedule.LastRefreshed != initialRefreshTime) Console.WriteLine("PASS: Different DateRange (AutoRefresh=true) -> IsDirty=false, Refreshed=true.");
                else Console.WriteLine($"FAIL: Different DateRange (AutoRefresh=true) -> IsDirty={schedule.IsDirty}, Refreshed={schedule.LastRefreshed != initialRefreshTime}. Expected IsDirty=false, Refreshed=true.");
                var refreshTimeAfterE = schedule.LastRefreshed;


                // --- Test 5: Modifying Current Config: ActiveDays (AutoRefresh=true) ---
                Console.WriteLine("\n--- Test 5: Modifying Current Config Property: ActiveDays (AutoRefresh=true) ---");
                schedule.Configuration.ActiveDays = [DayOfWeek.Saturday]; // Modify property
                                                                                            // PropertyChanged -> OnConfigurationChanged -> requiresReload=true -> AutoRefresh=true -> LoadData called
                if (!schedule.IsDirty && schedule.LastRefreshed != refreshTimeAfterE) Console.WriteLine("PASS: Modifying ActiveDays (AutoRefresh=true) -> IsDirty=false, Refreshed=true.");
                else Console.WriteLine($"FAIL: Modifying ActiveDays (AutoRefresh=true) -> IsDirty={schedule.IsDirty}, Refreshed={schedule.LastRefreshed != refreshTimeAfterE}. Expected IsDirty=false, Refreshed=true.");
                var refreshTimeAfterActiveDays = schedule.LastRefreshed;


                // --- Test 6: Modifying Current Config: TimeBlock (AutoRefresh=true) ---
                Console.WriteLine("\n--- Test 6: Modifying Current Config Property: TimeBlock (AutoRefresh=true) ---");
                schedule.Configuration.TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(14d), TimeSpan.FromHours(15d)); // Modify property
                                                                                                                                // PropertyChanged -> OnConfigurationChanged -> requiresReload=false -> LoadData NOT called
                                                                                                                                // IsDirty should be true because a property changed. LastRefreshed should NOT change.
                if (schedule.IsDirty && schedule.LastRefreshed == refreshTimeAfterActiveDays) Console.WriteLine("PASS: Modifying TimeBlock (AutoRefresh=true) -> IsDirty=true, Refreshed=false.");
                else Console.WriteLine($"FAIL: Modifying TimeBlock (AutoRefresh=true) -> IsDirty={schedule.IsDirty}, Refreshed={schedule.LastRefreshed == refreshTimeAfterActiveDays}. Expected IsDirty=true, Refreshed=false.");

            }
            finally
            {
                dbContext?.Dispose();
            }
            Console.WriteLine("=== End Test Configuration Change Refresh ===\n");
        }

        /// <summary>
        /// Runs performance tests for BitSchedule ReadSchedule (in-memory) and WriteScheduleAsync (DB interaction).
        /// Uses the actual database connection. Performance will depend heavily on DB latency.
        /// </summary>
        public async Task RunPerformanceTests() // Changed to async Task
        {
            Console.WriteLine("=== BitSchedule Performance Tests (Integration) ===");
            const int testClientId = 3; // Ensure this client exists

            // Test setup - load a decent amount of data
            var config = new BitScheduleConfiguration
            {
                DateRange = new BitDateRange { StartDate = new DateTime(2025, 1, 1), EndDate = new DateTime(2025, 12, 31) }, // One year
                ActiveDays = [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
                ],
                TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9d), TimeSpan.FromHours(10d)),
                AutoRefreshOnConfigurationChange = false
            };

            // Ensure data exists for this range/client - run seeding if necessary beforehand
            // Or accept that the first run might be slower if data isn't cached by SQL Server
            Console.WriteLine("Creating BitSchedule instance for performance test (loads data)...");
            var schedule = CreateTestScheduleInstance(testClientId, config, out var dbContext);
            Console.WriteLine("Data loaded for performance test.");

            var request = CreateRequest(config);

            const int iterationsRead = 50_000; // Adjust iterations based on expected performance
            const int iterationsWrite = 500;   // Write operations hitting DB are much slower
            Stopwatch sw = new Stopwatch();
            BitScheduleResponse? lastResp = null;
            bool lastWriteResult = false;

            Console.WriteLine($"--- Running {iterationsRead:N0} Read iterations ---");
            sw.Start();
            for (int i = 0; i < iterationsRead; i++)
            {
                lastResp = schedule.ReadSchedule(request); // Reads from memory
            }
            sw.Stop();
            Console.WriteLine(
                "Performance Test: ReadSchedule {0:N0} iterations took {1} ms ({2:N4} ms/op)",
                iterationsRead, sw.ElapsedMilliseconds, (double)sw.ElapsedMilliseconds / iterationsRead);
            GC.KeepAlive(lastResp);

            Console.WriteLine($"--- Running {iterationsWrite:N0} Write iterations ---");
            sw.Restart();
            for (int i = 0; i < iterationsWrite; i++)
            {
                // Note: Repeatedly writing the same block might have different performance
                // characteristics than writing different blocks due to DB caching/locking.
                // This also modifies the database state on each run.
                lastWriteResult = await schedule.WriteScheduleAsync(request); // Use await and Async
            }
            sw.Stop();
            Console.WriteLine(
               "Performance Test: WriteScheduleAsync {0:N0} iterations took {1} ms ({2:N2} ms/op)",
               iterationsWrite, sw.ElapsedMilliseconds, (double)sw.ElapsedMilliseconds / iterationsWrite);
            GC.KeepAlive(lastWriteResult);

            Console.WriteLine("NOTE: Write performance depends heavily on database latency and load.");
            Console.WriteLine("=== End Performance Tests ===\n");

            // Clean up context if needed (though using 'out' parameter means caller might manage it)
            dbContext.Dispose(); // Dispose if created here explicitly
        }
    }
}