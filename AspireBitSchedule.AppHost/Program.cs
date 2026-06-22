var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireBitSchedule_ApiService>("apiservice");
var frontend = builder.AddProject<Projects.AspireBitSchedule_Web>("frontend")
    .WithEnvironment("BACKEND_BASE_URL", apiService.GetEndpoint("http"))
    .WithReference(apiService)
    .WaitFor(apiService);

var clientAppPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "AspireBitSchedule.Web", "ClientApp"));

builder.AddExecutable("angular-devserver", "npm", clientAppPath, "start")
    .WithHttpEndpoint(targetPort: 4200, port: 4200, isProxied: false)
    .WithEnvironment("BACKEND_BASE_URL", apiService.GetEndpoint("http"))
    .WaitFor(apiService);

builder.Build().Run();
