using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Shared.Data;

namespace Shared.HealthChecks;

public class RemoteWorkerHealthCheck : IHealthCheck
{
    private readonly PixelMartOrderProcessorDbContext _dbContext;
    private readonly ILogger<RemoteWorkerHealthCheck> _logger;
    private readonly string _workerName;
    private readonly TimeSpan? _timeout;

    public RemoteWorkerHealthCheck(PixelMartOrderProcessorDbContext dbContext,
        ILogger<RemoteWorkerHealthCheck> logger,
        string workerName,
        TimeSpan? timeout = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _workerName = workerName;
        _timeout = timeout ?? TimeSpan.FromSeconds(30);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var workerStatus = await _dbContext.WorkerHealthStatuses
                .FirstOrDefaultAsync(status => status.WorkerName == _workerName, cancellationToken);

            if (workerStatus == null)
                return HealthCheckResult.Unhealthy($"Worker {_workerName} has never reported status");

            var timeSinceLastUpdate = DateTime.UtcNow - workerStatus.LastCheckTime;

            var data = new Dictionary<string, object>
            {
                { "worker_name", workerStatus.WorkerName },
                { "last_check_time", workerStatus.LastCheckTime },
                { "time_since_last_update", timeSinceLastUpdate.TotalSeconds },
                { "total_processed", workerStatus.TotalProcessed },
                { "total_errors", workerStatus.TotalErrors },
                { "error_rate", $"{workerStatus.ErrorRate:P2}" },
                { "reported_status", workerStatus.Status }
            };

            if (timeSinceLastUpdate >= _timeout)
                return HealthCheckResult.Unhealthy($"Worker {_workerName} hasn't reported in {timeSinceLastUpdate.TotalSeconds:F0}s", data: data);

            // Return the status reported by the worker
            return workerStatus.Status switch
            {
                "Healthy" => HealthCheckResult.Healthy($"Worker {_workerName} is healthy", data),
                "Degraded" => HealthCheckResult.Degraded($"Worker {_workerName} is degraded", null, data),
                _ => HealthCheckResult.Unhealthy($"Worker {_workerName} is unhealthy", null, data)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health of worker {WorkerName}", _workerName);

            return HealthCheckResult.Unhealthy($"Error checking worker {_workerName} health", ex);
        }
    }
}
