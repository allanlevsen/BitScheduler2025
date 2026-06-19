namespace BitSchedulerCore.Models;

public sealed record GeocodingResult(
    double Latitude,
    double Longitude,
    string FormattedAddress);
