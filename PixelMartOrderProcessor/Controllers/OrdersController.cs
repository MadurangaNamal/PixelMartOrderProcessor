using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Helpers;
using Shared.Models;
using Shared.Orders;
using Shared.Repositories;

namespace PixelMartOrderProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IPixelMartOrderProcessorRepository _repository;
    private readonly ILogger<OrdersController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMessagePublisher _messagePublisher;

    public OrdersController(IPixelMartOrderProcessorRepository repository,
        ILogger<OrdersController> logger,
        IConfiguration configuration,
        IMessagePublisher messagePublisher)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrderStatus([FromRoute] Guid orderId)
    {
        var order = await _repository.GetOrderByIdAsync(orderId);

        if (order == null)
            return NotFound($"Order with ID:{orderId} not found");

        var orderResponse = new
        {
            orderId = order.OrderId,
            customerEmail = order.CustomerEmail,
            totalAmount = order.TotalAmount,
            orderDate = order.OrderDate,
            status = order.Status.ToString(),
            paymentStatus = order.PaymentStatus.ToString(),
            inventoryStatus = order.InventoryStatus.ToString(),
            emailStatus = order.EmailStatus.ToString(),
            items = order.Items.Select(i => new
            {
                productId = i.ProductId,
                productName = i.ProductName,
                quantity = i.Quantity,
                price = i.Price
            })
        };

        return Ok(orderResponse);
    }

    [HttpGet("{orderId}/history")]
    public async Task<IActionResult> GetOrderHistory([FromRoute] Guid orderId)
    {
        var orderHistory = await _repository.GetOrderHistoryAsync(orderId);

        var orderHistoryResponse = orderHistory.Select(oh => new
        {
            status = oh.Status.ToString(),
            message = oh.Message,
            timestamp = oh.CreatedAt
        });

        return Ok(orderHistoryResponse);
    }

    [HttpGet("customer/{email}")]
    public async Task<IActionResult> GetOrdersByCustomer(string email)
    {
        var orders = await _repository.GetOrdersByCustomerEmailAsync(email);

        var ordersResponse = orders.Select(o => new
        {
            orderId = o.OrderId,
            totalAmount = o.TotalAmount,
            orderDate = o.OrderDate,
            status = o.Status.ToString(),
            itemCount = o.Items.Count
        });

        return Ok(ordersResponse);
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] CreateOrderRequest orderRequest,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return BadRequest(new { message = "Idempotency-Key header is required" });

        var existingOrder = await _repository.GetOrderByIdempotencyKeyAsync(idempotencyKey);

        if (existingOrder != null)
        {
            _logger.LogInformation(
                "Duplicate order request detected with idempotency key: {IdempotencyKey}. Returning existing order: {OrderId}",
                idempotencyKey, existingOrder.OrderId);

            return Ok(new
            {
                orderId = existingOrder.OrderId,
                status = existingOrder.Status.ToString(),
                message = "Order already exists (idempotent response)",
                duplicate = true
            });
        }

        try
        {
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                IdempotencyKey = idempotencyKey,
                CustomerEmail = orderRequest.CustomerEmail,
                TotalAmount = orderRequest.TotalAmount,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                PaymentStatus = ProcessingStatus.Pending,
                InventoryStatus = ProcessingStatus.Pending,
                EmailStatus = ProcessingStatus.Pending,
                Items = orderRequest.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            var newOrder = await _repository.CreateOrderAsync(order);

            var message = new OrderPlacedMessage
            {
                MessageId = Guid.NewGuid(),
                OrderId = newOrder.OrderId,
                CustomerEmail = newOrder.CustomerEmail,
                TotalAmount = newOrder.TotalAmount,
                Timestamp = DateTime.UtcNow,
                Items = newOrder.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList(),
            };

            var queueName = _configuration["RabbitMq:OrderPlacedQueue"] ?? "order-placed-queue";
            await _messagePublisher.PublishAsync(queueName, message);

            _logger.LogInformation("Order {OrderId} placed successfully with idempotency key {IdempotencyKey}", newOrder.OrderId, idempotencyKey);

            return Ok(new
            {
                orderId = newOrder.OrderId,
                status = newOrder.Status.ToString(),
                message = "Order placed successfully. Processing...",
                duplicate = false
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("idempotency_key") == true)
        {
            _logger.LogWarning(ex, "Concurrent duplicate order request detected for idempotency key: {IdempotencyKey}", idempotencyKey);

            var previousOrder = await _repository.GetOrderByIdempotencyKeyAsync(idempotencyKey!);

            if (previousOrder != null)
            {
                return Ok(new
                {
                    orderId = previousOrder.OrderId,
                    status = previousOrder.Status.ToString(),
                    message = "Order already exists (idempotent response)",
                    duplicate = true
                });
            }

            return StatusCode(500, new { message = "Error processing order" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error placing order");

            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while placing the order" });
        }
    }
}
