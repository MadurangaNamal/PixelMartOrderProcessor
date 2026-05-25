using Microsoft.Extensions.Configuration;
using Shared.Constants;

namespace Shared.Configuration;

public class DatabaseConfiguration
{
    protected DatabaseConfiguration()
    {
    }

    public static string GetConnectionString(IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString(AppConstants.Configuration.DefaultConnection)
            ?? throw new InvalidOperationException($"Connection string '{AppConstants.Configuration.DefaultConnection}' not found.");

        var dbPassword = configuration[AppConstants.Configuration.DbPassword]
            ?? throw new InvalidOperationException($"Database password '{AppConstants.Configuration.DbPassword}' not found in configuration.");

        var connectionString = rawConnectionString.Replace(AppConstants.Configuration.DbPasswordPlaceholder, dbPassword);
        return connectionString;
    }
}
