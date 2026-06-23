using BitScheduleServices.Features.Clients;
using BitSchedulerCore.Models;

namespace AspireBitSchedule.ApiService.Features.Clients;

internal static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/clients");
        group.MapGet("/", ListClientsAsync);
        group.MapGet("/current", GetCurrentClientAsync);
        group.MapPost("/current", SetCurrentClientAsync);

        endpoints.MapGet("/Clients", ListClientsAsync);
        endpoints.MapGet("/Clients/Current", GetCurrentClientAsync);
        endpoints.MapPost("/Clients/Current", SetCurrentClientAsync);

        return endpoints;
    }

    private static Task<IResult> ListClientsAsync(
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListClientsAsync(logger, cancellationToken);
    }

    private static Task<IResult> GetCurrentClientAsync(
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.GetCurrentClientAsync(logger, cancellationToken);
    }

    private static Task<IResult> SetCurrentClientAsync(
        BitClientSelectionRequest request,
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.SetCurrentClientAsync(request, logger, cancellationToken);
    }
}
