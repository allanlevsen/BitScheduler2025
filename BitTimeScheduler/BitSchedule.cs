using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitTimeScheduler.Data;
using BitTimeScheduler.Models;

namespace BitTimeScheduler
{
    /// <summary>
    /// Provides scheduling functionality built on BitDay/BitMonth.
    /// Stores internal schedule data and the current configuration.
    /// When the Configuration property is changed, if AutoRefreshOnConfigurationChange is true,
    /// the internal data is refreshed automatically.
    /// The ReadSchedule and WriteSchedule methods operate on the internally stored data.
    /// </summary>
    public class BitSchedule
    {
        private MockData _mockData;

        // Internal schedule data—stored as a list of BitDay objects.
        private List<BitDay> scheduleData;

        // The current configuration.
        private BitScheduleConfiguration _configuration;

        /// <summary>
        /// Gets or sets the schedule configuration.
        /// If AutoRefreshOnConfigurationChange is true, setting a new configuration triggers a data refresh.
        /// </summary>
        public BitScheduleConfiguration Configuration
        {
            get { return _configuration; }
            set
            {
                _configuration = value;
                if (AutoRefreshOnConfigurationChange)
                {
                    RefreshScheduleData();
                }
            }
        }

        /// <summary>
        /// When set to true, changes to the Configuration property automatically refresh the internal schedule data.
        /// </summary>
        public bool AutoRefreshOnConfigurationChange { get; set; } = true;

        /// <summary>
        /// Empty constructor. Initializes the internal schedule data to empty.
        /// </summary>
        public BitSchedule()
        {
            scheduleData = new List<BitDay>();
        }

        public BitSchedule(BitScheduleConfiguration configuration)
        {
            _configuration = configuration;
            _mockData = new MockData();
            scheduleData = _mockData.LoadMockData(_configuration);
        }

        /// <summary>
        /// Refreshes the internal schedule data using the current configuration.
        /// In a production system this would read data from a database.
        /// </summary>
        public void RefreshScheduleData()
        {
            if (_configuration != null)
            {
                scheduleData = _mockData.LoadMockData(_configuration);
            }
        }

        /// <summary>
        /// Reads the schedule from the internal data by filtering it based on the provided BitScheduleRequest.
        /// The request defines the date range, active weekdays, and (optionally) the time block.
        /// Returns a BitScheduleResponse containing the BitDay objects that fall within the request’s parameters.
        /// </summary>
        public BitScheduleResponse ReadSchedule(BitScheduleRequest request)
        {
            List<BitDay> result = new List<BitDay>();
            DateTime start = request.DateRange.StartDate.Date;
            DateTime end = request.DateRange.EndDate.Date;

            foreach (BitDay day in scheduleData)
            {
                // Only consider days within the requested date range.
                if (day.Date < start || day.Date > end)
                    continue;

                // If active weekdays are specified, only include days that match one of them.
                if (request.ActiveDays != null && request.ActiveDays.Length > 0)
                {
                    bool match = false;
                    foreach (DayOfWeek dow in request.ActiveDays)
                    {
                        if (day.Date.DayOfWeek == dow)
                        {
                            match = true;
                            break;
                        }
                    }
                    if (!match)
                        continue;
                }

                // Optionally, you might check the time block availability here as well,
                // but for this example we simply add the day.
                result.Add(day);
            }

            return new BitScheduleResponse { ScheduledDays = result };
        }

        /// <summary>
        /// Writes the schedule to the internal data by reserving the specified time block on all days
        /// that fall within the date range and match the active weekdays defined in the request.
        /// Returns true if reservations on all applicable days succeed; otherwise, returns false.
        /// </summary>
        public bool WriteSchedule(BitScheduleRequest request)
        {
            bool allSucceeded = true;

            // Convert the time block from the request into block indices.
            int startBlock = BitDay.TimeToBlockIndex(request.TimeBlock.StartTime);
            int length = (int)((request.TimeBlock.EndTime - request.TimeBlock.StartTime).TotalMinutes / 15);
            DateTime start = request.DateRange.StartDate.Date;
            DateTime end = request.DateRange.EndDate.Date;

            foreach (BitDay day in scheduleData)
            {
                // Only process days within the requested date range.
                if (day.Date < start || day.Date > end)
                    continue;

                // If active weekdays are specified, only reserve on matching days.
                if (request.ActiveDays != null && request.ActiveDays.Length > 0)
                {
                    bool isActive = false;
                    foreach (DayOfWeek dow in request.ActiveDays)
                    {
                        if (day.Date.DayOfWeek == dow)
                        {
                            isActive = true;
                            break;
                        }
                    }
                    if (!isActive)
                        continue;
                }

                // Reserve the specified time block on this day.
                if (!day.ReserveRange(startBlock, length))
                    allSucceeded = false;
            }

            return allSucceeded;
        }
    }
}
