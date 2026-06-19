using BitScheduleApi.Services;
using BitSchedulerCore.HexGrid;
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
        services.AddSingleton<IHexCoordinateService>(_ => new HexCoordinateService(HexGridServiceAreas.EdmontonMetro));
        services.AddSingleton<HexGridTableBuilder>();
        services.AddSingleton<IHexGridLookupProvider, HexGridLookupProvider>();
        services.AddScoped<IHexGridGenerationService, HexGridGenerationService>();
        services.AddScoped<IHexGridTableService, HexGridTableService>();
        services.AddScoped<IHexGridSearchService, HexGridSearchService>();

        return services;
    }
}
