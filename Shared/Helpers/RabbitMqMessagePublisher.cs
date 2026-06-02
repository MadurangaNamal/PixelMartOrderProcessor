using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Shared.Configuration;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Shared.Helpers;

public sealed class RabbitMqMessagePublisher : IMessagePublisher
{
    private readonly RabbitMqConnectionManager _rabbitMq;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;

    public RabbitMqMessagePublisher(RabbitMqConnectionManager rabbitMq, ILogger<RabbitMqMessagePublisher> logger)
    {
        _rabbitMq = rabbitMq ?? throw new ArgumentNullException(nameof(rabbitMq));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TMessage>(string queueName, TMessage message)
    {
        if (_rabbitMq.Channel is null || !_rabbitMq.Channel.IsOpen)
            throw new InvalidOperationException("RabbitMQ channel is not available.");

        using var activity = RabbitMqInstrumentation.StartPublishActivity(queueName, message!);

        try
        {
            await _rabbitMq.DeclareQueueAsync(queueName);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = new BasicProperties
            {
                Persistent = true,
                Headers = new Dictionary<string, object?>()
            };

            // Inject trace context into message headers
            RabbitMqInstrumentation.InjectTraceContext(properties.Headers!);

            await _rabbitMq.Channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false,
                basicProperties: properties, body: body);

            activity?.SetTag("message.size", body.Length);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Published message to queue {QueueName} with TraceId {TraceId}",
                queueName,
                activity?.TraceId);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddException(ex);

            _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);

            throw;
        }
    }
}
