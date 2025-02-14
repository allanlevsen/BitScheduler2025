namespace BitTimeScheduler.Models
{
    /// <summary>
    /// Represents the response from a schedule read operation.
    /// For simplicity, this example returns a list of BitDay objects
    /// that meet the schedule criteria.
    /// </summary>

    public class BitScheduleResponse
    {
        public BitScheduleRequest Request { get; set; }
        public List<BitMonth> ScheduledMonths { get; set; }
    }
}
