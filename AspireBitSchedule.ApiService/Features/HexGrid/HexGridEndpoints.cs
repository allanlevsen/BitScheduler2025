using BitScheduleServices.Features.HexGrid;

namespace AspireBitSchedule.ApiService.Features.HexGrid;

internal static class HexGridEndpoints
{
    public static IEndpointRouteBuilder MapHexGridEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/hex-grid");
        group.MapPost("/generate/edmonton-metro", GenerateEdmontonMetroHexGridAsync);
        group.MapPost("/{versionId:int}/build-tables", BuildHexGridTablesAsync);
        group.MapPost("/reload", ReloadHexGridLookupAsync);
        group.MapGet("/cell", GetHexGridCell);
        group.MapGet("/{gridId:int}/neighbors", GetHexGridNeighbors);
        group.MapGet("/{gridId:int}/ring/{maxRingDistance:int}", GetHexGridRing);

        endpoints.MapPost("/HexGrid/GenerateEdmontonMetro", GenerateEdmontonMetroHexGridAsync);
        endpoints.MapPost("/HexGrid/{versionId:int}/BuildTables", BuildHexGridTablesAsync);
        endpoints.MapPost("/HexGrid/Reload", ReloadHexGridLookupAsync);
        endpoints.MapGet("/HexGrid/Cell", GetHexGridCell);
        endpoints.MapGet("/HexGrid/{gridId:int}/Neighbors", GetHexGridNeighbors);
        endpoints.MapGet("/HexGrid/{gridId:int}/Ring/{maxRingDistance:int}", GetHexGridRing);

        return endpoints;
    }

    private static Task<IResult> GenerateEdmontonMetroHexGridAsync(
        HexGridFeatureService featureService,
        CancellationToken cancellationToken)
    {
        return featureService.GenerateEdmontonMetroHexGridAsync(cancellationToken);
    }

    private static Task<IResult> BuildHexGridTablesAsync(
        int versionId,
        int? maxRingDistance,
        HexGridFeatureService featureService,
        CancellationToken cancellationToken)
    {
        return featureService.BuildHexGridTablesAsync(versionId, maxRingDistance, cancellationToken);
    }

    private static Task<IResult> ReloadHexGridLookupAsync(
        HexGridFeatureService featureService,
        CancellationToken cancellationToken)
    {
        return featureService.ReloadHexGridLookupAsync(cancellationToken);
    }

    private static IResult GetHexGridCell(
        double latitude,
        double longitude,
        HexGridFeatureService featureService)
    {
        return featureService.GetHexGridCell(latitude, longitude);
    }

    private static IResult GetHexGridNeighbors(int gridId, HexGridFeatureService featureService)
    {
        return featureService.GetHexGridNeighbors(gridId);
    }

    private static IResult GetHexGridRing(
        int gridId,
        int maxRingDistance,
        HexGridFeatureService featureService)
    {
        return featureService.GetHexGridRing(gridId, maxRingDistance);
    }
}
