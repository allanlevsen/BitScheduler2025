using BitScheduleServices.Features.Configuration;
using BitScheduleServices.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspireBitSchedule.Tests;

public sealed class GoogleGeocodingOptionsConfigurationTests
{
    [Fact]
    public void AddBitScheduleServices_UsesGoogleMappingApiKeyWhenGoogleGeocodingApiKeyIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GoogleMapping:ApiKey"] = "mapping-key",
                ["GoogleMapping:Region"] = "CA",
                ["GoogleMapping:Language"] = "en"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddBitScheduleServices(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<GoogleGeocodingOptions>>().Value;

        Assert.Equal("mapping-key", options.ApiKey);
        Assert.Equal("CA", options.Region);
        Assert.Equal("en", options.Language);
    }
}
