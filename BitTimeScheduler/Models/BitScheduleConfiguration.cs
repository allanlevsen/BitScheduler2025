using System;
using System.ComponentModel;
using System.Linq;

namespace BitTimeScheduler.Models
{
    /// <summary>
    /// Represents the configuration for a schedule.
    /// Changes to any property trigger the PropertyChanged event.
    /// Consumers (like BitSchedule) can listen for these changes.
    /// </summary>
    public class BitScheduleConfiguration : INotifyPropertyChanged
    {
        private BitDateRange _dateRange;
        /// <summary>
        /// Gets or sets the date range for the schedule.
        /// </summary>
        public BitDateRange DateRange
        {
            get => _dateRange;
            set
            {
                // You may wish to implement a proper Equals on BitDateRange.
                if (_dateRange == null || !_dateRange.Equals(value))
                {
                    _dateRange = value;
                    OnPropertyChanged(nameof(DateRange));
                }
            }
        }

        private DayOfWeek[] _activeDays;
        /// <summary>
        /// Gets or sets the active days for the schedule.
        /// </summary>
        public DayOfWeek[] ActiveDays
        {
            get => _activeDays;
            set
            {
                // Compare lengths and then each element. (Order matters; adjust as needed.)
                if ((_activeDays == null && value != null) ||
                    (_activeDays != null && value == null) ||
                    (_activeDays != null && value != null && !_activeDays.SequenceEqual(value)))
                {
                    _activeDays = value;
                    OnPropertyChanged(nameof(ActiveDays));
                }
            }
        }

        private BitTimeRange _timeBlock;
        /// <summary>
        /// Gets or sets the time block for the schedule.
        /// </summary>
        public BitTimeRange TimeBlock
        {
            get => _timeBlock;
            set
            {
                // Again, consider implementing a proper Equals on BitTimeRange.
                if (_timeBlock == null || !_timeBlock.Equals(value))
                {
                    _timeBlock = value;
                    OnPropertyChanged(nameof(TimeBlock));
                }
            }
        }

        private bool _autoRefreshOnConfigurationChange;
        /// <summary>
        /// Gets or sets whether a change in configuration automatically triggers a refresh.
        /// </summary>
        public bool AutoRefreshOnConfigurationChange
        {
            get => _autoRefreshOnConfigurationChange;
            set
            {
                if (_autoRefreshOnConfigurationChange != value)
                {
                    _autoRefreshOnConfigurationChange = value;
                    OnPropertyChanged(nameof(AutoRefreshOnConfigurationChange));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            // Raise the event if any subscribers exist.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}