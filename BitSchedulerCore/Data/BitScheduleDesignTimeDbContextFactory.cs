using BitSchedulerCore.Data.BitTimeScheduler.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BitSchedulerCore.Data;

public sealed class BitScheduleDesignTimeDbContextFactory : IDesignTimeDbContextFactory<BitScheduleDbContext>
{
    public BitScheduleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BitScheduleDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=BitScheduleDesignTime;Username=postgres;Password=postgres");

        return new BitScheduleDbContext(optionsBuilder.Options);
    }
}
