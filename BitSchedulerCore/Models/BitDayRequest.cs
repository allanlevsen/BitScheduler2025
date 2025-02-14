using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitSchedulerCore.Models
{
    /// <summary>
    /// Represents a request to update (write) a specific day's schedule.
    /// Contains the date and a time block (start and end times).
    /// </summary>
    public class BitDayRequest
    {
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }
}
