using BitTimeScheduler.Models;

namespace BitTimeScheduler
{
    /// <summary>
    /// Represents a month (for a given year and month number) containing a set of BitDay objects.
    /// Provides methods for querying and updating day availability.
    /// </summary>
    public class BitMonth
    {
        public int Year { get; private set; }
        public int Month { get; private set; }
        private BitDay[] days;

        public BitMonth()
        {
            var today = DateTime.Now;
            Initialize(today.Year, today.Month);
        }

        public BitMonth(int year, int month)
        {
            Initialize(year, month);
        }

        private void Initialize(int year, int month)
        {
            Year = year;
            Month = month;
            int daysInMonth = DateTime.DaysInMonth(year, month);
            days = new BitDay[daysInMonth];
            for (int d = 0; d < daysInMonth; d++)
            {
                DateTime date = new DateTime(year, month, d + 1);
                days[d] = new BitDay(date);
            }
        }

        public BitDay this[int day]
        {
            get
            {
                if (day < 1 || day > days.Length)
                    throw new ArgumentOutOfRangeException(nameof(day));
                return days[day - 1];
            }
        }

        /// <summary>
        /// Returns a list of available BitDay objects based on the given search criteria.
        /// If criteria.Days is empty, it returns every day in the month where the time block is available.
        /// If criteria.Days contains one day, it returns only those BitDay objects for that day.
        /// If criteria.Days contains multiple days, it returns BitDay objects from weeks where every required weekday
        /// has the time block available.
        /// </summary>
        public List<BitDay> GetAvailableDays(BitSearchCriteria criteria)
        {
            if (criteria.Days == null || criteria.Days.Length == 0)
                return GetAvailableDaysForTimeBlock(criteria);
            else if (criteria.Days.Length == 1)
                return GetAvailableDaysForSingleDay(criteria);
            else
                return GetAvailableDaysByWeekGroup(criteria);
        }

        /// <summary>
        /// Returns all days in the month where the specified time block is available.
        /// Used when no weekdays are specified.
        /// </summary>
        private List<BitDay> GetAvailableDaysForTimeBlock(BitSearchCriteria criteria)
        {
            List<BitDay> availableDays = new List<BitDay>();
            int startSlot = (int)(criteria.StartTime.TotalMinutes / 15);
            int length = (int)((criteria.EndTime - criteria.StartTime).TotalMinutes / 15);
            foreach (BitDay day in days)
            {
                if (day.IsRangeAvailable(startSlot, length))
                    availableDays.Add(day);
            }
            return availableDays;
        }

        /// <summary>
        /// Returns all days in the month that match the single required weekday in the criteria
        /// and that have the specified time block available.
        /// </summary>
        private List<BitDay> GetAvailableDaysForSingleDay(BitSearchCriteria criteria)
        {
            List<BitDay> availableDays = new List<BitDay>();
            int startSlot = (int)(criteria.StartTime.TotalMinutes / 15);
            int length = (int)((criteria.EndTime - criteria.StartTime).TotalMinutes / 15);
            foreach (BitDay day in days)
            {
                if (day.Date.DayOfWeek == criteria.Days[0] && day.IsRangeAvailable(startSlot, length))
                    availableDays.Add(day);
            }
            return availableDays;
        }

        /// <summary>
        /// For search criteria specifying multiple weekdays, returns BitDay objects from weeks where
        /// every one of the required weekdays has the entire time block available.
        /// This version uses a brute-force loop over each week in the month (using Sunday as the week start)
        /// rather than LINQ grouping.
        /// </summary>
        private List<BitDay> GetAvailableDaysByWeekGroup(BitSearchCriteria criteria)
        {
            List<BitDay> availableDays = new List<BitDay>();
            int startSlot = (int)(criteria.StartTime.TotalMinutes / 15);
            int length = (int)((criteria.EndTime - criteria.StartTime).TotalMinutes / 15);

            // Determine the first day and the last day of the month.
            DateTime firstDay = days[0].Date;
            DateTime lastDay = days[days.Length - 1].Date;

            // Calculate the first Sunday (week start) on or before the first day.
            DateTime currentWeekStart = firstDay.AddDays(-(int)firstDay.DayOfWeek);

            // Loop over weeks until currentWeekStart is beyond the last day of the month.
            while (currentWeekStart <= lastDay)
            {
                bool weekQualifies = true;
                // For each required weekday, check if there is a corresponding day in this week that is in our month and available.
                List<BitDay> qualifiedDays = new List<BitDay>();

                foreach (DayOfWeek required in criteria.Days)
                {
                    DateTime requiredDate = currentWeekStart.AddDays((int)required);
                    // The required day must fall within the month.
                    if (requiredDate < firstDay || requiredDate > lastDay)
                    {
                        weekQualifies = false;
                        break;
                    }
                    // Since our days are stored as one per day, we can index directly.
                    BitDay day = days[requiredDate.Day - 1];
                    if (!day.IsRangeAvailable(startSlot, length))
                    {
                        weekQualifies = false;
                        break;
                    }
                    else
                    {
                        qualifiedDays.Add(day);
                    }
                }

                if (weekQualifies && qualifiedDays.Count == criteria.Days.Length)
                {
                    // Add the qualifying days from this week.
                    availableDays.AddRange(qualifiedDays);
                }

                // Move to the next week.
                currentWeekStart = currentWeekStart.AddDays(7);
            }

            return availableDays;
        }
    }
}
