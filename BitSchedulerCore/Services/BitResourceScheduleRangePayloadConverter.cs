using BitSchedulerCore.Models;
using BitTimeScheduler;

namespace BitSchedulerCore.Services
{
    public sealed class BitResourceScheduleRangePayloadConverter
    {
        private const int BitsPerDay = BitDay.TotalSlots;
        private const int HeaderSize = sizeof(ulong) + sizeof(ulong) + sizeof(byte);

        public int GetPayloadLength(DateTime startDate, DateTime endDate)
        {
            var dayCount = GetDayCount(startDate, endDate);
            return dayCount * HeaderSize;
        }

        public Dictionary<DateTime, BitDay> Deserialize(DateTime startDate, DateTime endDate, byte[]? payload, int clientId)
        {
            var normalizedStart = startDate.Date;
            var normalizedEnd = endDate.Date;
            var result = CreateEmptyDays(normalizedStart, normalizedEnd, clientId);

            if (payload == null || payload.Length == 0)
            {
                return result;
            }

            var dayCount = GetDayCount(normalizedStart, normalizedEnd);
            var expectedLength = dayCount * HeaderSize;
            if (payload.Length != expectedLength)
            {
                throw new InvalidDataException($"Payload length {payload.Length} does not match expected length {expectedLength}.");
            }

            for (var dayIndex = 0; dayIndex < dayCount; dayIndex++)
            {
                var offset = dayIndex * HeaderSize;
                var dayDate = normalizedStart.AddDays(dayIndex);
                var day = result[dayDate];
                day.BitsLow = BitConverter.ToUInt64(payload, offset);
                day.BitsHigh = BitConverter.ToUInt64(payload, offset + sizeof(ulong));
                day.IsFree = payload[offset + sizeof(ulong) + sizeof(ulong)] != 0;
            }

            return result;
        }

        public byte[] Serialize(DateTime startDate, DateTime endDate, IReadOnlyDictionary<DateTime, BitDay> days)
        {
            var normalizedStart = startDate.Date;
            var dayCount = GetDayCount(normalizedStart, endDate.Date);
            var payload = new byte[dayCount * HeaderSize];

            for (var dayIndex = 0; dayIndex < dayCount; dayIndex++)
            {
                var currentDate = normalizedStart.AddDays(dayIndex);
                if (!days.TryGetValue(currentDate, out var day))
                {
                    day = new BitDay(currentDate);
                }

                var offset = dayIndex * HeaderSize;
                WriteUInt64(payload, offset, day.BitsLow);
                WriteUInt64(payload, offset + sizeof(ulong), day.BitsHigh);
                payload[offset + sizeof(ulong) + sizeof(ulong)] = day.IsFree ? (byte)1 : (byte)0;
            }

            return payload;
        }

        public int GetDayOffset(DateTime rangeStart, DateTime targetDate)
        {
            return (targetDate.Date - rangeStart.Date).Days;
        }

        public int GetBitOffset(DateTime rangeStart, DateTime targetDate, int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= BitsPerDay)
            {
                throw new ArgumentOutOfRangeException(nameof(blockIndex));
            }

            return GetDayOffset(rangeStart, targetDate) * BitsPerDay + blockIndex;
        }

        public void EnsureRangeContains(DateTime startDate, DateTime endDate, DateTime targetDate)
        {
            var normalizedTarget = targetDate.Date;
            if (normalizedTarget < startDate.Date || normalizedTarget > endDate.Date)
            {
                throw new ArgumentOutOfRangeException(nameof(targetDate), "Target date is outside the schedule range.");
            }
        }

        private static Dictionary<DateTime, BitDay> CreateEmptyDays(DateTime startDate, DateTime endDate, int clientId)
        {
            var days = new Dictionary<DateTime, BitDay>();
            for (var current = startDate.Date; current <= endDate.Date; current = current.AddDays(1))
            {
                days[current] = new BitDay(current) { ClientId = clientId };
            }

            return days;
        }

        private static int GetDayCount(DateTime startDate, DateTime endDate)
        {
            var normalizedStart = startDate.Date;
            var normalizedEnd = endDate.Date;
            if (normalizedEnd < normalizedStart)
            {
                throw new ArgumentOutOfRangeException(nameof(endDate), "End date must be on or after start date.");
            }

            return (normalizedEnd - normalizedStart).Days + 1;
        }

        private static void WriteUInt64(byte[] buffer, int offset, ulong value)
        {
            var bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
        }
    }
}
