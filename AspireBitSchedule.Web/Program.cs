using AspireBitSchedule.Web.Configuration;
using Microsoft.Extensions.Options;

const string AngularDevServerCorsPolicy = "AngularDevServer";

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.Configure<GoogleMappingOptions>(
    builder.Configuration.GetSection(GoogleMappingOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy(AngularDevServerCorsPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseCors(AngularDevServerCorsPolicy);
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/config/google-mapping", (IOptions<GoogleMappingOptions> options) =>
{
    var googleMapping = options.Value;

    return Results.Ok(new GoogleMappingClientConfiguration(
        googleMapping.ApiKey,
        googleMapping.MapId,
        googleMapping.Region,
        googleMapping.Language,
        googleMapping.Libraries,
        new GoogleMapCenterConfiguration(
            googleMapping.DefaultCenter.Latitude,
            googleMapping.DefaultCenter.Longitude),
        googleMapping.DefaultZoom));
});

app.MapFallbackToFile("index.html");

app.MapDefaultEndpoints();

app.Run();
