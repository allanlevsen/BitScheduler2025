using BitSchedulerCore.Data.BitTimeScheduler.Data;
using BitSchedulerCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BitScheduleServices.Infrastructure;

public sealed class ApiStartupInitializer(
    SeedingService seedingService,
    BitScheduleDbContext dbContext,
    IHexGridLookupProvider hexGridLookupProvider,
    IHostEnvironment hostEnvironment,
    ILogger<ApiStartupInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting database seeding on startup...");

        await EnsureDatabaseAsync(cancellationToken);

        logger.LogInformation("Seeding initial ResourceTypes and Clients...");
        await seedingService.SeedAsync();

        logger.LogInformation("Seeding initial Schedule Data (BitDays)...");
        await seedingService.SeedScheduleDataAsync();

        logger.LogInformation("Database seeding completed successfully.");

        logger.LogInformation("Loading active hex grid lookup...");
        await hexGridLookupProvider.ReloadAsync(cancellationToken);
        logger.LogInformation("Hex grid lookup loading completed.");
    }

    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Applying migrations completed.");
        }
        catch (InvalidOperationException ex) when (
            hostEnvironment.IsDevelopment() &&
            ex.Message.Contains("PendingModelChangesWarning", StringComparison.Ordinal))
        {
            logger.LogWarning(
                ex,
                "Migrations were blocked by pending model changes. Recreating the development database from the current EF model.");

            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            logger.LogInformation("Database created from the current EF model.");
        }
    }
}
