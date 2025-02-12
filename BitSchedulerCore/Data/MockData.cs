using BitTimeScheduler.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitTimeScheduler.Data
{
    public class MockData
    {


        /// <summary>
        /// Generates mock schedule data based on the configuration.
        /// For each day in the date range, a new BitDay is created.
        /// If the day is active (i.e. its DayOfWeek is in ActiveDays), a few random reservations are made.
        /// </summary>
        public List<BitDay> LoadMockData(BitScheduleConfiguration config)
        {
            List<BitDay> data = new List<BitDay>();
            Random rand = new Random();
            DateTime startDate = config.DateRange.StartDate.Date;
            DateTime endDate = config.DateRange.EndDate.Date;
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                BitDay day = new BitDay(date);
                if (config.ActiveDays != null && config.ActiveDays.Length > 0)
                {
                    bool isActive = false;
                    foreach (DayOfWeek dow in config.ActiveDays)
                    {
                        if (date.DayOfWeek == dow)
                        {
                            isActive = true;
                            break;
                        }
                    }
                    if (isActive)
                    {
                        // Create between 1 and 3 random reservations.
                        int reservations = rand.Next(1, 4);
                        for (int i = 0; i < reservations; i++)
                        {
                            int startBlock = rand.Next(0, BitDay.TotalSlots - 4);
                            int length = rand.Next(1, 5);
                            day.ReserveRange(startBlock, length);
                        }
                    }
                }
                data.Add(day);
            }
            return data;
        }

    }
}
