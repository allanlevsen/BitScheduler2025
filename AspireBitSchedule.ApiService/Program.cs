using AspireBitSchedule.ApiService.Features.Configuration;
using AspireBitSchedule.ApiService.Features.Events;
using AspireBitSchedule.ApiService.Features.HexGrid;
using AspireBitSchedule.ApiService.Features.Resources;
using AspireBitSchedule.ApiService.Features.Schedule;
using AspireBitSchedule.ServiceDefaults;
using BitScheduleServices.Infrastructure;
using BitSchedulerCore.Data.BitTimeScheduler.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.Configure<GoogleMappingOptions>(
    builder.Configuration.GetSection(GoogleMappingOptions.SectionName));

builder.Configuration
    .AddJsonFile(@"..\AspireBitSchedule.Web\appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($@"..\AspireBitSchedule.Web\appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false);

var connectionString = builder.Configuration.GetConnectionString("BitScheduleConnection")
    ?? throw new InvalidOperationException("Connection string 'BitScheduleConnection' was not found.");

builder.Services.AddDbContext<BitScheduleDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddBitScheduleServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler("/error");
app.Map("/error", () => Results.Problem("An unhandled error occurred."));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

await InitializeAsync(app.Services, app.Logger);

app.MapConfigurationEndpoints();
app.MapScheduleEndpoints();
app.MapEventEndpoints();
app.MapResourceEndpoints();
app.MapHexGridEndpoints();
app.MapDefaultEndpoints();

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
        logger.LogError(ex, "An error occurred during API initialization.");
    }
}
