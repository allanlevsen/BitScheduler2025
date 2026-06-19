using BitScheduleApi.Configuration;
using BitScheduleApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AspireBitSchedule.Tests;

[Trait("Category", "Integration")]
public sealed class GoogleGeocodingIntegrationTests
{
    private readonly IConfigurationRoot _configuration;

    public GoogleGeocodingIntegrationTests()
    {
        _configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
    }

    [Fact]
    public async Task GeocodeAsync_WithKnownAddress_ReachesGoogleAndReturnsCoordinates()
    {
        if (!CanRunLiveGoogleTests())
        {
            return;
        }

        var service = CreateLiveService();

        var result = await service.GeocodeAsync("10004 104 Ave NW, Edmonton, AB", CancellationToken.None);

        Assert.NotNull(result);
        Assert.InRange(result!.Latitude, 53.4, 53.7);
        Assert.InRange(result.Longitude, -113.7, -113.3);
        Assert.Contains("Edmonton", result.FormattedAddress, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GeocodeAsync_WithSecondKnownAddress_ReturnsFormattedAddress()
    {
        if (!CanRunLiveGoogleTests())
        {
            return;
        }

        var service = CreateLiveService();

        var result = await service.GeocodeAsync("10220 103 St NW, Edmonton, AB", CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.FormattedAddress));
        Assert.InRange(result.Latitude, 53.4, 53.7);
        Assert.InRange(result.Longitude, -113.7, -113.3);
    }

    private GoogleGeocodingService CreateLiveService()
    {
        if (!LiveGoogleTestsEnabled())
        {
            throw new InvalidOperationException("Live Google API tests are not enabled.");
        }

        var apiKey = _configuration["GOOGLE_GEOCODING_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _configuration["GoogleGeocoding:ApiKey"];
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("No Google geocoding API key is configured for live tests.");
        }

        var options = Options.Create(new GoogleGeocodingOptions
        {
            ApiKey = apiKey,
            Region = _configuration["GoogleGeocoding:Region"] ?? "CA",
            Language = _configuration["GoogleGeocoding:Language"] ?? "en"
        });

        return new GoogleGeocodingService(
            new HttpClient(),
            options,
            NullLogger<GoogleGeocodingService>.Instance);
    }

    private bool LiveGoogleTestsEnabled()
    {
        var enabledValue = _configuration["RUN_LIVE_GOOGLE_API_TESTS"];
        return bool.TryParse(enabledValue, out var enabled) && enabled;
    }

    private bool CanRunLiveGoogleTests()
    {
        if (!LiveGoogleTestsEnabled())
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(_configuration["GOOGLE_GEOCODING_API_KEY"]) ||
               !string.IsNullOrWhiteSpace(_configuration["GoogleGeocoding:ApiKey"]);
    }
}
