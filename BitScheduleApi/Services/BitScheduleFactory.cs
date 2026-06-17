using BitSchedulerCore;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;

namespace BitScheduleApi.Services;

internal sealed class BitScheduleFactory(BitScheduleDataService dataService, BitScheduleDbContext dbContext, ILogger<BitSchedule> logger)
{
    private const int DefaultClientId = 1;

    public int DefaultClient => DefaultClientId;

    public BitSchedule Create(BitScheduleConfiguration configuration, int clientId = DefaultClientId)
    {
        return new BitSchedule(clientId, configuration, dataService, dbContext, logger);
    }
}
