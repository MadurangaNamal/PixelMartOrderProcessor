using Shared.Models;

namespace Shared.Repositories;

public interface IPixelMartOrderProcessorRepository
{
    Task<Order> CreateOrderAsync(Order order);
    Task<Order?> GetOrderByIdAsync(Guid orderId);
    Task<List<Order>> GetOrdersByCustomerEmailAsync(string email);
    Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string message);
    Task<bool> UpdatePaymentStatusAsync(Guid orderId, ProcessingStatus status);
    Task<bool> UpdateInventoryStatusAsync(Guid orderId, ProcessingStatus status);
    Task<bool> UpdateEmailStatusAsync(Guid orderId, ProcessingStatus status);
    Task<List<OrderStatusHistory>> GetOrderHistoryAsync(Guid orderId);
}
