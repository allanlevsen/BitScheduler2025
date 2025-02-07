namespace BitTimeScheduler.Models
{
    /// <summary>
    /// Represents a scheduling request.
    /// (This class may still be used when constructing a schedule, but its properties are now consolidated into BitScheduleConfiguration.)
    /// </summary>
    public class BitScheduleRequest
    {
        public BitDateRange DateRange { get; set; }
        public DayOfWeek[] ActiveDays { get; set; }
        public BitTimeRange TimeBlock { get; set; }
    }
}
