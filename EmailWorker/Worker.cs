using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configuration;
using Shared.Models;
using Shared.Orders;
using Shared.Repositories;
using System.Text;
using System.Text.Json;

namespace EmailWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly RabbitMqConnectionManager _rabbitMq;
        private readonly IConfiguration _configuration;

        public Worker(ILogger<Worker> logger,
            IServiceProvider serviceProvider,
            RabbitMqConnectionManager rabbitMq,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _rabbitMq = rabbitMq ?? throw new ArgumentNullException(nameof(rabbitMq));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queueName = _configuration["RabbitMq:EmailQueue"] ?? "email-queue";

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

                _logger.LogInformation("Sending confirmation email for Order {OrderId}", orderMessage.OrderId);

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var orderRepository = scope.ServiceProvider.GetRequiredService<IPixelMartOrderProcessorRepository>();

                    await orderRepository.UpdateEmailStatusAsync(orderMessage.OrderId, ProcessingStatus.InProgress);

                    await Task.Delay(2000, stoppingToken); // Simulate email sending

                    _logger.LogInformation("Email sent to {Email} for Order {OrderId}", orderMessage.CustomerEmail, orderMessage.OrderId);

                    await orderRepository.UpdateEmailStatusAsync(orderMessage.OrderId, ProcessingStatus.Completed);

                    await _rabbitMq.Channel!.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email for Order {OrderId}", orderMessage.OrderId);
                    await _rabbitMq.Channel!.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _rabbitMq.Channel!.BasicConsumeAsync(queueName, false, consumer, stoppingToken);

            _logger.LogInformation("EmailWorker started consuming from {QueueName}", queueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);  // Keep the worker running
        }
    }
}
