using Microsoft.Extensions.Configuration;

namespace Shared.Configuration;

public class DatabaseConfiguration
{
    protected DatabaseConfiguration()
    {
    }

    public static string GetConnectionString(IConfiguration configuration)
    {
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var dbPassword = configuration["DB_PASSWORD"]
            ?? throw new InvalidOperationException("Database password 'DB_PASSWORD' not found in configuration.");

        var connectionString = rawConnectionString.Replace("{DB_PASSWORD}", dbPassword);

        return connectionString;
    }
}
