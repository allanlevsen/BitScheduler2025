using AspireBitSchedule.ServiceDefaults;
using AspireBitSchedule.Web.Configuration;
using Microsoft.Extensions.Options;

const string angularDevServerCorsPolicy = "AngularDevServer";
const string angularDevServerHttpClient = "AngularDevServer";
const string angularDevServerUrl = "http://localhost:4200";

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();
builder.Services.AddHttpClient(angularDevServerHttpClient, client =>
{
    client.BaseAddress = new Uri(angularDevServerUrl);
    client.Timeout = Timeout.InfiniteTimeSpan;
});

builder.Services.Configure<GoogleMappingOptions>(
    builder.Configuration.GetSection(GoogleMappingOptions.SectionName));

builder.Services.AddCors(options =>
{
    options.AddPolicy(angularDevServerCorsPolicy, policy =>
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
    app.UseCors(angularDevServerCorsPolicy);

    app.MapWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        developmentApp =>
        {
            developmentApp.Run(async context =>
            {
                if (HttpMethods.IsConnect(context.Request.Method))
                {
                    context.Response.StatusCode = StatusCodes.Status501NotImplemented;
                    await context.Response.WriteAsync("CONNECT proxying is not supported on the ASP.NET development endpoint. Use the direct Angular frontend endpoint for live reload sockets.", context.RequestAborted);
                    return;
                }

                var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                using var requestMessage = CreateProxyHttpRequest(context);
                using var responseMessage = await httpClientFactory
                    .CreateClient(angularDevServerHttpClient)
                    .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

                context.Response.StatusCode = (int)responseMessage.StatusCode;

                CopyProxyResponseHeaders(context, responseMessage);

                await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
            });
        });
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

static HttpRequestMessage CreateProxyHttpRequest(HttpContext context)
{
    var requestMessage = new HttpRequestMessage();
    var requestMethod = context.Request.Method;

    if (!HttpMethods.IsGet(requestMethod) &&
        !HttpMethods.IsHead(requestMethod) &&
        !HttpMethods.IsDelete(requestMethod) &&
        !HttpMethods.IsTrace(requestMethod))
    {
        requestMessage.Content = new StreamContent(context.Request.Body);
    }

    requestMessage.Method = new HttpMethod(requestMethod);
    requestMessage.RequestUri = new Uri($"{angularDevServerUrl}{context.Request.Path}{context.Request.QueryString}");

    foreach (var header in context.Request.Headers)
    {
        if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
        {
            requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }

    requestMessage.Headers.Host = null;

    return requestMessage;
}

static void CopyProxyResponseHeaders(HttpContext context, HttpResponseMessage responseMessage)
{
    foreach (var header in responseMessage.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    foreach (var header in responseMessage.Content.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    context.Response.Headers.Remove("transfer-encoding");
}
