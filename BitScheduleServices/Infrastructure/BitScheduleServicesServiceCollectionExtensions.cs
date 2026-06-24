using BitScheduleServices.Features.Clients;
using BitScheduleServices.Features.Configuration;
using BitScheduleServices.Features.Events;
using BitScheduleServices.Features.HexGrid;
using BitScheduleServices.Features.Locations;
using BitScheduleServices.Features.Resources;
using BitScheduleServices.Features.ResourceTypes;
using BitScheduleServices.Features.Schedule;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BitScheduleServices.Infrastructure;

public static class BitScheduleServicesServiceCollectionExtensions
{
    public static IServiceCollection AddBitScheduleServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GoogleGeocodingOptions>(
            configuration.GetSection(GoogleGeocodingOptions.SectionName));
        services.PostConfigure<GoogleGeocodingOptions>(options =>
        {
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                options.ApiKey = configuration["GoogleMapping:ApiKey"] ?? string.Empty;
            }

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
        services.AddHttpContextAccessor();
        services.AddHttpClient<IGeocodingService, GoogleGeocodingService>();
        services.AddScoped<IAddressLocationService, AddressLocationService>();
        services.AddScoped<IBitClientService, BitClientService>();
        services.AddScoped<IBitEventService, BitEventService>();
        services.AddScoped<IBitResourceService, BitResourceService>();
        services.AddScoped<IBitResourceTypeService, BitResourceTypeService>();
        services.AddScoped<ICurrentBitClientAccessor, SessionBitClientAccessor>();
        services.AddScoped<BitScheduleFactory>();
        services.AddScoped<ClientFeatureService>();
        services.AddScoped<ScheduleFeatureService>();
        services.AddScoped<EventFeatureService>();
        services.AddScoped<ResourceFeatureService>();
        services.AddScoped<ResourceTypeFeatureService>();
        services.AddScoped<HexGridFeatureService>();
        services.AddScoped<LocationFeatureService>();
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
