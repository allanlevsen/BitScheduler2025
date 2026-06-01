
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitTimeScheduler.Models;
using BitTimeScheduler;
using Microsoft.EntityFrameworkCore;

namespace BitSchedulerCore.Services
{
    public class BitScheduleDataService
    {
        private const int RangeMonths = 6;
        private readonly BitScheduleDbContext _dbContext;
        private readonly BitResourceScheduleRangePayloadConverter _payloadConverter = new();

        public BitScheduleDataService(BitScheduleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<BitDay> LoadScheduleData(BitScheduleConfiguration config, int clientId)
        {
            if (config?.DateRange == null)
            {
                return new List<BitDay>();
            }

            if (config.BitResourceId <= 0)
            {
                return LoadLegacyBitDays(config, clientId);
            }

            DateTime requestedStart = config.DateRange.StartDate.Date;
            DateTime requestedEnd = config.DateRange.EndDate.Date;

            var days = CreateEmptyDays(requestedStart, requestedEnd, clientId);

            var overlappingRanges = _dbContext.BitResourceScheduleRanges
                .Where(r => r.BitClientId == clientId &&
                            r.BitResourceId == config.BitResourceId &&
                            r.StartDate <= requestedEnd &&
                            r.EndDate >= requestedStart)
                .OrderBy(r => r.StartDate)
                .ToList();

            foreach (var range in overlappingRanges)
            {
                var rangeDays = _payloadConverter.Deserialize(range.StartDate, range.EndDate, range.Payload, clientId);
                foreach (var day in rangeDays.Values)
                {
                    if (day.Date < requestedStart || day.Date > requestedEnd)
                    {
                        continue;
                    }

                    day.ClientId = clientId;
                    days[day.Date] = day;
                }
            }

            return days.Values.OrderBy(d => d.Date).ToList();
        }

        public async Task SaveScheduleDataAsync(BitScheduleConfiguration config, int clientId, IReadOnlyDictionary<DateTime, BitDay> daysToSave)
        {
            if (config?.DateRange == null || daysToSave == null || daysToSave.Count == 0)
            {
                return;
            }

            if (config.BitResourceId <= 0)
            {
                await SaveLegacyBitDaysAsync(clientId, daysToSave);
                return;
            }

            var groupedByRange = daysToSave.Values
                .GroupBy(day => GetCanonicalRangeStart(day.Date))
                .ToList();

            foreach (var group in groupedByRange)
            {
                DateTime rangeStart = group.Key;
                DateTime rangeEnd = GetCanonicalRangeEnd(rangeStart);

                var scheduleRange = await _dbContext.BitResourceScheduleRanges
                    .SingleOrDefaultAsync(r => r.BitClientId == clientId &&
                                               r.BitResourceId == config.BitResourceId &&
                                               r.StartDate == rangeStart &&
                                               r.EndDate == rangeEnd);

                var rangeDays = scheduleRange == null
                    ? CreateEmptyDays(rangeStart, rangeEnd, clientId)
                    : _payloadConverter.Deserialize(rangeStart, rangeEnd, scheduleRange.Payload, clientId);

                foreach (var day in group)
                {
                    rangeDays[day.Date.Date] = CloneDay(day, clientId);
                }

                byte[] payload = _payloadConverter.Serialize(rangeStart, rangeEnd, rangeDays);

                if (scheduleRange == null)
                {
                    scheduleRange = new BitResourceScheduleRange
                    {
                        BitClientId = clientId,
                        BitResourceId = config.BitResourceId,
                        StartDate = rangeStart,
                        EndDate = rangeEnd,
                        Payload = payload,
                        CreatedBy = "system",
                        CreatedDate = DateTime.UtcNow,
                        UpdatedBy = "system",
                        UpdatedDate = DateTime.UtcNow
                    };

                    _dbContext.BitResourceScheduleRanges.Add(scheduleRange);
                }
                else
                {
                    scheduleRange.Payload = payload;
                    scheduleRange.UpdatedBy = "system";
                    scheduleRange.UpdatedDate = DateTime.UtcNow;
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        public (DateTime StartDate, DateTime EndDate) GetCanonicalRange(DateTime date)
        {
            DateTime rangeStart = GetCanonicalRangeStart(date);
            return (rangeStart, GetCanonicalRangeEnd(rangeStart));
        }

        private List<BitDay> LoadLegacyBitDays(BitScheduleConfiguration config, int clientId)
        {
            return _dbContext.BitDays
                .Where(d => d.Date >= config.DateRange.StartDate &&
                            d.Date <= config.DateRange.EndDate &&
                            d.ClientId == clientId)
                .OrderBy(d => d.Date)
                .ToList();
        }

        private async Task SaveLegacyBitDaysAsync(int clientId, IReadOnlyDictionary<DateTime, BitDay> daysToSave)
        {
            foreach (BitDay day in daysToSave.Values)
            {
                DateTime date = day.Date.Date;
                var existing = await _dbContext.BitDays.SingleOrDefaultAsync(d => d.ClientId == clientId && d.Date == date);
                if (existing == null)
                {
                    var newDay = CloneDay(day, clientId);
                    _dbContext.BitDays.Add(newDay);
                }
                else
                {
                    existing.BitsLow = day.BitsLow;
                    existing.BitsHigh = day.BitsHigh;
                    existing.IsFree = day.IsFree;
                }
            }

            await _dbContext.SaveChangesAsync();
        }

        private static Dictionary<DateTime, BitDay> CreateEmptyDays(DateTime startDate, DateTime endDate, int clientId)
        {
            var days = new Dictionary<DateTime, BitDay>();
            for (DateTime current = startDate.Date; current <= endDate.Date; current = current.AddDays(1))
            {
                days[current] = new BitDay(current) { ClientId = clientId };
            }

            return days;
        }

        private static BitDay CloneDay(BitDay source, int clientId)
        {
            return new BitDay(source.Date)
            {
                ClientId = clientId,
                BitsLow = source.BitsLow,
                BitsHigh = source.BitsHigh,
                IsFree = source.IsFree
            };
        }

        private static DateTime GetCanonicalRangeStart(DateTime date)
        {
            int startMonth = date.Month <= RangeMonths ? 1 : 1 + RangeMonths;
            return new DateTime(date.Year, startMonth, 1);
        }

        private static DateTime GetCanonicalRangeEnd(DateTime rangeStart)
        {
            return rangeStart.AddMonths(RangeMonths).AddDays(-1);
        }
    }
}

