using Shared.Models;

namespace Shared.Repositories;

public class PixelMartOrderProcessorRepository : IPixelMartOrderProcessorRepository
{
    public Task<Order> CreateOrderAsync(Order order)
    {
        throw new NotImplementedException();
    }

    public Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        throw new NotImplementedException();
    }

    public Task<List<OrderStatusHistory>> GetOrderHistoryAsync(Guid orderId)
    {
        throw new NotImplementedException();
    }

    public Task<List<Order>> GetOrdersByCustomerEmailAsync(string email)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateEmailStatusAsync(Guid orderId, ProcessingStatus status)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateInventoryStatusAsync(Guid orderId, ProcessingStatus status)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string message)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdatePaymentStatusAsync(Guid orderId, ProcessingStatus status)
    {
        throw new NotImplementedException();
    }
}
