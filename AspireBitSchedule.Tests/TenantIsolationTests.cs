using BitSchedulerCore;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace AspireBitSchedule.Tests;

public class TenantIsolationTests : IAsyncLifetime
{
    private readonly string _adminConnectionString;
    private readonly string _testConnectionString;
    private readonly string _testDatabaseName;

    public TenantIsolationTests()
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
            ? "bitscheduler_tenant_test"
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
    public async Task BitResourceTypeService_UsesBitClientIdForTenantIsolation()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientsAsync(dbContext, 101, 202);

        var service = new BitResourceTypeService(dbContext, NullLogger<BitResourceTypeService>.Instance);

        var clientOneType = await service.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 101,
            Name = "Nurse"
        });

        var clientTwoType = await service.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 202,
            Name = "Nurse"
        });

        Assert.NotEqual(clientOneType.BitResourceTypeId, clientTwoType.BitResourceTypeId);

        var clientOneTypes = await service.ListResourceTypesAsync(101);
        var clientTwoTypes = await service.ListResourceTypesAsync(202);

        Assert.Single(clientOneTypes);
        Assert.Single(clientTwoTypes);
        Assert.Equal(101, clientOneTypes[0].BitClientId);
        Assert.Equal(202, clientTwoTypes[0].BitClientId);
        Assert.Null(await service.GetResourceTypeAsync(101, clientTwoType.BitResourceTypeId));
    }

    [Fact]
    public async Task BitResourceService_RejectsCrossTenantResourceTypeAndScopesQueriesByBitClientId()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientsAsync(dbContext, 101, 202);

        var typeService = new BitResourceTypeService(dbContext, NullLogger<BitResourceTypeService>.Instance);
        var resourceService = new BitResourceService(dbContext, NullLogger<BitResourceService>.Instance);

        var clientOneType = await typeService.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 101,
            Name = "Dispatcher"
        });

        var clientTwoType = await typeService.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 202,
            Name = "Dispatcher"
        });

        var clientOneResource = await resourceService.CreateResourceAsync(101, new BitResourceRequest
        {
            BitResourceTypeId = clientOneType.BitResourceTypeId,
            FirstName = "Alice",
            LastName = "One",
            EmailAddress = "alice.one@example.com"
        });

        var clientTwoResource = await resourceService.CreateResourceAsync(202, new BitResourceRequest
        {
            BitResourceTypeId = clientTwoType.BitResourceTypeId,
            FirstName = "Bob",
            LastName = "Two",
            EmailAddress = "bob.two@example.com"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => resourceService.CreateResourceAsync(101, new BitResourceRequest
        {
            BitResourceTypeId = clientTwoType.BitResourceTypeId,
            FirstName = "Cross",
            LastName = "Tenant",
            EmailAddress = "cross.tenant@example.com"
        }));

        var clientOneResources = await resourceService.ListResourcesAsync(101);
        var clientTwoResources = await resourceService.ListResourcesAsync(202);

        Assert.Single(clientOneResources);
        Assert.Single(clientTwoResources);
        Assert.Equal(clientOneResource.BitResourceId, clientOneResources[0].BitResourceId);
        Assert.Equal(clientTwoResource.BitResourceId, clientTwoResources[0].BitResourceId);
        Assert.Null(await resourceService.GetResourceAsync(101, clientTwoResource.BitResourceId));
    }

    [Fact]
    public async Task BitEventService_RejectsCrossTenantResourcesAndPersistsBitClientIdOnEventAndScheduleRange()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientsAsync(dbContext, 101, 202);

        var typeService = new BitResourceTypeService(dbContext, NullLogger<BitResourceTypeService>.Instance);
        var resourceService = new BitResourceService(dbContext, NullLogger<BitResourceService>.Instance);

        var clientOneType = await typeService.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 101,
            Name = "Therapist"
        });

        var clientTwoType = await typeService.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 202,
            Name = "Therapist"
        });

        var clientOneResource = await resourceService.CreateResourceAsync(101, new BitResourceRequest
        {
            BitResourceTypeId = clientOneType.BitResourceTypeId,
            FirstName = "Casey",
            LastName = "One",
            EmailAddress = "casey.one@example.com"
        });

        var clientTwoResource = await resourceService.CreateResourceAsync(202, new BitResourceRequest
        {
            BitResourceTypeId = clientTwoType.BitResourceTypeId,
            FirstName = "Jordan",
            LastName = "Two",
            EmailAddress = "jordan.two@example.com"
        });

        var eventService = CreateEventService(dbContext);

        var createdEvent = await eventService.CreateEventAsync(101, new BitEventRequest
        {
            BitResourceId = clientOneResource.BitResourceId,
            StartDateTime = new DateTime(2025, 8, 4, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 8, 4, 10, 0, 0, DateTimeKind.Utc),
            EventType = "Visit",
            UpdatedBy = "tenant-test"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => eventService.CreateEventAsync(101, new BitEventRequest
        {
            BitResourceId = clientTwoResource.BitResourceId,
            StartDateTime = new DateTime(2025, 8, 4, 11, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 8, 4, 12, 0, 0, DateTimeKind.Utc),
            UpdatedBy = "tenant-test"
        }));

        var storedEvent = await dbContext.BitEvents.SingleAsync(bitEvent => bitEvent.BitEventId == createdEvent.BitEventId);
        var storedRange = await dbContext.BitResourceScheduleRanges.SingleAsync();

        Assert.Equal(101, storedEvent.BitClientId);
        Assert.Equal(clientOneResource.BitResourceId, storedEvent.BitResourceId);
        Assert.Equal(101, storedRange.BitClientId);
        Assert.Equal(clientOneResource.BitResourceId, storedRange.BitResourceId);
        Assert.Null(await eventService.GetEventAsync(202, createdEvent.BitEventId));
        Assert.Single(await eventService.ListEventsAsync(101, null));
        Assert.Empty(await eventService.ListEventsAsync(202, null));
    }

    [Fact]
    public async Task BitScheduleDataService_SeparatesLegacyAndResourceScheduleRowsByBitClientId()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientsAsync(dbContext, 101, 202);

        var typeService = new BitResourceTypeService(dbContext, NullLogger<BitResourceTypeService>.Instance);
        var resourceService = new BitResourceService(dbContext, NullLogger<BitResourceService>.Instance);

        var clientOneType = await typeService.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 101,
            Name = "Scheduler"
        });

        var clientTwoType = await typeService.CreateResourceTypeAsync(new BitResourceTypeRequest
        {
            BitClientId = 202,
            Name = "Scheduler"
        });

        var clientOneResource = await resourceService.CreateResourceAsync(101, new BitResourceRequest
        {
            BitResourceTypeId = clientOneType.BitResourceTypeId,
            FirstName = "Dana",
            LastName = "One",
            EmailAddress = "dana.one@example.com"
        });

        var clientTwoResource = await resourceService.CreateResourceAsync(202, new BitResourceRequest
        {
            BitResourceTypeId = clientTwoType.BitResourceTypeId,
            FirstName = "Evan",
            LastName = "Two",
            EmailAddress = "evan.two@example.com"
        });

        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());

        var clientOneResourceConfig = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), clientOneResource.BitResourceId);
        var clientTwoResourceConfig = CreateConfiguration(new DateTime(2025, 1, 1), new DateTime(2025, 6, 30), clientTwoResource.BitResourceId);

        var clientOneDay = ReservedDay(new DateTime(2025, 2, 10), 101, 9, 10);
        var clientTwoDay = ReservedDay(new DateTime(2025, 2, 10), 202, 13, 14);

        await dataService.SaveScheduleDataAsync(clientOneResourceConfig, 101, new Dictionary<DateTime, BitDay>
        {
            [clientOneDay.Date] = clientOneDay
        });

        await dataService.SaveScheduleDataAsync(clientTwoResourceConfig, 202, new Dictionary<DateTime, BitDay>
        {
            [clientTwoDay.Date] = clientTwoDay
        });

        var resourceRanges = await dbContext.BitResourceScheduleRanges
            .OrderBy(range => range.BitClientId)
            .ToListAsync();

        Assert.Equal(2, resourceRanges.Count);
        Assert.Equal([101, 202], resourceRanges.Select(range => range.BitClientId).ToArray());

        var clientOneLoadedResourceDays = dataService.LoadScheduleData(clientOneResourceConfig, 101);
        var clientTwoLoadedResourceDays = dataService.LoadScheduleData(clientTwoResourceConfig, 202);

        Assert.False(Assert.Single(clientOneLoadedResourceDays, day => day.Date == clientOneDay.Date)
            .IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(9)), 4));
        Assert.False(Assert.Single(clientTwoLoadedResourceDays, day => day.Date == clientTwoDay.Date)
            .IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(13)), 4));

        var legacyConfiguration = CreateConfiguration(new DateTime(2025, 3, 1), new DateTime(2025, 3, 31), resourceId: 0);
        var clientOneLegacyDay = ReservedDay(new DateTime(2025, 3, 12), 101, 8, 9);
        var clientTwoLegacyDay = ReservedDay(new DateTime(2025, 3, 12), 202, 15, 16);

        await dataService.SaveScheduleDataAsync(legacyConfiguration, 101, new Dictionary<DateTime, BitDay>
        {
            [clientOneLegacyDay.Date] = clientOneLegacyDay
        });

        await dataService.SaveScheduleDataAsync(legacyConfiguration, 202, new Dictionary<DateTime, BitDay>
        {
            [clientTwoLegacyDay.Date] = clientTwoLegacyDay
        });

        var legacyDays = await dbContext.BitDays
            .OrderBy(day => day.ClientId)
            .ToListAsync();

        Assert.Equal(2, legacyDays.Count);
        Assert.Equal([101, 202], legacyDays.Select(day => day.ClientId).ToArray());

        var clientOneLoadedLegacyDay = Assert.Single(dataService.LoadScheduleData(legacyConfiguration, 101));
        var clientTwoLoadedLegacyDay = Assert.Single(dataService.LoadScheduleData(legacyConfiguration, 202));

        Assert.False(clientOneLoadedLegacyDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(8)), 4));
        Assert.False(clientTwoLoadedLegacyDay.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(15)), 4));
    }

    private BitScheduleDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BitScheduleDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        return new BitScheduleDbContext(options);
    }

    private static BitEventService CreateEventService(BitScheduleDbContext dbContext)
    {
        return new BitEventService(
            dbContext,
            new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter()),
            new StubGeocodingService(),
            new StubHexGridSearchService(),
            NullLogger<BitEventService>.Instance);
    }

    private static BitScheduleConfiguration CreateConfiguration(DateTime start, DateTime end, int resourceId)
    {
        return new BitScheduleConfiguration
        {
            BitResourceId = resourceId,
            DateRange = new BitDateRange
            {
                StartDate = start,
                EndDate = end
            },
            ActiveDays = [],
            TimeBlock = BitDay.CreateRangeFromTimes(TimeSpan.FromHours(9), TimeSpan.FromHours(10))
        };
    }

    private static BitDay ReservedDay(DateTime date, int clientId, int startHour, int endHour)
    {
        var day = new BitDay(date) { ClientId = clientId };
        var startBlock = BitDay.TimeToBlockIndex(TimeSpan.FromHours(startHour));
        var length = (int)(TimeSpan.FromHours(endHour - startHour).TotalMinutes / 15);
        day.ReserveRange(startBlock, length);
        return day;
    }

    private static async Task SeedClientsAsync(BitScheduleDbContext dbContext, params int[] clientIds)
    {
        foreach (var clientId in clientIds)
        {
            dbContext.BitClients.Add(new BitClient
            {
                BitClientId = clientId,
                Name = $"Client {clientId}"
            });
        }

        await dbContext.SaveChangesAsync();
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

    private sealed class StubGeocodingService : IGeocodingService
    {
        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<GeocodingResult?>(null);
        }
    }

    private sealed class StubHexGridSearchService : IHexGridSearchService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public int? GetGridId(double latitude, double longitude)
        {
            return null;
        }

        public HexCellDto? GetCell(int gridId)
        {
            return null;
        }

        public IReadOnlyList<int> GetNeighborGridIds(int gridId)
        {
            return [];
        }

        public IReadOnlyList<int> GetGridIdsWithinRing(int gridId, int maxRingDistance)
        {
            return [];
        }

        public IReadOnlyList<int> GetGridIdsWithinRing(double latitude, double longitude, int maxRingDistance)
        {
            return [];
        }

        public IReadOnlyList<(double Latitude, double Longitude)> GetHexPolygon(int gridId)
        {
            return [];
        }

        public IReadOnlyList<int> ExpandSearch(int gridId, int startRing, int endRing)
        {
            return [];
        }
    }
}
