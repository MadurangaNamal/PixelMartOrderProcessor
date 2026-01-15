using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Configuration;
using System.Text;
using System.Text.Json;

namespace Shared.Helpers;

public sealed class RabbitMqMessagePublisher : IMessagePublisher
{
    private readonly RabbitMqConnectionManager _rabbitMq;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;

    public RabbitMqMessagePublisher(RabbitMqConnectionManager rabbitMq, ILogger<RabbitMqMessagePublisher> logger)
    {
        _rabbitMq = rabbitMq;
        _logger = logger;
    }

    public async Task PublishAsync<TMessage>(string queueName, TMessage message)
    {
        if (_rabbitMq.Channel is null || !_rabbitMq.Channel.IsOpen)
            throw new InvalidOperationException("RabbitMQ channel is not available.");

        try
        {
            await _rabbitMq.DeclareQueueAsync(queueName);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties { Persistent = true };

            await _rabbitMq.Channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: properties, body: body);

            _logger.LogInformation("Published message to {QueueName} | MessageType: {MessageType}", queueName, typeof(TMessage).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to {QueueName} | MessageType: {MessageType}", queueName, typeof(TMessage).Name);
            throw;
        }
    }
}
