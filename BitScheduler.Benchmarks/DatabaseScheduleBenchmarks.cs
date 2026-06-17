using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BitSchedulerCore;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.EntityFrameworkCore;

namespace BitScheduler.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public sealed class DatabaseScheduleBenchmarks
{
    private const int ClientId = 1;
    private const int ResourceTypeId = 1;
    private const int ResourceId = 1;
    private const int ReservationLength = 4;
    private static readonly DateTime BenchmarkStartDate = new(2025, 1, 1);

    private readonly BitResourceScheduleRangePayloadConverter _payloadConverter = new();
    private string _databasePath = string.Empty;
    private BitScheduleConfiguration _configuration = null!;
    private IReadOnlyDictionary<DateTime, BitDay> _seededDays = null!;
    private IReadOnlyDictionary<DateTime, BitDay> _insertDays = null!;
    private IReadOnlyDictionary<DateTime, BitDay> _updatedDays = null!;

    [Params(7, 30, 180)]
    public int RequestedDayCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var benchmarkDirectory = Path.Combine(Path.GetTempPath(), "BitScheduler.Benchmarks");
        Directory.CreateDirectory(benchmarkDirectory);

        _databasePath = Path.Combine(benchmarkDirectory, $"database-schedule-{Guid.NewGuid():N}.db");
        _configuration = CreateConfiguration(RequestedDayCount);
        _seededDays = CreateDaysToPersist(_configuration.DateRange!.StartDate, RequestedDayCount, startBlock: 32);
        _insertDays = CreateDaysToPersist(_configuration.DateRange.StartDate, RequestedDayCount, startBlock: 36);
        _updatedDays = CreateDaysToPersist(_configuration.DateRange.StartDate, RequestedDayCount, startBlock: 40);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    [IterationSetup(Target = nameof(LoadScheduleData_FromSqlite))]
    public void SetupReadIteration()
    {
        ResetDatabase(_seededDays);
    }

    [IterationSetup(Target = nameof(SaveScheduleData_InsertNewResourceRangeAsync))]
    public void SetupInsertIteration()
    {
        ResetDatabase();
    }

    [IterationSetup(Target = nameof(SaveScheduleData_UpdateExistingResourceRangeAsync))]
    public void SetupUpdateIteration()
    {
        ResetDatabase(_seededDays);
    }

    [Benchmark(Baseline = true)]
    public int LoadScheduleData_FromSqlite()
    {
        using var dbContext = CreateContext();
        var dataService = CreateDataService(dbContext);

        return dataService.LoadScheduleData(_configuration, ClientId).Count;
    }

    [Benchmark]
    public async Task<int> SaveScheduleData_InsertNewResourceRangeAsync()
    {
        await using var dbContext = CreateContext();
        var dataService = CreateDataService(dbContext);

        await dataService.SaveScheduleDataAsync(_configuration, ClientId, _insertDays);

        return await dbContext.BitResourceScheduleRanges.CountAsync();
    }

    [Benchmark]
    public async Task<int> SaveScheduleData_UpdateExistingResourceRangeAsync()
    {
        await using var dbContext = CreateContext();
        var dataService = CreateDataService(dbContext);

        await dataService.SaveScheduleDataAsync(_configuration, ClientId, _updatedDays);

        var scheduleRange = await dbContext.BitResourceScheduleRanges.SingleAsync(range =>
            range.BitClientId == ClientId &&
            range.BitResourceId == ResourceId);

        return scheduleRange.Payload.Length;
    }

    private void ResetDatabase(IReadOnlyDictionary<DateTime, BitDay>? seededDays = null)
    {
        using var dbContext = CreateContext();
        dbContext.Database.EnsureDeleted();
        dbContext.Database.EnsureCreated();

        SeedReferenceData(dbContext);

        if (seededDays != null)
        {
            SeedScheduleRange(dbContext, seededDays);
        }
    }

    private BitScheduleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BitScheduleDbContext>()
            .UseSqlite($"Data Source={_databasePath}")
            .Options;

        return new BitScheduleDbContext(options);
    }

    private BitScheduleDataService CreateDataService(BitScheduleDbContext dbContext)
    {
        return new BitScheduleDataService(dbContext, _payloadConverter);
    }

    private void SeedReferenceData(BitScheduleDbContext dbContext)
    {
        dbContext.BitClients.Add(new BitClient
        {
            BitClientId = ClientId,
            Name = "Benchmark Client"
        });

        dbContext.BitResourceTypes.Add(new BitResourceType
        {
            BitResourceTypeId = ResourceTypeId,
            Name = "Benchmark Resource Type"
        });

        dbContext.BitResources.Add(new BitResource
        {
            BitResourceId = ResourceId,
            BitClientId = ClientId,
            BitResourceTypeId = ResourceTypeId,
            FirstName = "Benchmark",
            LastName = "Resource",
            EmailAddress = "benchmark.resource@example.com"
        });

        dbContext.SaveChanges();
    }

    private void SeedScheduleRange(BitScheduleDbContext dbContext, IReadOnlyDictionary<DateTime, BitDay> seededDays)
    {
        var dataService = CreateDataService(dbContext);
        var range = dataService.GetCanonicalRange(_configuration.DateRange!.StartDate);

        var allDaysInRange = CreateDaysToPersist(range.StartDate, (range.EndDate - range.StartDate).Days + 1, startBlock: -1);
        foreach (var seededDay in seededDays)
        {
            allDaysInRange[seededDay.Key] = CloneDay(seededDay.Value);
        }

        dbContext.BitResourceScheduleRanges.Add(new BitResourceScheduleRange
        {
            BitClientId = ClientId,
            BitResourceId = ResourceId,
            StartDate = range.StartDate,
            EndDate = range.EndDate,
            Payload = _payloadConverter.Serialize(range.StartDate, range.EndDate, allDaysInRange),
            CreatedBy = "benchmark",
            CreatedDate = DateTime.UtcNow,
            UpdatedBy = "benchmark",
            UpdatedDate = DateTime.UtcNow
        });

        dbContext.SaveChanges();
    }

    private static BitScheduleConfiguration CreateConfiguration(int requestedDayCount)
    {
        return new BitScheduleConfiguration
        {
            BitResourceId = ResourceId,
            DateRange = new BitDateRange
            {
                StartDate = BenchmarkStartDate,
                EndDate = BenchmarkStartDate.AddDays(requestedDayCount - 1)
            },
            ActiveDays = [],
            AutoRefreshOnConfigurationChange = false
        };
    }

    private static Dictionary<DateTime, BitDay> CreateDaysToPersist(DateTime startDate, int dayCount, int startBlock)
    {
        var days = new Dictionary<DateTime, BitDay>(dayCount);

        for (var index = 0; index < dayCount; index++)
        {
            var date = startDate.Date.AddDays(index);
            var day = new BitDay(date)
            {
                ClientId = ClientId
            };

            if (startBlock >= 0)
            {
                day.ReserveRange(startBlock, ReservationLength);
            }

            days[date] = day;
        }

        return days;
    }

    private static BitDay CloneDay(BitDay source)
    {
        return new BitDay(source.Date)
        {
            ClientId = source.ClientId,
            DayData = source.DayData,
            Metadata = source.Metadata
        };
    }
}