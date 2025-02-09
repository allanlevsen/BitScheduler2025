namespace BitTimeScheduler.Models
{
    /// <summary>
    /// Represents the configuration for a schedule.
    /// Contains a date range, active weekdays, and a time block.
    /// </summary>
    public class BitScheduleConfiguration
    {
        public bool AutoRefreshOnConfigurationChange { get; set; } = false;
        public BitDateRange DateRange { get; set; } = new BitDateRange();
        public DayOfWeek[] ActiveDays { get; set; } = new DayOfWeek[0];
        public BitTimeRange TimeBlock { get; set; } = new BitTimeRange();
    }
}
