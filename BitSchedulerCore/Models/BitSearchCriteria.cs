namespace BitTimeScheduler.Models
{

    /// <summary>
    /// Contains search criteria for locating available time blocks.
    /// </summary>
    public class BitSearchCriteria
    {
        /// <summary>
        /// The start time for the search (assumed to align with a 15–minute boundary).
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// The end time for the search (assumed to align with a 15–minute boundary).
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// The days of the week to include in the search.
        /// </summary>
        public DayOfWeek[] Days { get; set; }
    }

}
