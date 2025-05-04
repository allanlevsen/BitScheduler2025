
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BitSchedulerCore;
using BitTimeScheduler.Models;
using Microsoft.EntityFrameworkCore;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitTimeScheduler;

namespace BitSchedulerCore.Services
{

    public class BitScheduleDataService
    {
        private readonly BitScheduleDbContext _dbContext;

        public BitScheduleDataService(BitScheduleDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public List<BitDay> LoadScheduleData(BitScheduleConfiguration config, int clientId)
        {
            // Check if any BitDay records exist for this month.
            var bitData = _dbContext.BitDays
                .Where(d => d.Date >= config.DateRange.StartDate &&
                            d.Date <= config.DateRange.EndDate &&
                            d.ClientId == clientId)
                .ToList();
            return bitData;
        }


    }
}

