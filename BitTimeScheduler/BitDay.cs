using BitTimeScheduler.Models;

namespace BitTimeScheduler
{

    /// <summary>
    /// Represents one day divided into 96 15–minute slots (96 bits) plus 32 bits of metadata.
    /// The lower 96 bits (split between two 64–bit values) are used for time slots,
    /// while the upper 32 bits of the second value are reserved for metadata.
    /// A <see cref="Date"/> property holds the actual date of the BitDay.
    /// </summary>
    public class BitDay
    {
        /// <summary>
        /// Total number of 15–minute slots in a day (24 * 4).
        /// </summary>
        public const int TotalSlots = 96;

        /// <summary>
        /// The date this BitDay represents.
        /// </summary>
        public DateTime Date { get; set; }

        // The complete 128–bit state is stored using two 64–bit unsigned integers.
        // _bitsLow holds bits 0–63.
        // _bitsHigh holds bits 64–127.
        // In _bitsHigh, the lower 32 bits (bits 64–95 overall) are used for day slots,
        // and the upper 32 bits (bits 96–127 overall) are reserved for metadata.
        private ulong _bitsLow;
        private ulong _bitsHigh;

        // Masks for isolating the day portion and the metadata portion of _bitsHigh:
        private const ulong HighDayMask = 0x00000000FFFFFFFFUL;  // Bits 64–95 (day slots)
        private const ulong MetadataMask = 0xFFFFFFFF00000000UL;  // Bits 96–127 (metadata)

        /// <summary>
        /// Parameterless constructor.
        /// Creates a BitDay for today's date.
        /// </summary>
        public BitDay() : this(DateTime.Now)
        {
        }

        /// <summary>
        /// Creates a BitDay for the specified date.
        /// </summary>
        /// <param name="date">The date for this BitDay. Only the date part is used.</param>
        public BitDay(DateTime date)
        {
            Date = date.Date;
            _bitsLow = 0UL;
            _bitsHigh = 0UL;

            // Mark day as free since no slots are reserved.
            SetMetadataFlag(BitTimeMetadataFlags.IsFree, true);
        }

        #region Day Slot Operations

        /// <summary>
        /// Validates that the specified time range (in slots) is within bounds.
        /// </summary>
        /// <param name="startSlot">The starting slot index (0–95).</param>
        /// <param name="length">The number of slots in the range.</param>
        private void ValidateRange(int startSlot, int length)
        {
            if (startSlot < 0 || length <= 0 || startSlot + length > TotalSlots)
                throw new ArgumentOutOfRangeException(nameof(startSlot),
                    $"Range must be within 0 and {TotalSlots - 1}");
        }

        /// <summary>
        /// Creates two masks (one for _bitsLow and one for the day portion of _bitsHigh) for the given slot range.
        /// </summary>
        /// <param name="startSlot">The starting slot index.</param>
        /// <param name="length">The number of slots in the range.</param>
        /// <param name="maskLow">Output mask for _bitsLow.</param>
        /// <param name="maskHigh">Output mask for the day portion of _bitsHigh.</param>
        private void GetDayMask(int startSlot, int length, out ulong maskLow, out ulong maskHigh)
        {
            maskLow = 0UL;
            maskHigh = 0UL;
            int endSlot = startSlot + length - 1;

            if (endSlot < 64)
            {
                // Entire range is in _bitsLow.
                maskLow = CreateMask64(length) << startSlot;
            }
            else if (startSlot >= 64)
            {
                // Entire range is in the day portion of _bitsHigh.
                int startIndexHigh = startSlot - 64;
                maskHigh = CreateMask32(length) << startIndexHigh;
            }
            else
            {
                // Range spans both _bitsLow and _bitsHigh.
                int countLow = 64 - startSlot;
                int countHigh = length - countLow;
                maskLow = CreateMask64(countLow) << startSlot;
                maskHigh = CreateMask32(countHigh);
            }
        }

        /// <summary>
        /// Creates a mask with the given number of bits set (for a 64–bit value).
        /// </summary>
        private static ulong CreateMask64(int length)
        {
            return length >= 64 ? ulong.MaxValue : (1UL << length) - 1;
        }

        /// <summary>
        /// Creates a mask with the given number of bits set (for a 32–bit portion stored in a ulong).
        /// </summary>
        private static ulong CreateMask32(int length)
        {
            return length >= 32 ? 0xFFFFFFFFUL : (1UL << length) - 1;
        }

        /// <summary>
        /// Returns true if the specified range of slots is available.
        /// If the day is marked as entirely free (IsFree == true), then the range is available without checking the bits.
        /// </summary>
        public bool IsRangeAvailable(int startSlot, int length)
        {
            ValidateRange(startSlot, length);

            // Quick path: if the entire day is free, then any range is available.
            if (IsFree)
                return true;

            GetDayMask(startSlot, length, out ulong maskLow, out ulong maskHigh);
            // Only consider the day portion of _bitsHigh.
            ulong currentHighDay = _bitsHigh & HighDayMask;
            return (_bitsLow & maskLow) == 0UL && (currentHighDay & maskHigh) == 0UL;
        }

        /// <summary>
        /// Reserves the specified range of slots if available.
        /// Returns true if the reservation succeeded. After a reservation, the day is marked as not free.
        /// </summary>
        public bool ReserveRange(int startSlot, int length)
        {
            ValidateRange(startSlot, length);

            // Check if the range is available (using our fast IsFree shortcut if applicable).
            if (!IsRangeAvailable(startSlot, length))
                return false;

            GetDayMask(startSlot, length, out ulong maskLow, out ulong maskHigh);
            _bitsLow |= maskLow;
            ulong currentHighDay = _bitsHigh & HighDayMask;
            currentHighDay |= maskHigh;
            _bitsHigh = _bitsHigh & MetadataMask | currentHighDay;

            // Since at least one slot is now reserved, mark the day as not free.
            SetMetadataFlag(BitTimeMetadataFlags.IsFree, false);

            return true;
        }

        /// <summary>
        /// Frees (marks as available) the specified range of slots.
        /// If, after freeing, all slots in the day are available, the day is marked as free.
        /// </summary>
        public void FreeRange(int startSlot, int length)
        {
            ValidateRange(startSlot, length);

            GetDayMask(startSlot, length, out ulong maskLow, out ulong maskHigh);
            _bitsLow &= ~maskLow;
            ulong currentHighDay = _bitsHigh & HighDayMask;
            currentHighDay &= ~maskHigh;
            _bitsHigh = _bitsHigh & MetadataMask | currentHighDay;

            // If the entire day is free (i.e. all 96 slots are unreserved),
            // then mark the day as free for faster future checks.
            if (_bitsLow == 0UL && (_bitsHigh & HighDayMask) == 0UL)
            {
                SetMetadataFlag(BitTimeMetadataFlags.IsFree, true);
            }
        }

        #endregion

        #region Utility Methods for Time and Block Conversion

        /// <summary>
        /// Converts a TimeSpan (assumed to be aligned on a 15-minute boundary) into a block index.
        /// </summary>
        public static int TimeToBlockIndex(TimeSpan time)
        {
            return (int)(time.TotalMinutes / 15);
        }

        /// <summary>
        /// Converts a block index into a TimeSpan representing the start time of that block.
        /// </summary>
        public static TimeSpan BlockIndexToTime(int blockIndex)
        {
            return TimeSpan.FromMinutes(blockIndex * 15);
        }

        /// <summary>
        /// Creates a BitTimeRange from start and end block indices.
        /// Assumes that the block range is inclusive.
        /// The StartTime is calculated from the startBlock and the EndTime from (endBlock + 1).
        /// </summary>
        public static BitTimeRange CreateRangeFromBlocks(int startBlock, int endBlock)
        {
            BitTimeRange range = new BitTimeRange
            {
                StartBlock = startBlock,
                EndBlock = endBlock,
                StartTime = BlockIndexToTime(startBlock),
                EndTime = BlockIndexToTime(endBlock + 1)
            };
            return range;
        }

        /// <summary>
        /// Creates a BitTimeRange from two TimeSpans.
        /// Assumes the times are aligned on 15-minute boundaries.
        /// The StartBlock is computed as (startTime.TotalMinutes / 15) and the EndBlock as (endTime.TotalMinutes / 15) - 1.
        /// </summary>
        public static BitTimeRange CreateRangeFromTimes(TimeSpan startTime, TimeSpan endTime)
        {
            BitTimeRange range = new BitTimeRange
            {
                StartTime = startTime,
                EndTime = endTime,
                StartBlock = TimeToBlockIndex(startTime)
            };

            // If endTime is exactly on a 15-minute boundary, treat the range as [start, endBlock] inclusive.
            // For example, 9:00 to 10:00 results in blocks 36 to 39.
            if (endTime.TotalMinutes % 15 == 0)
                range.EndBlock = (int)(endTime.TotalMinutes / 15) - 1;
            else
                range.EndBlock = TimeToBlockIndex(endTime);

            return range;
        }

        #endregion

        #region Metadata Operations

        /// <summary>
        /// Retrieves the value of the specified metadata flag.
        /// The flag is shifted 32 bits to align with the metadata portion.
        /// </summary>
        public bool GetMetadataFlag(BitTimeMetadataFlags flag)
        {
            ulong flagMask = (ulong)flag << 32;
            return (_bitsHigh & flagMask) != 0;
        }

        /// <summary>
        /// Sets or clears the specified metadata flag.
        /// </summary>
        public void SetMetadataFlag(BitTimeMetadataFlags flag, bool value)
        {
            ulong flagMask = (ulong)flag << 32;
            if (value)
                _bitsHigh |= flagMask;
            else
                _bitsHigh &= ~flagMask;
        }

        /// <summary>
        /// A convenience property for the IsFree metadata flag.
        /// </summary>
        public bool IsFree
        {
            get { return GetMetadataFlag(BitTimeMetadataFlags.IsFree); }
            set { SetMetadataFlag(BitTimeMetadataFlags.IsFree, value); }
        }

        #endregion

        /// <summary>
        /// Returns a string representation of the BitDay.
        /// </summary>
        public override string ToString()
        {
            // For example: "4/12/2025 - [128-bit state in hex]"
            return $"{Date.ToShortDateString()} - {_bitsHigh:X16}{_bitsLow:X16}";
        }
    }


}
