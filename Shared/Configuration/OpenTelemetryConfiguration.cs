using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Shared.Constants;
using System.Diagnostics;

namespace Shared.Configuration;

public static class OpenTelemetryConfiguration
{
    public const string ServiceName = AppConstants.ApplicationName;
    public static readonly ActivitySource ActivitySource = new(ServiceName);

    public static IServiceCollection AddPixelMartTelemetry(
        this IServiceCollection services,
        string serviceName,
        IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: "1.0.0",
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddSource(ServiceName)
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.EnrichWithHttpRequest = (activity, request) =>
                    {
                        activity.SetTag("http.request_content_length", request.ContentLength);
                        activity.SetTag("http.request_content_type", request.ContentType);
                    };
                })
                .AddHttpClientInstrumentation(options =>
                {
                    options.RecordException = true;
                })
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.EnrichWithIDbCommand = (activity, command) =>
                    {
                        activity?.SetTag("db.statement", command.CommandText);
                    };
                })
                .AddConsoleExporter() // For development
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }))
            .WithMetrics(metrics => metrics
                .AddMeter(ServiceName)
                .AddRuntimeInstrumentation()
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddConsoleExporter()
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }));

        return services;
    }
}
