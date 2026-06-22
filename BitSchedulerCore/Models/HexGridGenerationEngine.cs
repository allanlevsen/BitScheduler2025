namespace BitSchedulerCore.Models;

public static class HexGridGenerationEngine
{
    public static IReadOnlyList<HexGridCell> GenerateCells(HexGridGenerationOptions options)
    {
        ValidateOptions(options);

        var coordinateService = new HexCoordinateService(options);
        var corners = new[]
        {
            coordinateService.LatLongToLocalMeters(options.MinLatitude, options.MinLongitude),
            coordinateService.LatLongToLocalMeters(options.MinLatitude, options.MaxLongitude),
            coordinateService.LatLongToLocalMeters(options.MaxLatitude, options.MinLongitude),
            coordinateService.LatLongToLocalMeters(options.MaxLatitude, options.MaxLongitude)
        };

        var axialCorners = corners
            .Select(corner => coordinateService.LocalMetersToFractionalAxial(corner.X, corner.Y))
            .ToArray();

        const int edgePadding = 3;
        var minQ = (int)Math.Floor(axialCorners.Min(corner => corner.Q)) - edgePadding;
        var maxQ = (int)Math.Ceiling(axialCorners.Max(corner => corner.Q)) + edgePadding;
        var minR = (int)Math.Floor(axialCorners.Min(corner => corner.R)) - edgePadding;
        var maxR = (int)Math.Ceiling(axialCorners.Max(corner => corner.R)) + edgePadding;

        var cells = new List<HexGridCell>();
        var createdUtc = DateTime.UtcNow;

        for (var q = minQ; q <= maxQ; q++)
        {
            for (var r = minR; r <= maxR; r++)
            {
                var local = coordinateService.AxialToLocalMeters(q, r);
                var center = coordinateService.LocalMetersToLatLong(local.X, local.Y);

                if (!IsInsideBoundingBox(center.Latitude, center.Longitude, options))
                {
                    continue;
                }

                var cell = new HexGridCell
                {
                    Q = q,
                    R = r,
                    CenterLatitude = center.Latitude,
                    CenterLongitude = center.Longitude,
                    HexRadiusMeters = options.HexRadiusMeters,
                    IsActive = true,
                    AreaName = options.AreaName,
                    CreatedUtc = createdUtc
                };

                if (options.IncludePolygonVertices)
                {
                    var vertices = coordinateService.GetHexPolygon(center.Latitude, center.Longitude);
                    for (var vertexIndex = 0; vertexIndex < vertices.Count; vertexIndex++)
                    {
                        cell.Vertices.Add(new HexGridCellVertex
                        {
                            VertexOrder = vertexIndex,
                            Latitude = vertices[vertexIndex].Latitude,
                            Longitude = vertices[vertexIndex].Longitude
                        });
                    }
                }

                cells.Add(cell);
            }
        }

        return cells
            .OrderBy(cell => cell.Q)
            .ThenBy(cell => cell.R)
            .ToArray();
    }

    private static void ValidateOptions(HexGridGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.AreaName))
        {
            throw new ArgumentException("AreaName is required.", nameof(options));
        }

        if (options.HexRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "HexRadiusMeters must be greater than zero.");
        }

        if (options.MinLatitude >= options.MaxLatitude)
        {
            throw new ArgumentException("MinLatitude must be less than MaxLatitude.", nameof(options));
        }

        if (options.MinLongitude >= options.MaxLongitude)
        {
            throw new ArgumentException("MinLongitude must be less than MaxLongitude.", nameof(options));
        }

        if (options.MaxPrecomputedRingDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxPrecomputedRingDistance must be zero or greater.");
        }
    }

    private static bool IsInsideBoundingBox(double latitude, double longitude, HexGridGenerationOptions options)
    {
        return latitude >= options.MinLatitude &&
               latitude <= options.MaxLatitude &&
               longitude >= options.MinLongitude &&
               longitude <= options.MaxLongitude;
    }
}
