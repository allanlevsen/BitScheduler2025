var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.AspireBitSchedule_ApiService>("apiservice");
var apiBitScheduleService = builder.AddProject<Projects.BitScheduleApi>("apiBitScheduleservice");
builder.AddProject<Projects.AspireBitSchedule_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiBitScheduleService)
    .WaitFor(apiService);

builder.Build().Run();
