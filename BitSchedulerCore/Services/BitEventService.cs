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
        ValidateRequest(request);
        await EnsureResourceExistsAsync(clientId, request.BitResourceId, cancellationToken);

        var bitEvent = await CreateBitEventAsync(clientId, request, cancellationToken);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.BitEvents.Add(bitEvent);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (bitEvent.ScheduleBitsReserved)
        {
            await ReserveEventTimeAsync(clientId, bitEvent);
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

    public async Task<IReadOnlyList<BitEvent>> ListEventsAsync(int clientId, BitEventListRequest? request, CancellationToken cancellationToken = default)
    {
        request ??= new BitEventListRequest();

        if (request.RangeStart.HasValue && request.RangeEnd.HasValue && request.RangeEnd <= request.RangeStart)
        {
            throw new ArgumentException("RangeEnd must be after RangeStart.", nameof(request));
        }

        var query = dbContext.BitEvents
            .AsNoTracking()
            .Where(bitEvent => bitEvent.BitClientId == clientId);

        var resourceIds = request.BitResourceIds?
            .Where(resourceId => resourceId > 0)
            .Distinct()
            .ToArray();

        if (resourceIds is { Length: > 0 })
        {
            query = query.Where(bitEvent => resourceIds.Contains(bitEvent.BitResourceId));
        }

        var eventTypes = request.EventTypes?
            .Select(Normalize)
            .Where(eventType => eventType is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (eventTypes is { Length: > 0 })
        {
            query = query.Where(bitEvent => bitEvent.EventType != null && eventTypes.Contains(bitEvent.EventType));
        }

        if (request.RangeStart.HasValue)
        {
            query = query.Where(bitEvent => bitEvent.EndDateTime >= request.RangeStart.Value);
        }

        if (request.RangeEnd.HasValue)
        {
            query = query.Where(bitEvent => bitEvent.StartDateTime <= request.RangeEnd.Value);
        }

        return await query
            .OrderBy(bitEvent => bitEvent.StartDateTime)
            .ThenBy(bitEvent => bitEvent.BitEventId)
            .ToListAsync(cancellationToken);
    }

    public async Task<BitEvent?> UpdateEventAsync(int clientId, int bitEventId, BitEventRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureResourceExistsAsync(clientId, request.BitResourceId, cancellationToken);

        var bitEvent = await dbContext.BitEvents
            .SingleOrDefaultAsync(
                item => item.BitEventId == bitEventId && item.BitClientId == clientId,
                cancellationToken);

        if (bitEvent is null)
        {
            return null;
        }

        var previousReservation = CreateReservationSnapshot(bitEvent);

        await ApplyRequestToEventAsync(bitEvent, request, cancellationToken);
        bitEvent.UpdatedBy = NormalizeAuditUser(request.UpdatedBy);
        bitEvent.UpdatedDate = DateTime.UtcNow;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (previousReservation.ScheduleBitsReserved)
        {
            await ReleaseEventTimeAsync(clientId, previousReservation);
        }

        if (bitEvent.ScheduleBitsReserved)
        {
            await ReserveEventTimeAsync(clientId, bitEvent);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Updated BitEvent {BitEventId} for ClientId {ClientId}, ResourceId {BitResourceId}, Start {StartDateTime}, End {EndDateTime}.",
            bitEvent.BitEventId,
            clientId,
            bitEvent.BitResourceId,
            bitEvent.StartDateTime,
            bitEvent.EndDateTime);

        return bitEvent;
    }

    public async Task<bool> DeleteEventAsync(int clientId, int bitEventId, CancellationToken cancellationToken = default)
    {
        var bitEvent = await dbContext.BitEvents
            .SingleOrDefaultAsync(
                item => item.BitEventId == bitEventId && item.BitClientId == clientId,
                cancellationToken);

        if (bitEvent is null)
        {
            return false;
        }

        var reservation = CreateReservationSnapshot(bitEvent);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (reservation.ScheduleBitsReserved)
        {
            await ReleaseEventTimeAsync(clientId, reservation);
        }

        dbContext.BitEvents.Remove(bitEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Deleted BitEvent {BitEventId} for ClientId {ClientId}.",
            bitEventId,
            clientId);

        return true;
    }

    private async Task ReserveEventTimeAsync(int clientId, BitEvent bitEvent)
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

    private async Task ReleaseEventTimeAsync(int clientId, EventReservationSnapshot reservation)
    {
        var configuration = new BitScheduleConfiguration
        {
            BitResourceId = reservation.BitResourceId,
            DateRange = new BitDateRange
            {
                StartDate = reservation.StartDateTime.Date,
                EndDate = reservation.EndDateTime.Date
            }
        };

        var days = dataService.LoadScheduleData(configuration, clientId)
            .ToDictionary(day => day.Date.Date, day => CloneDay(day, clientId));

        var modifiedDays = new Dictionary<DateTime, BitDay>();

        for (var cursor = reservation.StartDateTime; cursor < reservation.EndDateTime;)
        {
            var dayDate = cursor.Date;
            var dayEnd = dayDate.AddDays(1);
            var segmentEnd = reservation.EndDateTime < dayEnd ? reservation.EndDateTime : dayEnd;

            if (!days.TryGetValue(dayDate, out var day))
            {
                day = new BitDay(dayDate) { ClientId = clientId };
                days[dayDate] = day;
            }

            var timeRange = BitDay.CreateRangeFromTimes(cursor.TimeOfDay, segmentEnd - dayDate);
            var slotLength = timeRange.EndBlock - timeRange.StartBlock + 1;

            day.FreeRange(timeRange.StartBlock, slotLength);
            modifiedDays[dayDate] = day;
            cursor = segmentEnd;
        }

        if (modifiedDays.Count > 0)
        {
            await dataService.SaveScheduleDataAsync(configuration, clientId, modifiedDays);
        }
    }

    private async Task<BitEvent> CreateBitEventAsync(int clientId, BitEventRequest request, CancellationToken cancellationToken)
    {
        var bitEvent = new BitEvent
        {
            BitClientId = clientId,
            CreatedBy = NormalizeAuditUser(request.UpdatedBy),
            CreatedDate = DateTime.UtcNow
        };

        await ApplyRequestToEventAsync(bitEvent, request, cancellationToken);
        bitEvent.UpdatedBy = NormalizeAuditUser(request.UpdatedBy);
        bitEvent.UpdatedDate = DateTime.UtcNow;

        return bitEvent;
    }

    private async Task ApplyRequestToEventAsync(BitEvent bitEvent, BitEventRequest request, CancellationToken cancellationToken)
    {
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

        bitEvent.BitResourceId = request.BitResourceId;
        bitEvent.StartDateTime = request.StartDateTime;
        bitEvent.EndDateTime = request.EndDateTime;
        bitEvent.StartAddress = startLocation.Address;
        bitEvent.StartLatitude = startLocation.Latitude;
        bitEvent.StartLongitude = startLocation.Longitude;
        bitEvent.StartHexGridId = startLocation.HexGridId;
        bitEvent.EndAddress = endLocation.Address;
        bitEvent.EndLatitude = endLocation.Latitude;
        bitEvent.EndLongitude = endLocation.Longitude;
        bitEvent.EndHexGridId = endLocation.HexGridId;
        bitEvent.RequiresTransportation = request.RequiresTransportation;
        bitEvent.RequiresReturnTransportation = request.RequiresReturnTransportation;
        bitEvent.EventType = Normalize(request.EventType);
        bitEvent.ScheduleBitsReserved = request.ReserveScheduleBits;
    }

    private async Task EnsureResourceExistsAsync(int clientId, int bitResourceId, CancellationToken cancellationToken)
    {
        var resourceExists = await dbContext.BitResources
            .AnyAsync(
                resource => resource.BitResourceId == bitResourceId && resource.BitClientId == clientId,
                cancellationToken);

        if (!resourceExists)
        {
            throw new InvalidOperationException($"Resource {bitResourceId} does not exist for client {clientId}.");
        }
    }

    private static void ValidateRequest(BitEventRequest request)
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
    }

    private static EventReservationSnapshot CreateReservationSnapshot(BitEvent bitEvent)
    {
        return new EventReservationSnapshot(
            bitEvent.BitResourceId,
            bitEvent.StartDateTime,
            bitEvent.EndDateTime,
            bitEvent.ScheduleBitsReserved);
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

    private sealed record EventReservationSnapshot(
        int BitResourceId,
        DateTime StartDateTime,
        DateTime EndDateTime,
        bool ScheduleBitsReserved);

    private sealed record ResolvedEventLocation(
        string? Address,
        double? Latitude,
        double? Longitude,
        int? HexGridId);
}
