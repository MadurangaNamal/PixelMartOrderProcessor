using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Shared.Constants;

namespace Shared.Configuration;

public class RabbitMqConnectionManager : IAsyncDisposable
{
    private readonly ILogger<RabbitMqConnectionManager> _logger;
    public IConnection? Connection { get; private set; }
    public IChannel? Channel { get; private set; }

    public RabbitMqConnectionManager(IConfiguration configuration, ILogger<RabbitMqConnectionManager> logger)
    {
        _logger = logger;
        InitializeAsync(configuration).GetAwaiter().GetResult();
    }

    private async Task InitializeAsync(IConfiguration configuration)
    {
        var factory = new ConnectionFactory
        {
            HostName = configuration[AppConstants.RabbitMq.Host] ?? AppConstants.RabbitMq.DefaultHost,
            Port = int.Parse(configuration[AppConstants.RabbitMq.Port] ?? AppConstants.RabbitMq.DefaultPort),
            UserName = configuration[AppConstants.RabbitMq.Username] ?? AppConstants.RabbitMq.DefaultUsername,
            Password = configuration[AppConstants.RabbitMq.Password] ?? AppConstants.RabbitMq.DefaultPassword,
        };

        try
        {
            Connection = await factory.CreateConnectionAsync();
            Channel = await Connection.CreateChannelAsync();

            await Channel.BasicQosAsync(0, 1, false);

            _logger.LogInformation("RabbitMQ connection and channel established.");
        }
        catch (BrokerUnreachableException ex)
        {
            _logger.LogCritical(ex, "Failed to connect to RabbitMQ. Check host/port/credentials.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error connecting to RabbitMQ.");
            throw;
        }
    }

    public async Task DeclareQueueAsync(string queueName)
    {
        await Channel!.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (Channel != null) await Channel.CloseAsync();
            if (Connection != null) await Connection.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMQ connection/channel.");
        }

        Channel?.Dispose();
        Connection?.Dispose();
    }
}
