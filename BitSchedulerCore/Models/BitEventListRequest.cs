namespace BitSchedulerCore.Models;

public class BitEventListRequest
{
    public IReadOnlyCollection<int>? BitResourceIds { get; set; }
    public DateTime? RangeStart { get; set; }
    public DateTime? RangeEnd { get; set; }
    public IReadOnlyCollection<string>? EventTypes { get; set; }
}
