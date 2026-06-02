namespace BitSchedulerCore.Models
{
    /// <summary>
    /// Represents a request to update (write) a specific day's schedule.
    /// Contains the date and a time block (start and end times).
    /// </summary>
    public class BitDayRequest
    {
        public int BitResourceId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
