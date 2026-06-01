using BitSchedulerCore.Models;

namespace BitSchedulerCore
{
    public class BitResourceScheduleRange : AuditableEntity
    {
        public int BitResourceScheduleRangeId { get; set; }

        public int BitClientId { get; set; }
        public BitClient BitClient { get; set; } = null!;

        public int BitResourceId { get; set; }
        public BitResource BitResource { get; set; } = null!;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public byte[] Payload { get; set; } = Array.Empty<byte>();
    }
}
