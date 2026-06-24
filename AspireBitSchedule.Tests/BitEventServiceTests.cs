using BitSchedulerCore;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using BitScheduleServices.Features.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace AspireBitSchedule.Tests;

public class BitEventServiceTests : IAsyncLifetime
{
    private readonly string _adminConnectionString;
    private readonly string _testConnectionString;
    private readonly string _testDatabaseName;

    public BitEventServiceTests()
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
            ? "bitscheduler_event_test"
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
    public async Task CreateEventAsync_PersistsAddressesCoordinatesFlagsAndResolvedHexIds()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientAndResourceAsync(dbContext, clientId: 101, resourceId: 1001);

        var service = CreateEventService(
            dbContext,
            new StubGeocodingService(),
            new StubHexGridSearchService((53.5, -113.5, 7001), (53.6, -113.6, 7002)));

        var created = await service.CreateEventAsync(101, new BitEventRequest
        {
            BitResourceId = 1001,
            StartDateTime = new DateTime(2025, 7, 10, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 7, 10, 10, 30, 0, DateTimeKind.Utc),
            StartAddress = "123 Main St",
            StartLatitude = 53.5,
            StartLongitude = -113.5,
            EndAddress = "500 Second Ave",
            EndLatitude = 53.6,
            EndLongitude = -113.6,
            RequiresTransportation = true,
            RequiresReturnTransportation = false,
            UpdatedBy = "tester"
        });

        var persisted = await dbContext.BitEvents.SingleAsync();

        Assert.Equal(created.BitEventId, persisted.BitEventId);
        Assert.Equal(7001, persisted.StartHexGridId);
        Assert.Equal(7002, persisted.EndHexGridId);
        Assert.Equal("123 Main St", persisted.StartAddress);
        Assert.Equal("500 Second Ave", persisted.EndAddress);
        Assert.Equal(53.5, persisted.StartLatitude);
        Assert.Equal(-113.5, persisted.StartLongitude);
        Assert.True(persisted.RequiresTransportation);
        Assert.False(persisted.RequiresReturnTransportation);
        Assert.Equal("tester", persisted.CreatedBy);
        Assert.Single(dbContext.BitResourceScheduleRanges);
    }

    [Fact]
    public async Task CreateEventAsync_ReservesBitRangesAcrossMidnight()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientAndResourceAsync(dbContext, clientId: 102, resourceId: 1002);

        var service = CreateEventService(dbContext, new StubGeocodingService(), new StubHexGridSearchService());

        await service.CreateEventAsync(102, new BitEventRequest
        {
            BitResourceId = 1002,
            StartDateTime = new DateTime(2025, 7, 10, 23, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 7, 11, 1, 30, 0, DateTimeKind.Utc),
            UpdatedBy = "tester"
        });

        var dataService = new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter());
        var loadedDays = dataService.LoadScheduleData(new BitScheduleConfiguration
        {
            BitResourceId = 1002,
            DateRange = new BitDateRange
            {
                StartDate = new DateTime(2025, 7, 10),
                EndDate = new DateTime(2025, 7, 11)
            }
        }, 102);

        var july10 = Assert.Single(loadedDays, day => day.Date == new DateTime(2025, 7, 10));
        var july11 = Assert.Single(loadedDays, day => day.Date == new DateTime(2025, 7, 11));

        Assert.False(july10.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.FromHours(23)), 4));
        Assert.False(july11.IsRangeAvailable(BitDay.TimeToBlockIndex(TimeSpan.Zero), 6));
    }

    [Fact]
    public async Task CreateEventAsync_DoesNotPersistEventWhenReservationConflicts()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientAndResourceAsync(dbContext, clientId: 103, resourceId: 1003);

        var service = CreateEventService(dbContext, new StubGeocodingService(), new StubHexGridSearchService());

        await service.CreateEventAsync(103, new BitEventRequest
        {
            BitResourceId = 1003,
            StartDateTime = new DateTime(2025, 7, 10, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 7, 10, 10, 0, 0, DateTimeKind.Utc)
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateEventAsync(103, new BitEventRequest
        {
            BitResourceId = 1003,
            StartDateTime = new DateTime(2025, 7, 10, 9, 30, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 7, 10, 10, 30, 0, DateTimeKind.Utc)
        }));

        Assert.Single(await dbContext.BitEvents.ToListAsync());
    }

    [Fact]
    public async Task CreateEventAsync_GeocodesAddressesWhenCoordinatesAreMissing()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientAndResourceAsync(dbContext, clientId: 104, resourceId: 1004);

        var geocodingService = new StubGeocodingService(
            ("123 Main St", new GeocodingResult(53.51, -113.51, "123 Main St, Edmonton, AB")),
            ("500 Second Ave", new GeocodingResult(53.61, -113.61, "500 Second Ave, Edmonton, AB")));
        var hexGridService = new StubHexGridSearchService((53.51, -113.51, 7101), (53.61, -113.61, 7102));
        var service = CreateEventService(dbContext, geocodingService, hexGridService);

        var created = await service.CreateEventAsync(104, new BitEventRequest
        {
            BitResourceId = 1004,
            StartDateTime = new DateTime(2025, 7, 12, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 7, 12, 10, 0, 0, DateTimeKind.Utc),
            StartAddress = "123 Main St",
            EndAddress = "500 Second Ave"
        });

        Assert.Equal(53.51, created.StartLatitude);
        Assert.Equal(-113.51, created.StartLongitude);
        Assert.Equal(7101, created.StartHexGridId);
        Assert.Equal("123 Main St, Edmonton, AB", created.StartAddress);
        Assert.Equal(53.61, created.EndLatitude);
        Assert.Equal(-113.61, created.EndLongitude);
        Assert.Equal(7102, created.EndHexGridId);
        Assert.Equal("500 Second Ave, Edmonton, AB", created.EndAddress);
    }

    [Fact]
    public async Task UpdateEventAsync_RegeocodesAddressWhenAddressChangesButOldCoordinatesRemain()
    {
        await using var dbContext = CreateDbContext();
        await SeedClientAndResourceAsync(dbContext, clientId: 105, resourceId: 1005);

        var geocodingService = new StubGeocodingService(
            ("123 Main St", new GeocodingResult(53.51, -113.51, "123 Main St, Edmonton, AB")),
            ("999 New Ave", new GeocodingResult(53.71, -113.71, "999 New Ave, Edmonton, AB")));
        var hexGridService = new StubHexGridSearchService((53.51, -113.51, 7101), (53.71, -113.71, 7201));
        var service = CreateEventService(dbContext, geocodingService, hexGridService);

        var created = await service.CreateEventAsync(105, new BitEventRequest
        {
            BitResourceId = 1005,
            StartDateTime = new DateTime(2025, 7, 12, 9, 0, 0, DateTimeKind.Utc),
            EndDateTime = new DateTime(2025, 7, 12, 10, 0, 0, DateTimeKind.Utc),
            StartAddress = "123 Main St"
        });

        var updated = await service.UpdateEventAsync(105, created.BitEventId, new BitEventRequest
        {
            BitResourceId = 1005,
            StartDateTime = created.StartDateTime,
            EndDateTime = created.EndDateTime,
            StartAddress = "999 New Ave",
            StartLatitude = created.StartLatitude,
            StartLongitude = created.StartLongitude,
            StartHexGridId = created.StartHexGridId
        });

        Assert.NotNull(updated);
        Assert.Equal("999 New Ave, Edmonton, AB", updated.StartAddress);
        Assert.Equal(53.71, updated.StartLatitude);
        Assert.Equal(-113.71, updated.StartLongitude);
        Assert.Equal(7201, updated.StartHexGridId);
    }

    private BitScheduleDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BitScheduleDbContext>()
            .UseNpgsql(_testConnectionString)
            .Options;

        return new BitScheduleDbContext(options);
    }

    private static async Task SeedClientAndResourceAsync(BitScheduleDbContext dbContext, int clientId, int resourceId)
    {
        var client = new BitClient
        {
            BitClientId = clientId,
            Name = $"Client {clientId}"
        };

        var resourceType = new BitResourceType
        {
            BitResourceTypeId = resourceId,
            BitClientId = clientId,
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
    }

    private static BitEventService CreateEventService(
        BitScheduleDbContext dbContext,
        IGeocodingService geocodingService,
        IHexGridSearchService hexGridSearchService)
    {
        return new BitEventService(
            dbContext,
            new BitScheduleDataService(dbContext, new BitResourceScheduleRangePayloadConverter()),
            new AddressLocationService(geocodingService, hexGridSearchService),
            NullLogger<BitEventService>.Instance);
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

    private sealed class StubGeocodingService(params (string Address, GeocodingResult Result)[] mappings) : IGeocodingService
    {
        public Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            foreach (var mapping in mappings)
            {
                if (string.Equals(mapping.Address, address, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<GeocodingResult?>(mapping.Result);
                }
            }

            return Task.FromResult<GeocodingResult?>(null);
        }
    }

    private sealed class StubHexGridSearchService(params (double Latitude, double Longitude, int GridId)[] mappings) : IHexGridSearchService
    {
        public Task ReloadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public int? GetGridId(double latitude, double longitude)
        {
            foreach (var mapping in mappings)
            {
                if (mapping.Latitude.Equals(latitude) && mapping.Longitude.Equals(longitude))
                {
                    return mapping.GridId;
                }
            }

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
