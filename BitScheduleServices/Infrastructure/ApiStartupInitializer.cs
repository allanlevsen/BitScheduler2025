using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Services;
using Microsoft.EntityFrameworkCore;

namespace BitScheduleServices.Infrastructure;

public sealed class ApiStartupInitializer(
    SeedingService seedingService,
    BitScheduleDbContext dbContext,
    IHexGridLookupProvider hexGridLookupProvider,
    ILogger<ApiStartupInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting database seeding on startup...");

        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Applying migrations completed.");

        logger.LogInformation("Seeding initial ResourceTypes and Clients...");
        await seedingService.SeedAsync();

        logger.LogInformation("Seeding initial Schedule Data (BitDays)...");
        await seedingService.SeedScheduleDataAsync();

        logger.LogInformation("Database seeding completed successfully.");

        logger.LogInformation("Loading active hex grid lookup...");
        await hexGridLookupProvider.ReloadAsync(cancellationToken);
        logger.LogInformation("Hex grid lookup loading completed.");
    }
}
