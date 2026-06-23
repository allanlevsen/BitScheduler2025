namespace BitSchedulerCore
{
    public class BitResource
    {
        public int BitResourceId { get; set; }
        public int BitResourceTypeId { get; set; }
        public BitResourceType BitResourceType { get; set; } = null!;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public int BitClientId { get; set; }
        public BitClient BitClient { get; set; } = null!;
        public ICollection<BitResourceScheduleRange> BitResourceScheduleRanges { get; set; } = new List<BitResourceScheduleRange>();
        public ICollection<BitEvent> BitEvents { get; set; } = new List<BitEvent>();
    }
}
