using BitScheduleServices.Features.Configuration;
using BitScheduleServices.Features.Events;
using BitScheduleServices.Features.HexGrid;
using BitScheduleServices.Features.Schedule;
using BitSchedulerCore.HexGrid;
using BitSchedulerCore.Services;

namespace BitScheduleServices.Infrastructure;

public static class BitScheduleServicesServiceCollectionExtensions
{
    public static IServiceCollection AddBitScheduleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GoogleGeocodingOptions>(
            configuration.GetSection(GoogleGeocodingOptions.SectionName));
        services.PostConfigure<GoogleGeocodingOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.Region))
            {
                options.Region = configuration["GoogleMapping:Region"] ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(options.Language))
            {
                options.Language = configuration["GoogleMapping:Language"] ?? string.Empty;
            }
        });

        services.AddScoped<BitResourceScheduleRangePayloadConverter>();
        services.AddScoped<SeedingService>();
        services.AddScoped<BitScheduleDataService>();
        services.AddHttpClient<IGeocodingService, GoogleGeocodingService>();
        services.AddScoped<IBitEventService, BitEventService>();
        services.AddScoped<BitScheduleFactory>();
        services.AddScoped<ScheduleFeatureService>();
        services.AddScoped<EventFeatureService>();
        services.AddScoped<HexGridFeatureService>();
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
