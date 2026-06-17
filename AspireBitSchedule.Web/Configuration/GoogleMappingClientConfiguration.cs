namespace AspireBitSchedule.Web.Configuration;

public sealed record GoogleMappingClientConfiguration(
    string ApiKey,
    string MapId,
    string Region,
    string Language,
    IReadOnlyList<string> Libraries,
    GoogleMapCenterConfiguration DefaultCenter,
    int DefaultZoom);

public sealed record GoogleMapCenterConfiguration(
    double Latitude,
    double Longitude);
