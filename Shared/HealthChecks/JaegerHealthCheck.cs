using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shared.HealthChecks;

public sealed class JaegerHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;

    public JaegerHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.GetAsync(
                "http://jaeger:14269/health",
                cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Jaeger is healthy")
                : HealthCheckResult.Unhealthy($"Jaeger returned HTTP {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Jaeger health check failed", ex);
        }
    }
}