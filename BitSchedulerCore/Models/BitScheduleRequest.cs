namespace BitSchedulerCore.Models;

/// <summary>
/// Represents a scheduling request.
/// (This class may still be used when constructing a schedule, but its properties are now consolidated into BitScheduleConfiguration.)
/// </summary>
public class BitScheduleRequest
{
    public int BitResourceId { get; set; }
    public BitDateRange? DateRange { get; set; } = new();
    public DayOfWeek[]? ActiveDays { get; set; } = Array.Empty<DayOfWeek>();
    public BitTimeRange? TimeBlock { get; set; } = new();
}
