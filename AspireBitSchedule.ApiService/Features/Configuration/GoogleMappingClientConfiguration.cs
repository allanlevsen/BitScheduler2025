namespace AspireBitSchedule.ApiService.Features.Configuration;

internal sealed record GoogleMappingClientConfiguration(
    string ApiKey,
    string MapId,
    string Region,
    string Language,
    IReadOnlyList<string> Libraries,
    GoogleMapCenterConfiguration DefaultCenter,
    int DefaultZoom);

internal sealed record GoogleMapCenterConfiguration(
    double Latitude,
    double Longitude);
