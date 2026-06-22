namespace AspireBitSchedule.ApiService.Features.Configuration;

internal sealed class GoogleMappingOptions
{
    public const string SectionName = "GoogleMapping";

    public string ApiKey { get; set; } = string.Empty;

    public string MapId { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string Language { get; set; } = string.Empty;

    public string[] Libraries { get; set; } = [];

    public GoogleMapCenterOptions DefaultCenter { get; set; } = new();

    public int DefaultZoom { get; set; } = 10;
}

internal sealed class GoogleMapCenterOptions
{
    public double Latitude { get; set; } = 55.6761;

    public double Longitude { get; set; } = 12.5683;
}
