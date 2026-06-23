using BitSchedulerCore.Data.BitTimeScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BitSchedulerCore.Data;

public sealed class BitScheduleDesignTimeDbContextFactory : IDesignTimeDbContextFactory<BitScheduleDbContext>
{
    public BitScheduleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BitScheduleDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=BitScheduler;Username=postgres;Password=app1e4u!");

        return new BitScheduleDbContext(optionsBuilder.Options);
    }
}
