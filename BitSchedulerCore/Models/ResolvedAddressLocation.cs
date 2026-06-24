namespace BitSchedulerCore.Models;

public sealed record ResolvedAddressLocation(
    string Address,
    double Latitude,
    double Longitude,
    int? HexGridId);
