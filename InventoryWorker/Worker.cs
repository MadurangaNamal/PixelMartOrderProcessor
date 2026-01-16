using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configuration;
using Shared.Helpers;
using Shared.Models;
using Shared.Orders;
using Shared.Repositories;
using System.Text;
using System.Text.Json;

namespace InventoryWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqConnectionManager _rabbitMq;
    private readonly IConfiguration _configuration;
    private readonly IMessagePublisher _messagePublisher;

    public Worker(ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        RabbitMqConnectionManager rabbitMq,
        IConfiguration configuration,
        IMessagePublisher messagePublisher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _rabbitMq = rabbitMq ?? throw new ArgumentNullException(nameof(rabbitMq));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration["RabbitMq:InventoryQueue"] ?? "inventory-queue";

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
                await _rabbitMq.Channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            _logger.LogInformation("Updating inventory for Order {OrderId}", orderMessage.OrderId);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IPixelMartOrderProcessorRepository>();

                await orderRepository.UpdateInventoryStatusAsync(orderMessage.OrderId, ProcessingStatus.InProgress);

                await Task.Delay(2000, stoppingToken); // Simulate inventory update process

                orderMessage.Items.ForEach(item =>
                _logger.LogInformation("Updated inventory for Product {ProductId}: -{Quantity}", item.ProductId, item.Quantity));

                // Publish to email queue
                var emailQueue = _configuration["RabbitMq:EmailQueue"] ?? "email-queue";
                await _messagePublisher.PublishAsync(emailQueue, orderMessage);

                await _rabbitMq.Channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory for Order {OrderId}", orderMessage.OrderId);
                await _rabbitMq.Channel!.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        await _rabbitMq.Channel!.BasicConsumeAsync(queueName, false, consumer, stoppingToken);

        _logger.LogInformation("InventoryWorker started consuming from {QueueName}", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
