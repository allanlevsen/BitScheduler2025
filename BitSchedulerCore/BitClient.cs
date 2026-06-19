namespace BitSchedulerCore
{
    public class BitClient
    {
        public int BitClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<BitResource> BitResources { get; set; } = new List<BitResource>();
        public ICollection<BitReservation> BitReservations { get; set; } = new List<BitReservation>();
        public ICollection<BitResourceScheduleRange> BitResourceScheduleRanges { get; set; } = new List<BitResourceScheduleRange>();
        public ICollection<BitEvent> BitEvents { get; set; } = new List<BitEvent>();
    }
}
