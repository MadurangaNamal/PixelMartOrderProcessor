using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configuration;
using Shared.Messages;
using Shared.Models;
using Shared.Repositories;
using System.Text;
using System.Text.Json;

namespace PaymentWorker;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqConnectionManager _rabbitMq;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, RabbitMqConnectionManager rabbitMq, IConfiguration configuration)
        : this(logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _rabbitMq = rabbitMq ?? throw new ArgumentNullException(nameof(rabbitMq));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration["RabbitMq:OrderPlacedQueue"] ?? "order-placed-queue";

        await _rabbitMq.DeclareQueueAsync(queueName);
        var consumer = new AsyncEventingBasicConsumer(_rabbitMq.Channel!);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var orderMessage = JsonSerializer.Deserialize<OrderPlacedMessage>(message);

            if (orderMessage == null)
            {
                _logger.LogWarning("Received null order message");

                await _rabbitMq.Channel!.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            _logger.LogInformation("Processing payment for Order {OrderId}", orderMessage.OrderId);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IPixelMartOrderProcessorRepository>();

                await orderRepository.UpdatePaymentStatusAsync(orderMessage.OrderId, ProcessingStatus.InProgress);

                await Task.Delay(3000, stoppingToken); // Simulate payment processing

                // Simulate payment logic (90% success rate)
                var random = new Random();
                var paymentSuccess = random.Next(100) < 90;

                if (paymentSuccess)
                {
                    _logger.LogInformation("Payment successful for Order {OrderId}", orderMessage.OrderId);

                    await orderRepository.UpdatePaymentStatusAsync(orderMessage.OrderId, ProcessingStatus.Completed);

                    // Publish to inventory queue
                    var inventoryQueue = _configuration["RabbitMq:InventoryQueue"] ?? "inventory-queue";
                    await PublishToQueueAsync(inventoryQueue, orderMessage);
                }
                else
                {
                    _logger.LogError("Payment failed for Order {OrderId}", orderMessage.OrderId);

                    await orderRepository.UpdatePaymentStatusAsync(orderMessage.OrderId, ProcessingStatus.Failed);
                }

                await _rabbitMq.Channel!.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment for Order {OrderId}", orderMessage.OrderId);

                await _rabbitMq.Channel!.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await _rabbitMq.Channel!.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        _logger.LogInformation("PaymentWorker started consuming from queue: {QueueName}", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);  // Keep the worker running
    }

    private async Task PublishToQueueAsync(string queueName, OrderPlacedMessage message)
    {
        await _rabbitMq.DeclareQueueAsync(queueName);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties
        {
            Persistent = true
        };

        await _rabbitMq.Channel!.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: properties, body: body);

        _logger.LogInformation("Published message to queue {QueueName} for Order {OrderId}", queueName, message.OrderId);
    }
}
