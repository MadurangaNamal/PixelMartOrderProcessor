using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Shared.Data;

namespace Shared.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly PixelMartOrderProcessorDbContext _dbContext;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(PixelMartOrderProcessorDbContext dbContext, ILogger<DatabaseHealthCheck> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

            if (!canConnect)
                return HealthCheckResult.Unhealthy("Cannot connect to database");

            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingCount = pendingMigrations.Count();

            var data = new Dictionary<string, object>
            {
                { "database", _dbContext.Database.GetDbConnection().Database },
                { "pending_migrations", pendingCount }
            };

            if (pendingCount > 0)
            {
                return HealthCheckResult.Degraded($"Database has {pendingCount} pending migrations", data: data);
            }

            return HealthCheckResult.Healthy("Database is healthy", data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");

            return HealthCheckResult.Unhealthy("Database health check failed", ex);
        }
    }
}
