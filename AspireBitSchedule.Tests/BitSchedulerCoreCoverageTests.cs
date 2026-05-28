using BitSchedulerCore;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using BitTimeScheduler;
using BitTimeScheduler.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspireBitSchedule.Tests;

public class BitSchedulerCoreCoverageTests
{
    [Fact]
    public void BitDay_DefaultAndDateConstructors_InitializeExpectedState()
    {
        var explicitDate = new DateTime(2025, 4, 12, 13, 45, 0);

        var defaultDay = new BitDay();
        var explicitDay = new BitDay(explicitDate);

        Assert.Equal(DateTime.Today, defaultDay.Date);
        Assert.Equal(explicitDate.Date, explicitDay.Date);
        Assert.True(explicitDay.IsFree);
        Assert.Equal(0UL, explicitDay.BitsLow);
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

        day.BitsLow = 0x0123456789ABCDEF;
        day.BitsHigh = 0xFEDCBA9876543210;

        Assert.Equal(0x0123456789ABCDEFUL, day.BitsLow);
        Assert.Equal(0xFEDCBA9876543210UL, day.BitsHigh);

        day.SetMetadataFlag(BitTimeMetadataFlags.IsFree, false);
        Assert.False(day.GetMetadataFlag(BitTimeMetadataFlags.IsFree));

        day.IsFree = true;

        Assert.Equal(42, day.BitDayId);
        Assert.Equal(7, day.ClientId);
        Assert.Equal(0x0123456789ABCDEFUL, day.BitsLow);
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
            Days = null!
        };
        Assert.Equal(allAvailable.Count, month.GetAvailableDays(nullDaysCriteria).Count);
    }

    [Fact]
    public void BitSchedule_Constructor_ThrowsForNullDependencies()
    {
        var configuration = CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30));
        var dbContext = CreateDbContext();
        var dataService = new BitScheduleDataService(dbContext);
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

        schedule.Configuration = null!;
        Assert.True(schedule.IsDirty);

        schedule.LoadScheduleData();
        Assert.False(schedule.IsDirty);
        Assert.True(schedule.LastRefreshed <= DateTime.Now);
        Assert.True(schedule.ReadDay(new DateTime(2025, 4, 2)).IsFree);

        dbContext.Dispose();
        schedule.Configuration = CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 5, 31));
        schedule.LoadScheduleData();
        Assert.True(schedule.IsDirty);
        Assert.True(schedule.ReadDay(new DateTime(2025, 4, 2)).IsFree);
    }

    [Fact]
    public async Task BitSchedule_WriteDayAsync_CoversNewExistingFailureAndExceptionPaths()
    {
        await using var dbContext = CreateDbContext();
        SeedBitDay(dbContext, new DateTime(2025, 4, 2), clientId: 3);
        await dbContext.SaveChangesAsync();

        var schedule = CreateSchedule(dbContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30)), clientId: 3);

        var newDay = await schedule.WriteDayAsync(new BitDayRequest
        {
            Date = new DateTime(2025, 4, 3),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        });
        Assert.False(newDay.IsFree);
        Assert.NotEqual(0, newDay.BitDayId);
        Assert.NotNull(await dbContext.BitDays.SingleAsync(d => d.Date == new DateTime(2025, 4, 3) && d.ClientId == 3));

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
        var dbUpdateSchedule = CreateSchedule(dbUpdateContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30)), clientId: 8);

        await Assert.ThrowsAsync<DbUpdateException>(() => dbUpdateSchedule.WriteDayAsync(new BitDayRequest
        {
            Date = new DateTime(2025, 4, 4),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        }));

        await using var generalFailureContext = CreateThrowingDbContext(SaveFailureMode.General);
        var generalFailureSchedule = CreateSchedule(generalFailureContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30)), clientId: 9);

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

        var schedule = CreateSchedule(dbContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 5, 31)), clientId: 11);

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
        var dbUpdateFailureSchedule = CreateSchedule(dbUpdateFailureContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30)), clientId: 15);

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
        var generalFailureSchedule = CreateSchedule(generalFailureContext, CreateConfiguration(new DateTime(2025, 4, 1), new DateTime(2025, 4, 30)), clientId: 16);

        var generalFailureResult = await generalFailureSchedule.WriteScheduleAsync(new BitScheduleRequest
        {
            DateRange = new BitDateRange { StartDate = new DateTime(2025, 4, 15), EndDate = new DateTime(2025, 4, 15) },
            ActiveDays = [DayOfWeek.Tuesday],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        });
        Assert.False(generalFailureResult);
    }

    private static BitSchedule CreateSchedule(BitScheduleDbContext dbContext, BitScheduleConfiguration configuration, int clientId)
    {
        var dataService = new BitScheduleDataService(dbContext);
        return new BitSchedule(clientId, configuration, dataService, dbContext, NullLogger<BitSchedule>.Instance);
    }

    private static BitScheduleConfiguration CreateConfiguration(DateTime start, DateTime end, bool autoRefresh = false, DayOfWeek[]? activeDays = null)
    {
        return new BitScheduleConfiguration
        {
            DateRange = new BitDateRange { StartDate = start, EndDate = end },
            ActiveDays = activeDays ?? Array.Empty<DayOfWeek>(),
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10)),
            AutoRefreshOnConfigurationChange = autoRefresh
        };
    }

    private static BitScheduleDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BitScheduleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BitScheduleDbContext(options);
    }

    private static ThrowingBitScheduleDbContext CreateThrowingDbContext(SaveFailureMode failureMode)
    {
        var options = new DbContextOptionsBuilder<BitScheduleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ThrowingBitScheduleDbContext(options, failureMode);
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
        None,
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
}
