namespace BitTimeScheduler.Models
{
    /// <summary>
    /// Represents the configuration for a schedule.
    /// Contains a date range, active weekdays, and a time block.
    /// </summary>
    public class BitScheduleConfiguration
    {
        public BitDateRange DateRange { get; set; }
        public DayOfWeek[] ActiveDays { get; set; }
        public BitTimeRange TimeBlock { get; set; }
    }
}
