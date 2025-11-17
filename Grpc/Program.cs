using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Core.Strategies;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Grpc.Services;
using Jeager.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

var connectionString =
    "Host=host.docker.internal;Port=5435;Database=kong;Username=kong;Password=kongpass;SSL Mode=Prefer;Trust Server Certificate=true;";
builder.Services.AddDbContext<JeagerDbContext>(options =>
    options.UseNpgsql(connectionString)
);

Sdk.CreateTracerProviderBuilder()
    .AddSource("GRPC.Api")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddTelemetrySdk())
    .AddXRayTraceId()
    .AddAWSInstrumentation()
    .AddAspNetCoreInstrumentation(options =>
    {
        options.EnrichWithHttpRequest = (activity, request) =>
        {
            activity.SetTag("http.request.header.user-agent", request.Headers["User-Agent"].ToString());
            activity.SetTag("http.request.header.requestor", request.Headers["RequestorId"].ToString());

            // Add method and path (already included by default, but can customize)
            activity.SetTag("http.method", request.Method);
            activity.SetTag("http.path", request.Path);
        };
        options.EnrichWithHttpResponse = (activity, httpResponse) =>
        {
            activity.SetTag("http.response.status_code", httpResponse.StatusCode);
        };
        options.EnrichWithException = (activity, ex) => { activity.SetTag("http.response.exception", ex.Message); };
    })
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(options => options.Endpoint = new Uri("http://host.docker.internal:4317"))
    .Build();
Sdk.CreateMeterProviderBuilder()
    .AddMeter("adot")
    .AddOtlpExporter()
    .Build();

Sdk.SetDefaultTextMapPropagator(new AWSXRayPropagator());

// Add services to the container.
builder.Services.AddGrpc();

// builder.Services
//     .AddOpenTelemetry()
//     .ConfigureResource(rs => rs.AddService("Grpc.Api"))
//     .WithTracing(trace =>
//     {
//         trace.AddAspNetCoreInstrumentation()
//             .AddHttpClientInstrumentation()
//             .AddNpgsql()
//             .AddSource("Grpc.Api")
//             .AddOtlpExporter(opt => opt.Endpoint = new Uri("http://localhost:4317"));
//     });
var app = builder.Build();
app.Use(async (context, next) =>
{
    AWSXRayRecorder.Instance.BeginSegment("Notification");
    try
    {
        await next();
    }
    finally
    {
        AWSXRayRecorder.Instance.EndSegment();
    }
});
// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.Run();