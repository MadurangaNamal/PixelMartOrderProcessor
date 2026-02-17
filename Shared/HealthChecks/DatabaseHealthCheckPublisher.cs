using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Shared.Data;
using Shared.Models;
using System.Text.Json;

namespace Shared.HealthChecks;

public class DatabaseHealthCheckPublisher : IHealthCheckPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseHealthCheckPublisher> _logger;
    private readonly string _workerName;

    public DatabaseHealthCheckPublisher(IServiceProvider serviceProvider,
        ILogger<DatabaseHealthCheckPublisher> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _workerName = configuration["WorkerName"] ?? "Unknown";
    }

    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PixelMartOrderProcessorDbContext>();

            var workerCheck = report.Entries.FirstOrDefault(e => e.Key == "worker");
            var workerData = workerCheck.Value.Data;

            var healthStatus = await dbContext.WorkerHealthStatuses
                .FirstOrDefaultAsync(status =>
                status.WorkerName == _workerName, cancellationToken);

            if (healthStatus == null)
            {
                healthStatus = new WorkerHealthStatus
                {
                    Id = Guid.NewGuid(),
                    WorkerName = _workerName
                };

                dbContext.WorkerHealthStatuses.Add(healthStatus);
            }

            healthStatus.Status = workerCheck.Value.Status.ToString();
            healthStatus.LastCheckTime = DateTime.UtcNow;
            healthStatus.UpdatedAt = DateTime.UtcNow;

            // Extract worker-specific metrics
            if (workerData.TryGetValue("total_processed", out var totalProcessed))
                healthStatus.TotalProcessed = Convert.ToInt32(totalProcessed);

            if (workerData.TryGetValue("total_errors", out var totalErrors))
                healthStatus.TotalErrors = Convert.ToInt32(totalErrors);

            if (workerData.TryGetValue("error_rate", out var errorRate))
            {
                var rateStr = errorRate?.ToString() ?? "0%";
                healthStatus.ErrorRate = double.Parse(rateStr.Replace("%", "")) / 100;
            }

            var details = new
            {
                totalDuration = report.TotalDuration.TotalMilliseconds,
                entries = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    data = e.Value.Data
                })
            };

            healthStatus.Details = JsonSerializer.Serialize(details);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Published health status for {WorkerName}: {Status}", _workerName, report.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish health status to database");
        }
    }
}


