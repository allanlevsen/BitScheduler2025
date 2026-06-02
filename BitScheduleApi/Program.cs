using BitScheduleApi.Extensions;
using BitScheduleApi.Services;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BitScheduleConnection")
    ?? throw new InvalidOperationException("Connection string 'BitScheduleConnection' was not found.");

builder.Services.AddDbContext<BitScheduleDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddBitScheduleApiServices();

var app = builder.Build();

await InitializeAsync(app.Services, app.Logger);

app.MapScheduleApi();

app.Run();

static async Task InitializeAsync(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();

    try
    {
        var initializer = scope.ServiceProvider.GetRequiredService<ApiStartupInitializer>();
        await initializer.InitializeAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database seeding.");
    }
}