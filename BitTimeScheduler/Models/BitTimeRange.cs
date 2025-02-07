namespace BitTimeScheduler.Models
{
    /// Represents a time range both in TimeSpan form and in 15-minute block indices.
    /// The StartBlock and EndBlock represent an inclusive range.
    /// For example, a BitTimeRange with StartBlock=36 and EndBlock=39 corresponds to
    /// a time range from 9:00 AM to 10:00 AM.

    /// For example, a BitTimeRange with StartBlock = 36 and EndBlock = 39 corresponds to 9:00 AM to 10:00 AM.
    public class BitTimeRange

    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int StartBlock { get; set; }
        public int EndBlock { get; set; }

        public override string ToString()
        {
            return $"TimeRange: {StartTime} to {EndTime} (Blocks: {StartBlock} to {EndBlock})";
        }
    }
}
