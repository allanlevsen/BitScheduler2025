using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
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
        // Internal schedule data—stored as a list of BitDay objects.
        private List<BitDay> scheduleData;

        // The current configuration.
        private BitScheduleConfiguration _configuration;
        private BitScheduleDataService _data;

        public bool IsDirty { get; set; } = false;
        public DateTime LastRefreshed { get; set; }

        /// <summary>
        /// Gets or sets the schedule configuration.
        /// If AutoRefreshOnConfigurationChange is true, setting a new configuration triggers a data refresh
        /// only if any of the configuration properties (DateRange, ActiveDays, or TimeBlock) have changed.
        /// </summary>
        public BitScheduleConfiguration Configuration
        {
            get { return _configuration; }
            set
            {
                // Unsubscribe from old configuration events if present.
                if (_configuration != null)
                {
                    _configuration.PropertyChanged -= OnConfigurationChanged;
                }
                bool hasChanged = false;


                // If no configuration was previously set, then it's a change.
                if (_configuration == null)
                {
                    hasChanged = true;
                }

                // If the new configuration is null, treat that as a change.
                if (value == null)
                {
                    hasChanged = true;
                }

                // Compare DateRange.
                if (!_configuration.DateRange.StartDate.Equals(value.DateRange.StartDate) ||
                    !_configuration.DateRange.EndDate.Equals(value.DateRange.EndDate))
                {
                    hasChanged = true;
                }

                // Compare ActiveDays arrays.
                if ((_configuration.ActiveDays == null && value.ActiveDays != null) ||
                    (_configuration.ActiveDays != null && value.ActiveDays == null))
                {
                    hasChanged = true;
                }

                if (_configuration.ActiveDays != null && value.ActiveDays != null)
                {
                    if (_configuration.ActiveDays.Length != value.ActiveDays.Length)
                    {
                        hasChanged = true;
                    }
                    else
                    {
                        for (int i = 0; i < _configuration.ActiveDays.Length; i++)
                        {
                            if (_configuration.ActiveDays[i] != value.ActiveDays[i])
                            {
                                hasChanged = true;
                                break;
                            }
                        }
                    }
                }

                if (_configuration.AutoRefreshOnConfigurationChange != value.AutoRefreshOnConfigurationChange)
                {
                    hasChanged = true;
                }

                if (!_configuration.TimeBlock.StartTime.Equals(value.TimeBlock.StartTime) ||
                            !_configuration.TimeBlock.EndTime.Equals(value.TimeBlock.EndTime))
                {
                    hasChanged = true;
                }

                // Update the configuration.
                _configuration = value;
                IsDirty = hasChanged;

                // Subscribe to the new configuration's change events.
                if (_configuration != null)
                {
                    _configuration.PropertyChanged += OnConfigurationChanged;
                }

                // Refresh schedule data only if a change was detected and the auto-refresh flag is enabled.
                if (hasChanged && value.AutoRefreshOnConfigurationChange)
                {
                    RefreshScheduleData();
                }
            }
        }

        // Event handler for property changes on the configuration.
        protected void OnConfigurationChanged(object sender, PropertyChangedEventArgs e)
        {
            // Mark the schedule as dirty whenever any property of the configuration changes.
            IsDirty = true;

            // Optionally, immediately refresh if auto-refresh is enabled.
            if (_configuration.AutoRefreshOnConfigurationChange)
            {
                RefreshScheduleData();
            }
        }

        /// <summary>
        /// Empty constructor. Initializes the internal schedule data to empty.
        /// </summary>
        public BitSchedule()
        {
            scheduleData = new List<BitDay>();
            _configuration = new BitScheduleConfiguration();
            _configuration.PropertyChanged += OnConfigurationChanged;
            LastRefreshed = DateTime.MinValue;
        }

        public BitSchedule(BitScheduleConfiguration configuration)
        {
            _configuration = configuration;
            _configuration.PropertyChanged += OnConfigurationChanged;
            ReadScheduleDataFromDatabase();
        }

        /// <summary>
        /// Refreshes the internal schedule data using the current configuration.
        /// In a production system this would read data from a database.
        /// </summary>
        public void ReadScheduleDataFromDatabase()
        {
            if (_configuration != null)
            {
                scheduleData = _data.LoadScheduleData(_configuration, 1);
                LastRefreshed = DateTime.Now;
                IsDirty = false;
            }
        }


        /// <summary>
        /// Writes (updates) a specific day's schedule.
        /// It accepts a BitDayRequest containing a date and a time block (start and end times),
        /// reserves that time block on the specified day, and returns the updated BitDay.
        /// If the day does not exist in the internal schedule data, it creates a new BitDay,
        /// reserves the specified range, adds it to the schedule data, and returns it.
        /// </summary>
        public BitDay WriteDay(BitDayRequest request)
        {
            // Find an existing BitDay in the internal data for the requested date.
            var day = scheduleData.FirstOrDefault(d => d.Date.Date == request.Date.Date);
            if (day == null)
            {
                // If not found, create a new BitDay for the given date.
                day = new BitDay(request.Date);
                scheduleData.Add(day);
            }

            // Convert the provided times into block indices.
            int startBlock = BitDay.TimeToBlockIndex(request.StartTime);
            int length = (int)((request.EndTime - request.StartTime).TotalMinutes / 15);

            // Reserve the specified time block on the day.
            // You might want to handle a failure (if ReserveRange returns false) as needed.
            day.ReserveRange(startBlock, length);

            return day;
        }

        /// <summary>
        /// Reads a specific day's schedule from the internal data.
        /// If the day is not found, returns a new BitDay with the given date (which is free, with bitsLow and bitsHigh set to 0).
        /// </summary>
        public BitDay ReadDay(DateTime date)
        {
            var day = scheduleData.FirstOrDefault(d => d.Date.Date == date.Date);
            if (day == null)
            {
                // Return a new, free BitDay for the given date.
                return new BitDay(date);
            }
            return day;
        }

        /// <summary>
        /// Reads the schedule from the ***internal data*** by filtering it based on the provided BitScheduleRequest.
        /// The request defines the date range, active weekdays, and (optionally) the time block.
        /// Returns a BitScheduleResponse containing the BitDay objects that fall within the request’s parameters.
        /// </summary>
        public BitScheduleResponse ReadSchedule(BitScheduleRequest request)
        {
            // Filter internal scheduleData based on the request's date range and active weekdays.
            List<BitDay> filteredDays = new List<BitDay>();
            DateTime start = request.DateRange.StartDate.Date;
            DateTime end = request.DateRange.EndDate.Date;

            foreach (BitDay day in scheduleData)
            {
                if (day.Date < start || day.Date > end)
                    continue;

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
                filteredDays.Add(day);
            }

            // Group the filtered days by year and month.
            var monthGroups = filteredDays
                .GroupBy(d => new { d.Date.Year, d.Date.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month);

            List<BitMonth> months = new List<BitMonth>();

            // For each group, create a BitMonth using a helper method.
            foreach (var group in monthGroups)
            {
                BitMonth month = BitMonth.CreateFromDays(group.ToList());
                months.Add(month);
            }

            // Return the response with the original request and the grouped months.
            return new BitScheduleResponse
            {
                Request = request,
                ScheduledMonths = months
            };
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
