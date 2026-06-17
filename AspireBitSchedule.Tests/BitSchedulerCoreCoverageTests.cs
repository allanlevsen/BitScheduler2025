using BitSchedulerCore;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace AspireBitSchedule.Tests;

public class BitSchedulerCoreCoverageTests : IAsyncLifetime
{
    private readonly string _adminConnectionString;
    private readonly string _testConnectionString;
    private readonly string _testDatabaseName;

    public BitSchedulerCoreCoverageTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .Build();

        var baseConnectionString = configuration.GetConnectionString("BitScheduleTestConnection")
            ?? throw new InvalidOperationException("Could not find 'BitScheduleTestConnection' in appsettings.Test.json.");

        var baseConnectionBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Pooling = false
        };

        var baseDatabaseName = string.IsNullOrWhiteSpace(baseConnectionBuilder.Database)
            ? "bitscheduler_test"
            : baseConnectionBuilder.Database;

        _testDatabaseName = $"{baseDatabaseName}_{Guid.NewGuid():N}";

        if (_testDatabaseName.Length > 63)
        {
            _testDatabaseName = _testDatabaseName[..63];
        }

        baseConnectionBuilder.Database = _testDatabaseName;
        _testConnectionString = baseConnectionBuilder.ConnectionString;

        var adminConnectionBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        _adminConnectionString = adminConnectionBuilder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        await CreateDatabaseAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await DropDatabaseAsync();
    }

    [Fact]
    public void BitDay_DefaultAndDateConstructors_InitializeExpectedState()
    {
        var explicitDate = new DateTime(2025, 4, 12, 13, 45, 0);

        var defaultDay = new BitDay();
        var explicitDay = new BitDay(explicitDate);

        Assert.Equal(DateTime.Today, defaultDay.Date);
        Assert.Equal(explicitDate.Date, explicitDay.Date);
        Assert.True(explicitDay.IsFree);
        Assert.Equal((UInt128)0, explicitDay.DayData);
        Assert.Equal((uint)BitTimeMetadataFlags.IsFree, explicitDay.Metadata);
        Assert.NotNull(explicitDay.Reservations);
        Assert.Empty(explicitDay.Reservations);
    }

    [Fact]
    public void BitDay_BitsAndMetadataOperations_WorkAsExpected()
    {
        var day = new BitDay(new DateTime(2025, 4, 12))
        {
            BitDayId = 42,
            ClientId = 7
        };

        day.DayData = ((UInt128)0x00000000FEDCBA98 << 64) | 0x0123456789ABCDEF;
        day.Metadata = 0x76543210;

        Assert.Equal(((UInt128)0x00000000FEDCBA98 << 64) | 0x0123456789ABCDEF, day.DayData);
        Assert.Equal(0x76543210U, day.Metadata);

        day.SetMetadataFlag(BitTimeMetadataFlags.IsFree, false);
        Assert.False(day.GetMetadataFlag(BitTimeMetadataFlags.IsFree));

        day.IsFree = true;

        Assert.Equal(42, day.BitDayId);
        Assert.Equal(7, day.ClientId);
        Assert.Equal(((UInt128)0x00000000FEDCBA98 << 64) | 0x0123456789ABCDEF, day.DayData);
        Assert.True(day.GetMetadataFlag(BitTimeMetadataFlags.IsFree));
        Assert.Contains(day.Date.ToShortDateString(), day.ToString());
    }

    [Fact]
    public void BitDay_RangeOperationsAndConversions_CoverAvailableBranches()
    {
        var day = new BitDay(new DateTime(2025, 4, 12));

        Assert.True(day.IsRangeAvailable(0, 4));
        Assert.True(day.ReserveRange(0, 4));
        Assert.False(day.IsFree);
        Assert.False(day.IsRangeAvailable(0, 4));
        Assert.False(day.ReserveRange(0, 4));

        day.FreeRange(0, 4);
        Assert.True(day.IsFree);
        Assert.True(day.IsRangeAvailable(0, 4));

        Assert.Throws<ArgumentOutOfRangeException>(() => day.IsRangeAvailable(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => day.ReserveRange(95, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => day.FreeRange(0, 0));

        Assert.Equal(36, BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)));
        Assert.Equal(TimeSpan.FromHours(9), BitDay.BlockIndexToTime(36));

        var rangeFromBlocks = BitDay.CreateRangeFromBlocks(36, 39);
        Assert.Equal(36, rangeFromBlocks.StartBlock);
        Assert.Equal(39, rangeFromBlocks.EndBlock);
        Assert.Equal(TimeSpan.FromHours(9), rangeFromBlocks.StartTime);
        Assert.Equal(TimeSpan.FromHours(10), rangeFromBlocks.EndTime);
        Assert.Contains("Blocks: 36 to 39", rangeFromBlocks.ToString());

        Assert.Throws<ArgumentOutOfRangeException>(() => BitDay.CreateRangeFromBlocks(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => BitDay.CreateRangeFromBlocks(0, BitDay.TotalSlots));
        Assert.Throws<ArgumentException>(() => BitDay.CreateRangeFromBlocks(5, 4));

        var exactBoundary = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10));
        Assert.Equal(36, exactBoundary.StartBlock);
        Assert.Equal(39, exactBoundary.EndBlock);

        var nonBoundary = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9.25), TimeSpan.FromHours(10.1));
        Assert.Equal(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9.25)), nonBoundary.StartBlock);
        Assert.Equal(BitDay.TimeToBlockIndex(TimeSpan.FromHours(10.1)), nonBoundary.EndBlock);
    }

    [Fact]
    public void BitMonth_Constructors_Indexer_AndCreateFromDays_WorkAsExpected()
    {
        var currentMonth = new BitMonth();
        var april = new BitMonth(2025, 4);

        Assert.Equal(DateTime.Today.Year, currentMonth.Year);
        Assert.Equal(DateTime.Today.Month, currentMonth.Month);
        Assert.Equal(2025, april.Year);
        Assert.Equal(4, april.Month);
        Assert.Equal(30, april.Days.Count);
        Assert.Equal(new DateTime(2025, 4, 1), april[1].Date);
        Assert.Equal(new DateTime(2025, 4, 30), april[30].Date);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = april[0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = april[31]);

        var customDays = new List<BitDay>
        {
            new(new DateTime(2025, 5, 3)),
            new(new DateTime(2025, 5, 4))
        };

        var created = BitMonth.CreateFromDays(customDays);
        Assert.Equal(2025, created.Year);
        Assert.Equal(5, created.Month);
        Assert.Same(customDays, created.Days);

        Assert.Throws<ArgumentException>(() => BitMonth.CreateFromDays([]));
        Assert.Throws<ArgumentException>(() => BitMonth.CreateFromDays(null!));
    }

    [Fact]
    public void BitMonth_GetAvailableDays_CoversAllCriteriaModes()
    {
        var month = new BitMonth(2025, 4);
        ReserveHours(month[7], 9, 10);   // Apr 7 Mon
        ReserveHours(month[9], 9, 10);   // Apr 9 Wed
        ReserveHours(month[11], 9, 10);  // Apr 11 Fri
        ReserveHours(month[14], 9, 10);  // Apr 14 Mon
        ReserveHours(month[16], 9, 10);  // Apr 16 Wed
        ReserveHours(month[18], 9, 10);  // Apr 18 Fri
        ReserveHours(month[28], 9, 10);  // Apr 28 Mon

        var noDaysCriteria = new BitSearchCriteria
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Days = Array.Empty<DayOfWeek>()
        };
        var allAvailable = month.GetAvailableDays(noDaysCriteria);
        Assert.Equal(23, allAvailable.Count);
        Assert.DoesNotContain(allAvailable, d => d.Date.Day == 7);

        var singleDayCriteria = new BitSearchCriteria
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Days = [DayOfWeek.Monday]
        };
        var mondays = month.GetAvailableDays(singleDayCriteria);
        Assert.All(mondays, d => Assert.Equal(DayOfWeek.Monday, d.Date.DayOfWeek));
        Assert.Equal([21], mondays.Select(d => d.Date.Day).ToArray());

        var multiDayCriteria = new BitSearchCriteria
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Days = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday]
        };
        var grouped = month.GetAvailableDays(multiDayCriteria);
        Assert.Equal([21, 23, 25], grouped.Select(d => d.Date.Day).ToArray());

        var nullDaysCriteria = new BitSearchCriteria
        {
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10),
            Days = []
        };
        Assert.Equal(allAvailable.Count, month.GetAvailableDays(nullDaysCriteria).Count);
    }

    [Fact]
    public void BitSchedule_Constructor_ThrowsForNullDependencies()
    {
        var configuration = CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30));
        var dbContext = CreateDbContext();
        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());
        var logger = NullLogger<BitSchedule>.Instance;

        Assert.Throws<ArgumentNullException>(() => new BitSchedule(1, configuration, dataService, dbContext, null!));
        Assert.Throws<ArgumentNullException>(() => new BitSchedule(1, configuration, null!, dbContext, logger));
        Assert.Throws<ArgumentNullException>(() => new BitSchedule(1, configuration, dataService, null!, logger));
        Assert.Throws<ArgumentNullException>(() => new BitSchedule(1, null!, dataService, dbContext, logger));
    }

    [Fact]
    public void BitSchedule_LoadReadAndConfigurationBranches_AreCovered()
    {
        using var dbContext = CreateDbContext();
        SeedBitDay(dbContext, new DateTime(2025, 4, 2), clientId: 5);
        SeedBitDay(dbContext, new DateTime(2025, 5, 5), clientId: 5);
        SeedBitDay(dbContext, new DateTime(2025, 4, 4), clientId: 99);
        dbContext.SaveChanges();

        var configuration = CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 5, 31), autoRefresh: false);
        var schedule = CreateSchedule(dbContext, configuration, clientId: 5);

        Assert.False(schedule.IsDirty);
        Assert.True(schedule.LastRefreshed <= DateTime.Now);

        var existingDay = schedule.ReadDay(new DateTime(2025, 4, 2, 8, 0, 0));
        Assert.Equal(5, existingDay.ClientId);
        Assert.Equal(new DateTime(2025, 4, 2), existingDay.Date);

        var missingDay = schedule.ReadDay(new DateTime(2025, 4, 3));
        Assert.Equal(new DateTime(2025, 4, 3), missingDay.Date);
        Assert.True(missingDay.IsFree);
        Assert.Equal(5, missingDay.ClientId);

        var sameInstance = schedule.Configuration;
        schedule.Configuration = sameInstance;
        Assert.False(schedule.IsDirty);

        schedule.Configuration = CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 5, 31), autoRefresh: false);
        Assert.False(schedule.IsDirty);

        schedule.Configuration = CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 5, 31), autoRefresh: true, activeDays: [DayOfWeek.Monday]);
        Assert.False(schedule.IsDirty);
        var afterAutoRefresh = schedule.LastRefreshed;

        schedule.Configuration.TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(11), TimeSpan.FromHours(12));
        Assert.True(schedule.IsDirty);
        Assert.Equal(afterAutoRefresh, schedule.LastRefreshed);

        schedule.Configuration.ActiveDays = [DayOfWeek.Tuesday];
        Assert.False(schedule.IsDirty);
        Assert.True(schedule.LastRefreshed >= afterAutoRefresh);

        // TODO: Why are we setting it to null and testing????
        //schedule.Configuration = null!;
        //Assert.True(schedule.IsDirty);

        //schedule.LoadScheduleData();
        //Assert.False(schedule.IsDirty);
        //Assert.True(schedule.LastRefreshed <= DateTime.Now);
        //Assert.True(schedule.ReadDay(new DateTime(2025, 4, 2)).IsFree);

        schedule.Configuration = CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 5, 31));
        schedule.LoadScheduleData();
        Assert.False(schedule.IsDirty); // it isn't dirty after we just loaded data
        Assert.True(schedule.ReadDay(new DateTime(2025, 4, 2)).IsFree);
    }

    [Fact]
    public async Task BitSchedule_WriteDayAsync_CoversNewExistingFailureAndExceptionPaths()
    {
        await using var dbContext = CreateDbContext();
        SeedBitDay(dbContext, new DateTime(2025, 4, 2), clientId: 3);
        await dbContext.SaveChangesAsync();

        var schedule = CreateSchedule(dbContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30), resourceId: 0), clientId: 3);

        var newDay = await schedule.WriteDayAsync(new BitDayRequest
        {
            Date = new DateTime(2025, 4, 3),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        });
        Assert.False(newDay.IsFree);
        var persistedLegacyDay = await dbContext.BitDays.SingleAsync(d => d.Date == new DateTime(2025, 4, 3) && d.ClientId == 3);
        Assert.Equal(newDay.Date, persistedLegacyDay.Date);
        Assert.False(persistedLegacyDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)), 4));

        var existingDay = await schedule.WriteDayAsync(new BitDayRequest
        {
            Date = new DateTime(2025, 4, 2),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11)
        });
        Assert.False(existingDay.IsFree);
        Assert.False(existingDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(10)), 4));

        var failedReservation = await schedule.WriteDayAsync(new BitDayRequest
        {
            Date = new DateTime(2025, 4, 2),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11)
        });
        Assert.Same(existingDay, failedReservation);

        await using var dbUpdateContext = CreateThrowingDbContext(SaveFailureMode.DbUpdate);
        var dbUpdateSchedule = CreateSchedule(dbUpdateContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30), resourceId: 0), clientId: 8);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbUpdateSchedule.WriteDayAsync(new BitDayRequest
        {
            Date = new DateTime(2025, 4, 4),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        }));

        await using var generalFailureContext = CreateThrowingDbContext(SaveFailureMode.General);
        var generalFailureSchedule = CreateSchedule(generalFailureContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30), resourceId: 0), clientId: 9);

        await Assert.ThrowsAsync<InvalidOperationException>(() => generalFailureSchedule.WriteDayAsync(new BitDayRequest
        {
            Date = new DateTime(2025, 4, 4),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        }));
    }

    [Fact]
    public async Task BitSchedule_WriteScheduleAsync_AndReadSchedule_CoverSuccessFailureAndGrouping()
    {
        await using var dbContext = CreateDbContext();
        var april7 = SeedBitDay(dbContext, new DateTime(2025, 4, 7), clientId: 11);
        var april8 = SeedBitDay(dbContext, new DateTime(2025, 4, 8), clientId: 11);
        var may5 = SeedBitDay(dbContext, new DateTime(2025, 5, 5), clientId: 11);
        await dbContext.SaveChangesAsync();

        var schedule = CreateSchedule(dbContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 5, 31), resourceId: 0), clientId: 11);

        var successRequest = new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 4, 1), EndDate = new DateTime(2025, 5, 31) },
            ActiveDays = [DayOfWeek.Monday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        };
        Assert.True(await schedule.WriteScheduleAsync(successRequest));
        Assert.False(schedule.ReadDay(april7.Date).IsRangeAvailable(36, 4));
        Assert.True(schedule.ReadDay(april8.Date).IsRangeAvailable(36, 4));
        Assert.False(schedule.ReadDay(may5.Date).IsRangeAvailable(36, 4));

        var response = schedule.ReadSchedule(new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 4, 1), EndDate = new DateTime(2025, 5, 31) },
            ActiveDays = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        });
        Assert.Equal(2, response.ScheduledMonths.Count);
        Assert.Equal(2, response.ScheduledMonths[0].Days.Count);
        Assert.Single(response.ScheduledMonths[1].Days);

        var noMatches = schedule.ReadSchedule(new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 6, 1), EndDate = new DateTime(2025, 6, 30) },
            ActiveDays = null!,
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        });
        Assert.Empty(noMatches.ScheduledMonths);

        Assert.True(await schedule.WriteScheduleAsync(new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 4, 1), EndDate = new DateTime(2025, 4, 1) },
            ActiveDays = [DayOfWeek.Wednesday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        }));

        var failureRequest = new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 4, 7), EndDate = new DateTime(2025, 4, 8) },
            ActiveDays = [DayOfWeek.Monday, DayOfWeek.Tuesday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        };
        Assert.False(await schedule.WriteScheduleAsync(failureRequest));
        Assert.Equal(EntityState.Unchanged, dbContext.Entry(schedule.ReadDay(april8.Date)).State);

        await using var dbUpdateFailureContext = CreateThrowingDbContext(SaveFailureMode.DbUpdate);
        SeedBitDay(dbUpdateFailureContext, new DateTime(2025, 4, 14), clientId: 15);
        await dbUpdateFailureContext.SaveChangesCoreAsync();
        var dbUpdateFailureSchedule = CreateSchedule(dbUpdateFailureContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30), resourceId: 0), clientId: 15);

        var saveFailureResult = await dbUpdateFailureSchedule.WriteScheduleAsync(new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 4, 14), EndDate = new DateTime(2025, 4, 14) },
            ActiveDays = [DayOfWeek.Monday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        });
        Assert.False(saveFailureResult);

        await using var generalFailureContext = CreateThrowingDbContext(SaveFailureMode.General);
        SeedBitDay(generalFailureContext, new DateTime(2025, 4, 15), clientId: 16);
        await generalFailureContext.SaveChangesCoreAsync();
        var generalFailureSchedule = CreateSchedule(generalFailureContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30), resourceId: 0), clientId: 16);

        var generalFailureResult = await generalFailureSchedule.WriteScheduleAsync(new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 4, 15), EndDate = new DateTime(2025, 4, 15) },
            ActiveDays = [DayOfWeek.Tuesday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        });
        Assert.False(generalFailureResult);
    }

    [Fact]
    public async Task BitScheduleDataService_SaveAndLoadScheduleDataAsync_PersistsResourceRangeRows()
    {
        await using var dbContext = CreateDbContext();
        var resource = await SeedClientAndResourceAsync(dbContext, clientId: 21, resourceId: 2101);
        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());
        var configuration = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), resourceId: resource.BitResourceId);

        var januaryDay = new BitDay(new DateTime(2025, 1, 15)) { ClientId = 21 };
        januaryDay.ReserveRange(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)), 4);

        var aprilDay = new BitDay(new DateTime(2025, 4, 20)) { ClientId = 21 };
        aprilDay.ReserveRange(BitDay.TimeToBlockIndex(TimeSpan.FromHours(13)), 2);

        await dataService.SaveScheduleDataAsync(configuration, 21, new Dictionary<DateTime, BitDay>
        {
            [januaryDay.Date] = januaryDay,
            [aprilDay.Date] = aprilDay
        });

        var storedRange = await dbContext.BitResourceScheduleRanges.SingleAsync();
        Assert.Equal(21, storedRange.BitClientId);
        Assert.Equal(resource.BitResourceId, storedRange.BitResourceId);
        Assert.Equal(new DateTime(2025, 1, 1), storedRange.StartDate);
        Assert.Equal(new DateTime(2025, 6, 30), storedRange.EndDate);
        Assert.NotEmpty(storedRange.Payload);

        var loadedDays = dataService.LoadScheduleData(configuration, 21);
        var loadedJanuary = Assert.Single(loadedDays, d => d.Date == januaryDay.Date);
        var loadedApril = Assert.Single(loadedDays, d => d.Date == aprilDay.Date);

        Assert.False(loadedJanuary.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)), 4));
        Assert.False(loadedApril.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(13)), 2));
    }

    [Fact]
    public async Task BitScheduleDataService_SaveScheduleDataAsync_UpdatesExistingResourceRangeRowWithoutCreatingDuplicate()
    {
        await using var dbContext = CreateDbContext();
        var resource = await SeedClientAndResourceAsync(dbContext, clientId: 22, resourceId: 2201);
        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());
        var configuration = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), resourceId: resource.BitResourceId);

        var firstDay = new BitDay(new DateTime(2025, 2, 10)) { ClientId = 22 };
        firstDay.ReserveRange(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)), 4);

        await dataService.SaveScheduleDataAsync(configuration, 22, new Dictionary<DateTime, BitDay>
        {
            [firstDay.Date] = firstDay
        });

        var originalRange = await dbContext.BitResourceScheduleRanges.SingleAsync();
        var originalRangeId = originalRange.BitResourceScheduleRangeId;
        var originalPayload = originalRange.Payload.ToArray();

        var secondDay = new BitDay(new DateTime(2025, 2, 11)) { ClientId = 22 };
        secondDay.ReserveRange(BitDay.TimeToBlockIndex(TimeSpan.FromHours(11)), 2);

        await dataService.SaveScheduleDataAsync(configuration, 22, new Dictionary<DateTime, BitDay>
        {
            [secondDay.Date] = secondDay
        });

        Assert.Single(dbContext.BitResourceScheduleRanges);
        var updatedRange = await dbContext.BitResourceScheduleRanges.SingleAsync();
        Assert.Equal(originalRangeId, updatedRange.BitResourceScheduleRangeId);
        Assert.NotEqual(originalPayload, updatedRange.Payload);

        var loadedDays = dataService.LoadScheduleData(configuration, 22);
        var loadedFirstDay = Assert.Single(loadedDays, d => d.Date == firstDay.Date);
        var loadedSecondDay = Assert.Single(loadedDays, d => d.Date == secondDay.Date);

        Assert.False(loadedFirstDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)), 4));
        Assert.False(loadedSecondDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(11)), 2));
    }

    [Fact]
    public async Task BitScheduleDataService_SaveScheduleDataAsync_CreatesSeparateRowsAcrossCanonicalSixMonthBoundaries()
    {
        await using var dbContext = CreateDbContext();
        var resource = await SeedClientAndResourceAsync(dbContext, clientId: 23, resourceId: 2301);
        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());
        var configuration = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), resourceId: resource.BitResourceId);

        var juneDay = new BitDay(new DateTime(2025, 6, 30)) { ClientId = 23 };
        juneDay.ReserveRange(BitDay.TimeToBlockIndex(TimeSpan.FromHours(8)), 2);

        var julyDay = new BitDay(new DateTime(2025, 7, 1)) { ClientId = 23 };
        julyDay.ReserveRange(BitDay.TimeToBlockIndex(TimeSpan.FromHours(15)), 4);

        await dataService.SaveScheduleDataAsync(configuration, 23, new Dictionary<DateTime, BitDay>
        {
            [juneDay.Date] = juneDay,
            [julyDay.Date] = julyDay
        });

        var storedRanges = await dbContext.BitResourceScheduleRanges.OrderBy(r => r.StartDate).ToListAsync();
        Assert.Equal(2, storedRanges.Count);
        Assert.Equal(new DateTime(2025, 1, 1), storedRanges[0].StartDate);
        Assert.Equal(new DateTime(2025, 6, 30), storedRanges[0].EndDate);
        Assert.Equal(new DateTime(2025, 7, 1), storedRanges[1].StartDate);
        Assert.Equal(new DateTime(2025, 12, 31), storedRanges[1].EndDate);

        var loadedDays = dataService.LoadScheduleData(configuration, 23);
        var loadedJune = Assert.Single(loadedDays, d => d.Date == juneDay.Date);
        var loadedJuly = Assert.Single(loadedDays, d => d.Date == julyDay.Date);

        Assert.False(loadedJune.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(8)), 2));
        Assert.False(loadedJuly.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(15)), 4));
    }

    [Fact]
    public async Task BitScheduleDataService_SaveAndLoadScheduleDataAsync_UsesLegacyBitDayFallbackWithoutResourceId()
    {
        await using var dbContext = CreateDbContext();
        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());
        var configuration = CreateConfiguration(new DateTime(2025, 2, 1), new DateTime(2025, 2, 28), resourceId: 0);

        var legacyDay = new BitDay(new DateTime(2025, 2, 10)) { ClientId = 31 };
        legacyDay.ReserveRange(BitDay.TimeToBlockIndex(TimeSpan.FromHours(8)), 3);

        await dataService.SaveScheduleDataAsync(configuration, 31, new Dictionary<DateTime, BitDay>
        {
            [legacyDay.Date] = legacyDay
        });

        var persistedDay = await dbContext.BitDays.SingleAsync(d => d.ClientId == 31 && d.Date == legacyDay.Date);
        Assert.False(persistedDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(8)), 3));
        Assert.Empty(dbContext.BitResourceScheduleRanges);

        var loadedDays = dataService.LoadScheduleData(configuration, 31);
        var loadedDay = Assert.Single(loadedDays);
        Assert.False(loadedDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(8)), 3));
    }

    [Fact]
    public async Task BitSchedule_WriteDayAsync_AndReadDay_PersistThroughResourceScheduleRangeStore()
    {
        await using var dbContext = CreateDbContext();
        var resource = await SeedClientAndResourceAsync(dbContext, clientId: 41, resourceId: 4101);
        var configuration = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), resourceId: resource.BitResourceId);
        var schedule = CreateSchedule(dbContext, configuration, clientId: 41);

        var updatedDay = await schedule.WriteDayAsync(new BitDayRequest
        {
            BitResourceId = resource.BitResourceId,
            Date = new DateTime(2025, 3, 5),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(11)
        });

        Assert.False(updatedDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(10)), 4));
        Assert.Single(dbContext.BitResourceScheduleRanges);
        Assert.Empty(dbContext.BitDays);

        var reloadedSchedule = CreateSchedule(dbContext, configuration, clientId: 41);
        var reloadedDay = reloadedSchedule.ReadDay(new DateTime(2025, 3, 5));
        Assert.False(reloadedDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(10)), 4));
        Assert.Equal(41, reloadedDay.ClientId);
    }

    [Fact]
    public async Task BitSchedule_WriteScheduleAsync_PersistsMultipleResourceDaysAndReadScheduleReturnsThem()
    {
        await using var dbContext = CreateDbContext();
        var resource = await SeedClientAndResourceAsync(dbContext, clientId: 51, resourceId: 5101);
        var configuration = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), resourceId: resource.BitResourceId);
        var schedule = CreateSchedule(dbContext, configuration, clientId: 51);

        var request = new BitScheduleRequest
        {
            BitResourceId = resource.BitResourceId,
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 3, 3), EndDate = new DateTime(2025, 3, 7) },
            ActiveDays = [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(14), TimeSpan.FromHours(15))
        };

        var saved = await schedule.WriteScheduleAsync(request);
        Assert.True(saved);
        Assert.Single(dbContext.BitResourceScheduleRanges);

        var reloadedSchedule = CreateSchedule(dbContext, configuration, clientId: 51);
        var monday = reloadedSchedule.ReadDay(new DateTime(2025, 3, 3));
        var wednesday = reloadedSchedule.ReadDay(new DateTime(2025, 3, 5));
        var friday = reloadedSchedule.ReadDay(new DateTime(2025, 3, 7));
        var tuesday = reloadedSchedule.ReadDay(new DateTime(2025, 3, 4));

        Assert.False(monday.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(14)), 4));
        Assert.False(wednesday.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(14)), 4));
        Assert.False(friday.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(14)), 4));
        Assert.True(tuesday.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(14)), 4));

        var response = reloadedSchedule.ReadSchedule(request);
        Assert.Single(response.ScheduledMonths);
        Assert.Equal([3, 5, 7], response.ScheduledMonths[0].Days.Select(d => d.Date.Day).ToArray());
    }

    [Fact]
    public async Task BitSchedule_WriteDayAsync_UpdatesExistingResourceRangeDataAndPreservesPreviousReservations()
    {
        await using var dbContext = CreateDbContext();
        var resource = await SeedClientAndResourceAsync(dbContext, clientId: 61, resourceId: 6101);
        var configuration = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), resourceId: resource.BitResourceId);
        var schedule = CreateSchedule(dbContext, configuration, clientId: 61);

        await schedule.WriteDayAsync(new BitDayRequest
        {
            BitResourceId = resource.BitResourceId,
            Date = new DateTime(2025, 2, 12),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        });

        var firstPayload = (await dbContext.BitResourceScheduleRanges.SingleAsync()).Payload.ToArray();

        await schedule.WriteDayAsync(new BitDayRequest
        {
            BitResourceId = resource.BitResourceId,
            Date = new DateTime(2025, 2, 13),
            StartTime = TimeSpan.FromHours(13),
            EndTime = TimeSpan.FromHours(14)
        });

        Assert.Single(dbContext.BitResourceScheduleRanges);
        var updatedRange = await dbContext.BitResourceScheduleRanges.SingleAsync();
        Assert.NotEqual(firstPayload, updatedRange.Payload);

        var reloadedSchedule = CreateSchedule(dbContext, configuration, clientId: 61);
        var firstReloaded = reloadedSchedule.ReadDay(new DateTime(2025, 2, 12));
        var secondReloaded = reloadedSchedule.ReadDay(new DateTime(2025, 2, 13));

        Assert.False(firstReloaded.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)), 4));
        Assert.False(secondReloaded.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(13)), 4));
    }

    private static BitSchedule CreateSchedule(BitScheduleDbContext dbContext, BitScheduleConfiguration configuration, int clientId)
    {
        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());
        return new BitSchedule(clientId, configuration, dataService, dbContext, NullLogger<BitSchedule>.Instance);
    }

    private static BitScheduleConfiguration CreateConfiguration(DateTime start, DateTime end, bool autoRefresh = false, DayOfWeek[]? activeDays = null, int resourceId = 1)
    {
        return new BitScheduleConfiguration
        {
            BitResourceId = resourceId,
            DateRange = new BitDateRange { StartDate = start, EndDate = end },
            ActiveDays = activeDays ?? Array.Empty<DayOfWeek>(),
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
            AutoRefreshOnConfigurationChange = autoRefresh
        };
    }

    private BitScheduleDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BitScheduleDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        return new BitScheduleDbContext(options);
    }

    private ThrowingBitScheduleDbContext CreateThrowingDbContext(SaveFailureMode failureMode)
    {
        var options = new DbContextOptionsBuilder<BitScheduleDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        return new ThrowingBitScheduleDbContext(options, failureMode);
    }

    private async Task CreateDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{_testDatabaseName}\"";
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();

        await using (var terminateCommand = connection.CreateCommand())
        {
            terminateCommand.CommandText = @"SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = @databaseName
  AND pid <> pg_backend_pid();";
            terminateCommand.Parameters.AddWithValue("databaseName", _testDatabaseName);
            await terminateCommand.ExecuteNonQueryAsync();
        }

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{_testDatabaseName}\"";
        await dropCommand.ExecuteNonQueryAsync();
    }

    private static BitDay SeedBitDay(BitScheduleDbContext dbContext, DateTime date, int clientId)
    {
        var day = new BitDay(date) { ClientId = clientId };
        dbContext.BitDays.Add(day);
        return day;
    }

    private static void ReserveHours(BitDay day, int startHour, int endHour)
    {
        var startBlock = BitDay.TimeToBlockIndex(TimeSpan.FromHours(startHour));
        var length = (int)(TimeSpan.FromHours(endHour - startHour).TotalMinutes / 15);
        day.ReserveRange(startBlock, length);
    }

    private enum SaveFailureMode
    {
        DbUpdate,
        General
    }

    private sealed class ThrowingBitScheduleDbContext : BitScheduleDbContext
    {
        private readonly SaveFailureMode _failureMode;

        public ThrowingBitScheduleDbContext(DbContextOptions<BitScheduleDbContext> options, SaveFailureMode failureMode)
            : base(options)
        {
            _failureMode = failureMode;
        }

        public Task<int> SaveChangesCoreAsync()
        {
            return base.SaveChangesAsync();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _failureMode switch
            {
                SaveFailureMode.DbUpdate => Task.FromException<int>(new DbUpdateException("Simulated DbUpdateException")),
                SaveFailureMode.General => Task.FromException<int>(new InvalidOperationException("Simulated failure")),
                _ => base.SaveChangesAsync(cancellationToken)
            };
        }
    }

    private static async Task<BitResource> SeedClientAndResourceAsync(BitScheduleDbContext dbContext, int clientId, int resourceId)
    {
        var client = new BitClient
        {
            BitClientId = clientId,
            Name = $"Client {clientId}"
        };

        var resourceType = new BitResourceType
        {
            BitResourceTypeId = resourceId,
            Name = $"Type {resourceId}"
        };

        var resource = new BitResource
        {
            BitResourceId = resourceId,
            BitClientId = clientId,
            BitClient = client,
            BitResourceTypeId = resourceType.BitResourceTypeId,
            BitResourceType = resourceType,
            FirstName = "Resource",
            LastName = resourceId.ToString(),
            EmailAddress = $"resource{resourceId}@example.com"
        };

        dbContext.BitClients.Add(client);
        dbContext.BitResourceTypes.Add(resourceType);
        dbContext.BitResources.Add(resource);
        await dbContext.SaveChangesAsync();
        return resource;
    }
}
