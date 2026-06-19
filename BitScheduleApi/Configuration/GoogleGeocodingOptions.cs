namespace BitScheduleApi.Configuration;

public sealed class GoogleGeocodingOptions
{
    public const string SectionName = "GoogleGeocoding";

    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://maps.googleapis.com/maps/api/geocode/json";

    public string Region { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;
}
