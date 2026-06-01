using BitTimeScheduler;

namespace BitSchedulerCore.Services
{
    public sealed class BitResourceScheduleRangePayloadConverter
    {
        private const int BitsPerDay = BitDay.TotalSlots;
        private const int DayDataSize = 12;
        private const int MetadataSize = sizeof(uint);
        private const int HeaderSize = DayDataSize + MetadataSize;

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
                day.DayData = ReadUInt128(payload, offset, DayDataSize);
                day.Metadata = BitConverter.ToUInt32(payload, offset + DayDataSize);
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
                WriteUInt128(payload, offset, day.DayData, DayDataSize);
                WriteUInt32(payload, offset + DayDataSize, day.Metadata);
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

        private static UInt128 ReadUInt128(byte[] buffer, int offset, int byteCount)
        {
            UInt128 value = 0;
            for (var index = 0; index < byteCount; index++)
            {
                value |= (UInt128)buffer[offset + index] << (index * 8);
            }

            return value;
        }

        private static void WriteUInt128(byte[] buffer, int offset, UInt128 value, int byteCount)
        {
            for (var index = 0; index < byteCount; index++)
            {
                buffer[offset + index] = (byte)(value >> (index * 8));
            }
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            Buffer.BlockCopy(bytes, 0, buffer, offset, bytes.Length);
        }
    }
}
