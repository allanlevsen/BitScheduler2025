using BitSchedulerCore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitSchedulerCore
{
    public class BitReservation : AuditableEntity
    {
        // Primary key
        public int BitReservationId { get; set; }

        // Multi-tenant identifier.
        public Guid ClientId { get; set; }

        // The date for which this reservation applies.
        public DateTime Date { get; set; }

        // The resource identifier (e.g., person, room, equipment).
        public string ResourceId { get; set; }

        // The reserved time block, expressed as a starting block index and a length (in number of 15-minute slots).
        public int StartBlock { get; set; }
        public int SlotLength { get; set; }

        // Computed properties (optional)
        public TimeSpan StartTime => TimeSpan.FromMinutes(StartBlock * 15);
        public TimeSpan EndTime => TimeSpan.FromMinutes((StartBlock + SlotLength) * 15);
    }
}
