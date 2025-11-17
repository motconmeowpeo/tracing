using Grpc;
using Jeager.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Contrib.Extensions.AWSXRay.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    "Host=host.docker.internal;Port=5435;Database=kong;Username=kong;Password=kongpass;SSL Mode=Prefer;Trust Server Certificate=true;";
builder.Services.AddDbContext<JeagerDbContext>(options =>
    options.UseNpgsql(connectionString)
);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


Sdk.CreateTracerProviderBuilder()
    .AddSource("Jeager.Api")
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

//
// builder.Services
//     .AddOpenTelemetry()
//     .ConfigureResource(rs => rs.AddService("Jeager.Api"))
//     .WithTracing(trace =>
//     {
//         trace.AddAspNetCoreInstrumentation(options =>
//             {
//                 options.EnrichWithHttpRequest = (activity, request) =>
//                 {
//                     activity.SetTag("http.request.header.user-agent", request.Headers["User-Agent"].ToString());
//                     activity.SetTag("http.request.header.requestor", request.Headers["RequestorId"].ToString());
//
//                     // Add method and path (already included by default, but can customize)
//                     activity.SetTag("http.method", request.Method);
//                     activity.SetTag("http.path", request.Path);
//                 };
//                 options.EnrichWithHttpResponse = (activity, httpResponse) =>
//                 {
//                     activity.SetTag("http.response.status_code", httpResponse.StatusCode);
//                 };
//                 options.EnrichWithException = (activity, ex) =>
//                 {
//                     activity.SetTag("http.response.exception", ex.Message);
//                 };
//             })
//             .AddHttpClientInstrumentation()
//             .AddNpgsql()
//             .AddSource("Jeager.Api")
//             .AddOtlpExporter(opt => opt.Endpoint = new Uri("http://localhost:4317"));
//     });

builder.Services.AddGrpcClient<Greeter.GreeterClient>
        (options => options.Address = new Uri("http://xray-grpc:5131"))
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

        return handler;
    });
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();