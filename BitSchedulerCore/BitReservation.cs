using BitSchedulerCore.Models;

namespace BitSchedulerCore
{
    public class BitReservation : AuditableEntity
    {
        // Primary key for the reservation.
        public int BitReservationId { get; set; }

        // Foreign key to BitClient.
        public int BitClientId { get; set; }
        public BitClient BitClient { get; set; } = null!;

        // Foreign key to BitResource.
        public int BitResourceId { get; set; }
        public BitResource BitResource { get; set; } = null!;

        // The date for which this reservation applies.
        public DateTime Date { get; set; }

        // The reserved time block, in terms of 15-minute intervals.
        public int StartBlock { get; set; }
        public int SlotLength { get; set; }

        // Computed properties (optional)
        public TimeSpan StartTime => TimeSpan.FromMinutes(StartBlock * 15d);
        public TimeSpan EndTime => TimeSpan.FromMinutes((StartBlock + SlotLength) * 15d);
    }
}
