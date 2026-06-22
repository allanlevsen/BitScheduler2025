namespace BitSchedulerCore.Models;

public sealed class HexCoordinateService : IHexCoordinateService
{
    private const double EarthRadiusMeters = 6378137d;
    private readonly double _originLatitude;
    private readonly double _originLongitude;
    private readonly double _originLatitudeRadians;
    private readonly double _hexRadiusMeters;

    public HexCoordinateService(HexGridGenerationOptions options)
        : this(options.OriginLatitude, options.OriginLongitude, options.HexRadiusMeters)
    {
    }

    public HexCoordinateService(double originLatitude, double originLongitude, double hexRadiusMeters)
    {
        if (hexRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hexRadiusMeters), "Hex radius must be greater than zero.");
        }

        _originLatitude = originLatitude;
        _originLongitude = originLongitude;
        _originLatitudeRadians = DegreesToRadians(originLatitude);
        _hexRadiusMeters = hexRadiusMeters;
    }

    public (double X, double Y) LatLongToLocalMeters(double latitude, double longitude)
    {
        var x = DegreesToRadians(longitude - _originLongitude) * EarthRadiusMeters * Math.Cos(_originLatitudeRadians);
        var y = DegreesToRadians(latitude - _originLatitude) * EarthRadiusMeters;

        return (x, y);
    }

    public (double Latitude, double Longitude) LocalMetersToLatLong(double x, double y)
    {
        var latitude = _originLatitude + RadiansToDegrees(y / EarthRadiusMeters);
        var longitude = _originLongitude + RadiansToDegrees(x / (EarthRadiusMeters * Math.Cos(_originLatitudeRadians)));

        return (latitude, longitude);
    }

    public (double Q, double R) LocalMetersToFractionalAxial(double x, double y)
    {
        var q = ((Math.Sqrt(3d) / 3d) * x - (1d / 3d) * y) / _hexRadiusMeters;
        var r = ((2d / 3d) * y) / _hexRadiusMeters;

        return (q, r);
    }

    public (int Q, int R) LocalMetersToAxial(double x, double y)
    {
        var fractional = LocalMetersToFractionalAxial(x, y);
        return RoundAxial(fractional.Q, fractional.R);
    }

    public (double X, double Y) AxialToLocalMeters(int q, int r)
    {
        var x = _hexRadiusMeters * Math.Sqrt(3d) * (q + r / 2d);
        var y = _hexRadiusMeters * 1.5d * r;

        return (x, y);
    }

    public IReadOnlyList<(double Latitude, double Longitude)> GetHexPolygon(double centerLatitude, double centerLongitude)
    {
        var center = LatLongToLocalMeters(centerLatitude, centerLongitude);
        var vertices = new List<(double Latitude, double Longitude)>(6);

        for (var index = 0; index < 6; index++)
        {
            var angleRadians = DegreesToRadians(30d + 60d * index);
            var x = center.X + _hexRadiusMeters * Math.Cos(angleRadians);
            var y = center.Y + _hexRadiusMeters * Math.Sin(angleRadians);
            vertices.Add(LocalMetersToLatLong(x, y));
        }

        return vertices;
    }

    public (int Q, int R) LatLongToAxial(double latitude, double longitude)
    {
        var local = LatLongToLocalMeters(latitude, longitude);
        return LocalMetersToAxial(local.X, local.Y);
    }

    private static (int Q, int R) RoundAxial(double q, double r)
    {
        var x = q;
        var z = r;
        var y = -x - z;

        var rx = Math.Round(x);
        var ry = Math.Round(y);
        var rz = Math.Round(z);

        var xDiff = Math.Abs(rx - x);
        var yDiff = Math.Abs(ry - y);
        var zDiff = Math.Abs(rz - z);

        if (xDiff > yDiff && xDiff > zDiff)
        {
            rx = -ry - rz;
        }
        else if (yDiff > zDiff)
        {
            return ((int)rx, (int)Math.Round(-rx - rz));
        }
        else
        {
            rz = -rx - ry;
        }

        return ((int)rx, (int)rz);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180d / Math.PI;
    }
}
