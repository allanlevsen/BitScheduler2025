using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BitSchedulerCore.Services;

public sealed class BitEventService(
    BitScheduleDbContext dbContext,
    BitScheduleDataService dataService,
    IGeocodingService geocodingService,
    IHexGridSearchService hexGridSearchService,
    ILogger<BitEventService> logger) : IBitEventService
{
    public async Task<BitEvent> CreateEventAsync(int clientId, BitEventRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.BitResourceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.BitResourceId), "A valid resource id is required.");
        }

        if (request.EndDateTime <= request.StartDateTime)
        {
            throw new ArgumentException("EndDateTime must be after StartDateTime.", nameof(request));
        }

        var resourceExists = await dbContext.BitResources
            .AnyAsync(
                resource => resource.BitResourceId == request.BitResourceId && resource.BitClientId == clientId,
                cancellationToken);

        if (!resourceExists)
        {
            throw new InvalidOperationException($"Resource {request.BitResourceId} does not exist for client {clientId}.");
        }

        var startLocation = await ResolveLocationAsync(
            request.StartAddress,
            request.StartLatitude,
            request.StartLongitude,
            request.StartHexGridId,
            cancellationToken);

        var endLocation = await ResolveLocationAsync(
            request.EndAddress,
            request.EndLatitude,
            request.EndLongitude,
            request.EndHexGridId,
            cancellationToken);

        var bitEvent = new BitEvent
        {
            BitClientId = clientId,
            BitResourceId = request.BitResourceId,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            StartAddress = startLocation.Address,
            StartLatitude = startLocation.Latitude,
            StartLongitude = startLocation.Longitude,
            StartHexGridId = startLocation.HexGridId,
            EndAddress = endLocation.Address,
            EndLatitude = endLocation.Latitude,
            EndLongitude = endLocation.Longitude,
            EndHexGridId = endLocation.HexGridId,
            RequiresTransportation = request.RequiresTransportation,
            RequiresReturnTransportation = request.RequiresReturnTransportation,
            CreatedBy = NormalizeAuditUser(request.UpdatedBy),
            CreatedDate = DateTime.UtcNow,
            UpdatedBy = NormalizeAuditUser(request.UpdatedBy),
            UpdatedDate = DateTime.UtcNow
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.BitEvents.Add(bitEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.ReserveScheduleBits)
        {
            await ReserveEventTimeAsync(clientId, bitEvent, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Created BitEvent {BitEventId} for ClientId {ClientId}, ResourceId {BitResourceId}, Start {StartDateTime}, End {EndDateTime}.",
            bitEvent.BitEventId,
            clientId,
            bitEvent.BitResourceId,
            bitEvent.StartDateTime,
            bitEvent.EndDateTime);

        return bitEvent;
    }

    public Task<BitEvent?> GetEventAsync(int clientId, int bitEventId, CancellationToken cancellationToken = default)
    {
        return dbContext.BitEvents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                bitEvent => bitEvent.BitEventId == bitEventId && bitEvent.BitClientId == clientId,
                cancellationToken);
    }

    private async Task ReserveEventTimeAsync(int clientId, BitEvent bitEvent, CancellationToken cancellationToken)
    {
        var configuration = new BitScheduleConfiguration
        {
            BitResourceId = bitEvent.BitResourceId,
            DateRange = new BitDateRange
            {
                StartDate = bitEvent.StartDateTime.Date,
                EndDate = bitEvent.EndDateTime.Date
            }
        };

        var days = dataService.LoadScheduleData(configuration, clientId)
            .ToDictionary(day => day.Date.Date, day => CloneDay(day, clientId));

        var modifiedDays = new Dictionary<DateTime, BitDay>();

        for (var cursor = bitEvent.StartDateTime; cursor < bitEvent.EndDateTime;)
        {
            var dayDate = cursor.Date;
            var dayEnd = dayDate.AddDays(1);
            var segmentEnd = bitEvent.EndDateTime < dayEnd ? bitEvent.EndDateTime : dayEnd;

            if (!days.TryGetValue(dayDate, out var day))
            {
                day = new BitDay(dayDate) { ClientId = clientId };
                days[dayDate] = day;
            }

            var timeRange = BitDay.CreateRangeFromTimes(cursor.TimeOfDay, segmentEnd - dayDate);
            var slotLength = timeRange.EndBlock - timeRange.StartBlock + 1;

            if (!day.ReserveRange(timeRange.StartBlock, slotLength))
            {
                throw new InvalidOperationException(
                    $"The requested event overlaps an existing reservation for resource {bitEvent.BitResourceId} on {dayDate:yyyy-MM-dd}.");
            }

            modifiedDays[dayDate] = day;
            cursor = segmentEnd;
        }

        if (modifiedDays.Count > 0)
        {
            await dataService.SaveScheduleDataAsync(configuration, clientId, modifiedDays);
        }
    }

    private int? ResolveHexGridId(double? latitude, double? longitude)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return null;
        }

        return hexGridSearchService.GetGridId(latitude.Value, longitude.Value);
    }

    private async Task<ResolvedEventLocation> ResolveLocationAsync(
        string? address,
        double? latitude,
        double? longitude,
        int? existingHexGridId,
        CancellationToken cancellationToken)
    {
        var normalizedAddress = Normalize(address);
        var resolvedLatitude = latitude;
        var resolvedLongitude = longitude;
        var resolvedAddress = normalizedAddress;

        if ((!resolvedLatitude.HasValue || !resolvedLongitude.HasValue) && normalizedAddress is not null)
        {
            var geocodingResult = await geocodingService.GeocodeAsync(normalizedAddress, cancellationToken);
            if (geocodingResult is not null)
            {
                resolvedLatitude = geocodingResult.Latitude;
                resolvedLongitude = geocodingResult.Longitude;
                resolvedAddress = Normalize(geocodingResult.FormattedAddress) ?? normalizedAddress;
            }
        }

        var resolvedHexGridId = ResolveHexGridId(resolvedLatitude, resolvedLongitude) ?? existingHexGridId;

        return new ResolvedEventLocation(
            resolvedAddress,
            resolvedLatitude,
            resolvedLongitude,
            resolvedHexGridId);
    }

    private static BitDay CloneDay(BitDay source, int clientId)
    {
        return new BitDay(source.Date)
        {
            ClientId = clientId,
            DayData = source.DayData,
            Metadata = source.Metadata
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeAuditUser(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "system" : value.Trim();
    }

    private sealed record ResolvedEventLocation(
        string? Address,
        double? Latitude,
        double? Longitude,
        int? HexGridId);
}
