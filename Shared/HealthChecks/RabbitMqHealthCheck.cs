using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Shared.Configuration;

namespace Shared.HealthChecks;

public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqConnectionManager _connectionManager;
    private readonly ILogger<RabbitMqHealthCheck> _logger;

    public RabbitMqHealthCheck(RabbitMqConnectionManager connectionManager, ILogger<RabbitMqHealthCheck> logger)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_connectionManager.Connection == null || !_connectionManager.Connection.IsOpen)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ connection is not open");
            }

            if (_connectionManager.Channel == null || !_connectionManager.Channel.IsOpen)
            {
                return HealthCheckResult.Unhealthy("RabbitMQ channel is not open");
            }

            var data = new Dictionary<string, object>
            {
                { "connection_open", _connectionManager.Connection.IsOpen },
                { "channel_open", _connectionManager.Channel.IsOpen }
            };

            return HealthCheckResult.Healthy("RabbitMQ is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");

            return HealthCheckResult.Unhealthy("RabbitMQ health check failed", ex);
        }
    }
}
