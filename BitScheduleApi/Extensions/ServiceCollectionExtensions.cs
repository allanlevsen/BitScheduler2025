using BitScheduleApi.Services;
using BitSchedulerCore.Services;

namespace BitScheduleApi.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBitScheduleApiServices(this IServiceCollection services)
    {
        services.AddScoped<BitResourceScheduleRangePayloadConverter>();
        services.AddScoped<SeedingService>();
        services.AddScoped<BitScheduleDataService>();
        services.AddScoped<BitScheduleFactory>();
        services.AddScoped<ApiStartupInitializer>();

        return services;
    }
}
