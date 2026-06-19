namespace BitSchedulerCore.HexGrid;

public interface IHexCoordinateService
{
    (double X, double Y) LatLongToLocalMeters(double latitude, double longitude);
    (double Latitude, double Longitude) LocalMetersToLatLong(double x, double y);

    (double Q, double R) LocalMetersToFractionalAxial(double x, double y);
    (int Q, int R) LocalMetersToAxial(double x, double y);

    (double X, double Y) AxialToLocalMeters(int q, int r);
    IReadOnlyList<(double Latitude, double Longitude)> GetHexPolygon(double centerLatitude, double centerLongitude);
}
