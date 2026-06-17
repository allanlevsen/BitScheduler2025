using System.ComponentModel;

namespace BitSchedulerCore.Models;

/// <summary>
/// Represents the configuration for a schedule.
/// Changes to any property trigger the PropertyChanged event.
/// Consumers (like BitSchedule) can listen for these changes.
/// </summary>
public class BitScheduleConfiguration : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the resource whose schedule should be loaded and updated.
    /// </summary>
    public int BitResourceId
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged(nameof(BitResourceId));
        }
    }

    /// <summary>
    /// Gets or sets the date range for the schedule.
    /// </summary>
    public BitDateRange? DateRange
    {
        get;
        set
        {
            // You may wish to implement a proper Equals on BitDateRange.
            if (field != null && field.Equals(value)) return;

            field = value;
            OnPropertyChanged(nameof(DateRange));
        }
    }

    /// <summary>
    /// Gets or sets the active days for the schedule.
    /// </summary>
    public DayOfWeek[]? ActiveDays
    {
        get;
        set
        {
            var normalizedValue = value ?? [];
            if (field.SequenceEqual(normalizedValue)) return;

            field = normalizedValue;
            OnPropertyChanged(nameof(ActiveDays));
        }
    } = [];

    /// <summary>
    /// Gets or sets the time block for the schedule.
    /// </summary>
    public BitTimeRange? TimeBlock
    {
        get;
        set
        {
            // Again, consider implementing a proper Equals on BitTimeRange.
            if (field != null && field.Equals(value)) return;

            field = value;
            OnPropertyChanged(nameof(TimeBlock));
        }
    }

    /// <summary>
    /// Gets or sets whether a change in configuration automatically triggers a refresh.
    /// </summary>
    public bool AutoRefreshOnConfigurationChange
    {
        get;
        init
        {
            if (field == value) return;
            field = value;
            OnPropertyChanged(nameof(AutoRefreshOnConfigurationChange));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        // Raise the event if any subscribers exist.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
