using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Shared.HealthChecks;

public class WorkerHealthCheck : IHealthCheck
{
    private readonly ILogger<WorkerHealthCheck> _logger;
    private DateTime _lastProcessedTime = DateTime.UtcNow;
    private int _totalProcessed = 0;
    private int _totalErrors = 0;

    public WorkerHealthCheck(ILogger<WorkerHealthCheck> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var timeSinceLastProcessing = DateTime.UtcNow - _lastProcessedTime;
            var errorRate = _totalProcessed > 0 ? (double)_totalErrors / _totalProcessed : 0;

            var data = new Dictionary<string, object>
            {
                { "total_processed", _totalProcessed },
                { "total_errors", _totalErrors },
                { "error_rate", $"{errorRate:P2}" },
                { "last_processed", _lastProcessedTime },
                { "time_since_last_processing", timeSinceLastProcessing }
            };

            if (timeSinceLastProcessing.TotalMinutes > 60 && _totalProcessed > 0)
                return Task.FromResult(HealthCheckResult.Degraded("No messages processed recently", data: data));

            if (errorRate > 0.5 && _totalProcessed > 10)
                return Task.FromResult(HealthCheckResult.Unhealthy("High error rate detected", data: data));

            return Task.FromResult(HealthCheckResult.Healthy("Worker is healthy", data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker health check failed");

            return Task.FromResult(HealthCheckResult.Unhealthy("Worker health check failed", ex));
        }
    }

    public void RecordProcessing()
    {
        _lastProcessedTime = DateTime.UtcNow;
        Interlocked.Increment(ref _totalProcessed);
    }

    public void RecordError()
    {
        Interlocked.Increment(ref _totalErrors);
    }
}
