var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireBitSchedule_ApiService>("apiservice");
var webFrontend = builder.AddProject<Projects.AspireBitSchedule_Web>("webbackend")
    .WithExternalHttpEndpoints()
    .WithEnvironment("BACKEND_BASE_URL", apiService.GetEndpoint("http"))
    .WithReference(apiService)
    .WaitFor(apiService);

var clientAppPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "AspireBitSchedule.Web", "ClientApp"));

builder.AddExecutable("angularfrontend", "npm", clientAppPath, "start")
    .WithHttpEndpoint(targetPort: 4200, port: 4200, isProxied: false)
    .WithEnvironment("BACKEND_BASE_URL", apiService.GetEndpoint("http"))
    .WaitFor(webFrontend);

builder.Build().Run();
