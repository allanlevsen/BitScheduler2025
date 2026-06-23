using BitScheduleServices.Features.Clients;
using BitSchedulerCore.Models;

namespace AspireBitSchedule.ApiService.Features.Clients;

internal static class ClientEndpoints
{
    public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/clients");
        group.MapGet("/", ListClientsAsync);
        group.MapGet("/{bitClientId:int}", GetClientAsync);
        group.MapPost("/", CreateClientAsync);
        group.MapGet("/current", GetCurrentClientAsync);
        group.MapPost("/current", SetCurrentClientAsync);
        group.MapPut("/{bitClientId:int}", UpdateClientAsync);
        group.MapDelete("/{bitClientId:int}", DeleteClientAsync);

        endpoints.MapGet("/Clients", ListClientsAsync);
        endpoints.MapGet("/Clients/{bitClientId:int}", GetClientAsync);
        endpoints.MapPost("/Clients", CreateClientAsync);
        endpoints.MapGet("/Clients/Current", GetCurrentClientAsync);
        endpoints.MapPost("/Clients/Current", SetCurrentClientAsync);
        endpoints.MapPut("/Clients/{bitClientId:int}", UpdateClientAsync);
        endpoints.MapDelete("/Clients/{bitClientId:int}", DeleteClientAsync);

        return endpoints;
    }

    private static Task<IResult> GetClientAsync(
        int bitClientId,
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.GetClientAsync(bitClientId, logger, cancellationToken);
    }

    private static Task<IResult> ListClientsAsync(
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.ListClientsAsync(logger, cancellationToken);
    }

    private static Task<IResult> CreateClientAsync(
        BitClientRequest request,
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.CreateClientAsync(request, logger, cancellationToken);
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

    private static Task<IResult> UpdateClientAsync(
        int bitClientId,
        BitClientRequest request,
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.UpdateClientAsync(bitClientId, request, logger, cancellationToken);
    }

    private static Task<IResult> DeleteClientAsync(
        int bitClientId,
        ClientFeatureService featureService,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        return featureService.DeleteClientAsync(bitClientId, logger, cancellationToken);
    }
}
