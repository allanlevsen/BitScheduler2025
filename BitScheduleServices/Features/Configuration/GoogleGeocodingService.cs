using System.Text.Json.Serialization;
using BitSchedulerCore.Models;
using BitSchedulerCore.Services;
using Microsoft.Extensions.Options;

namespace BitScheduleServices.Features.Configuration;

public sealed class GoogleGeocodingService(
    HttpClient httpClient,
    IOptions<GoogleGeocodingOptions> options,
    ILogger<GoogleGeocodingService> logger) : IGeocodingService
{
    public async Task<GeocodingResult?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning("Google geocoding was requested, but no API key is configured.");
            return null;
        }

        var uri = BuildUri(settings, address.Trim());
        var response = await httpClient.GetFromJsonAsync<GoogleGeocodeResponse>(uri, cancellationToken);

        if (response is null)
        {
            logger.LogWarning("Google geocoding returned an empty response for address '{Address}'.", address);
            return null;
        }

        if (!string.Equals(response.Status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Google geocoding returned status {Status} for address '{Address}'. Error: {ErrorMessage}",
                response.Status,
                address,
                response.ErrorMessage);
            return null;
        }

        var firstResult = response.Results?.FirstOrDefault();
        if (firstResult?.Geometry?.Location is null)
        {
            logger.LogWarning("Google geocoding did not return coordinates for address '{Address}'.", address);
            return null;
        }

        return new GeocodingResult(
            firstResult.Geometry.Location.Lat,
            firstResult.Geometry.Location.Lng,
            firstResult.FormattedAddress ?? address);
    }

    private static Uri BuildUri(GoogleGeocodingOptions options, string address)
    {
        var query = new List<string>
        {
            $"address={Uri.EscapeDataString(address)}",
            $"key={Uri.EscapeDataString(options.ApiKey)}"
        };

        if (!string.IsNullOrWhiteSpace(options.Region))
        {
            query.Add($"region={Uri.EscapeDataString(options.Region)}");
        }

        if (!string.IsNullOrWhiteSpace(options.Language))
        {
            query.Add($"language={Uri.EscapeDataString(options.Language)}");
        }

        return new Uri($"{options.BaseUrl}?{string.Join("&", query)}", UriKind.Absolute);
    }

    private sealed class GoogleGeocodeResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("results")]
        public List<GoogleGeocodeResult>? Results { get; set; }
    }

    private sealed class GoogleGeocodeResult
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("geometry")]
        public GoogleGeocodeGeometry? Geometry { get; set; }
    }

    private sealed class GoogleGeocodeGeometry
    {
        [JsonPropertyName("location")]
        public GoogleGeocodeLocation? Location { get; set; }
    }

    private sealed class GoogleGeocodeLocation
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}
