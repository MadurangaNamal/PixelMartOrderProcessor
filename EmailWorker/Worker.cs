using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Configuration;
using Shared.Constants;
using Shared.Data;
using Shared.HealthChecks;
using Shared.Models;
using Shared.Orders;
using Shared.Repositories;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace EmailWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMqConnectionManager _rabbitMq;
    private readonly IConfiguration _configuration;
    private readonly WorkerHealthCheck _healthCheck;
    private static readonly ActivitySource ActivitySource = OpenTelemetryConfiguration.ActivitySource;

    public Worker(ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        RabbitMqConnectionManager rabbitMq,
        IConfiguration configuration,
        WorkerHealthCheck healthCheck)
    {
        _logger = logger;
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _rabbitMq = rabbitMq ?? throw new ArgumentNullException(nameof(rabbitMq));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _healthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Consume email queue
        var queueName = _configuration[AppConstants.RabbitMq.EmailQueue] ?? AppConstants.RabbitMq.DefaultEmailQueue;
        await _rabbitMq.DeclareQueueAsync(queueName);
        var consumer = new AsyncEventingBasicConsumer(_rabbitMq.Channel!);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            // Master stamp — connects to InventoryWorker trace via headers
            using var activity = RabbitMqInstrumentation.StartConsumeActivity(queueName, ea.BasicProperties?.Headers!);

            var body = ea.Body.ToArray(); // Get the message body as a byte array
            var message = Encoding.UTF8.GetString(body);
            var orderMessage = JsonSerializer.Deserialize<OrderPlacedMessage>(message);
            var messageId = orderMessage!.MessageId;

            if (orderMessage == null)
            {
                _logger.LogWarning("Received null order message");

                activity?.SetStatus(ActivityStatusCode.Error, "Received null order message");
                await _rabbitMq.Channel!.BasicNackAsync(ea.DeliveryTag, false, false);
                _healthCheck.RecordError();

                return;
            }

            activity?.SetTag("order.id", orderMessage.OrderId);
            activity?.SetTag("message.id", messageId);
            activity?.SetTag("customer.email", orderMessage.CustomerEmail);

            _logger.LogInformation("Sending confirmation email for Order {OrderId}, MessageId: {MessageId}, TraceId: {TraceId}",
                orderMessage.OrderId, messageId, activity?.TraceId);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PixelMartOrderProcessorDbContext>();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IPixelMartOrderProcessorRepository>();
                bool orderAlreadyProcessed;

                // span for deduplication check
                using (var dedupeActivity = ActivitySource.StartActivity("email.deduplication_check", ActivityKind.Internal))
                {
                    dedupeActivity?.SetTag("message.id", messageId);

                    orderAlreadyProcessed = await dbContext.ProcessedMessages
                        .AnyAsync(pm => pm.MessageId == messageId && pm.WorkerType == WorkerType.EmailWorker.ToString());

                    dedupeActivity?.SetTag("message.is_duplicate", orderAlreadyProcessed);
                }

                if (orderAlreadyProcessed)
                {
                    _logger.LogInformation("Message {MessageId} for Order {OrderId} already processed. Acknowledging duplicate.",
                        messageId, orderMessage.OrderId);

                    activity?.SetTag("message.skipped_as_duplicate", true);
                    await _rabbitMq.Channel!.BasicAckAsync(ea.DeliveryTag, false);
                    _healthCheck.RecordProcessing();

                    return;
                }

                // Span: Update status to InProgress
                using (var statusActivity = ActivitySource.StartActivity("email.update_status_in_progress", ActivityKind.Internal))
                {
                    statusActivity?.SetTag("order.id", orderMessage.OrderId);
                    statusActivity?.SetTag("email.status", ProcessingStatus.InProgress.ToString());

                    await orderRepository.UpdateEmailStatusAsync(orderMessage.OrderId, ProcessingStatus.InProgress);
                }

                await Task.Delay(2000, stoppingToken); // Simulate email sending

                // Span: Send email & record completion
                using (var emailActivity = ActivitySource.StartActivity("email.record_completion", ActivityKind.Internal))
                {
                    emailActivity?.SetTag("order.id", orderMessage.OrderId);
                    emailActivity?.SetTag("customer.email", orderMessage.CustomerEmail);

                    _logger.LogInformation("Email sent to {Email} for Order {OrderId}",
                        orderMessage.CustomerEmail, orderMessage.OrderId);

                    emailActivity?.AddEvent(new ActivityEvent("email.sent", tags: new ActivityTagsCollection
                        {
                            { "customer.email", orderMessage.CustomerEmail },
                            { "order.id", orderMessage.OrderId.ToString() }
                        }));

                    await orderRepository.UpdateEmailStatusAsync(orderMessage.OrderId, ProcessingStatus.Completed);

                    dbContext.ProcessedMessages.Add(new ProcessedMessage
                    {
                        Id = Guid.NewGuid(),
                        MessageId = messageId,
                        OrderId = orderMessage.OrderId,
                        WorkerType = WorkerType.EmailWorker.ToString(),
                        ProcessedAt = DateTime.UtcNow
                    });

                    await dbContext.SaveChangesAsync();
                }

                await _rabbitMq.Channel!.BasicAckAsync(ea.DeliveryTag, false);
                _healthCheck.RecordProcessing();
                activity?.SetStatus(ActivityStatusCode.Ok);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Concurrent duplicate processing detected for MessageId: {MessageId}", messageId);

                activity?.SetTag("exception.type", "DbUpdateException");
                activity?.SetTag("message.concurrent_duplicate", true);
                activity?.AddException(ex);

                await _rabbitMq.Channel!.BasicAckAsync(ea.DeliveryTag, false);
                _healthCheck.RecordProcessing();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email for Order {OrderId}", orderMessage.OrderId);

                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
                await _rabbitMq.Channel!.BasicNackAsync(ea.DeliveryTag, false, true);
                _healthCheck.RecordError();
            }
        };

        await _rabbitMq.Channel!.BasicConsumeAsync(queueName, false, consumer, stoppingToken);

        _logger.LogInformation("EmailWorker started consuming from {QueueName}", queueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);  // Keep the worker running
    }
}
