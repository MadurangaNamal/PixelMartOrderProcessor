using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Shared.Models;

namespace Shared.Repositories;

public class PixelMartOrderProcessorRepository : IPixelMartOrderProcessorRepository
{
    private readonly PixelMartOrderProcessorDbContext _dbContext;

    public PixelMartOrderProcessorRepository(PixelMartOrderProcessorDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Order> CreateOrderAsync(Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        order.Items.ForEach(item =>
        {
            item.OrderItemId = Guid.NewGuid();
        });

        await _dbContext.Orders.AddAsync(order);
        await AddStatusHistoryAsync(order.OrderId, OrderStatus.Pending, "Order created");
        await _dbContext.SaveChangesAsync();

        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(Guid orderId)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<List<OrderStatusHistory>> GetOrderHistoryAsync(Guid orderId)
    {
        return await _dbContext.OrderStatusHistories
            .AsNoTracking()
            .Where(oh => oh.OrderId == orderId)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Order>> GetOrdersByCustomerEmailAsync(string email)
    {
        return await _dbContext.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.CustomerEmail.Equals(email))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateEmailStatusAsync(Guid orderId, ProcessingStatus status)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);

        if (order == null)
            return false;

        order.EmailStatus = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == ProcessingStatus.Completed)
        {
            order.Status = OrderStatus.Completed;
            await AddStatusHistoryAsync(orderId, OrderStatus.Completed, "Order completed - Email sent successfully");
        }

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateInventoryStatusAsync(Guid orderId, ProcessingStatus status)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);

        if (order == null)
            return false;

        order.InventoryStatus = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == ProcessingStatus.Completed)
        {
            order.Status = OrderStatus.InventoryUpdated;
            await AddStatusHistoryAsync(orderId, OrderStatus.InventoryUpdated, "Inventory updated successfully");
        }

        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string message)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);

        if (order == null)
            return false;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        await AddStatusHistoryAsync(orderId, status, message);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdatePaymentStatusAsync(Guid orderId, ProcessingStatus status)
    {
        var order = await _dbContext.Orders.FindAsync(orderId);

        if (order == null)
            return false;

        order.PaymentStatus = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == ProcessingStatus.Completed)
        {
            order.Status = OrderStatus.PaymentCompleted;
            await AddStatusHistoryAsync(orderId, OrderStatus.PaymentCompleted, "Payment processed successfully");
        }
        else if (status == ProcessingStatus.Failed)
        {
            order.Status = OrderStatus.Failed;
            await AddStatusHistoryAsync(orderId, OrderStatus.Failed, "Payment processing failed");
        }

        await _dbContext.SaveChangesAsync();

        return true;
    }

    private async Task AddStatusHistoryAsync(Guid orderId, OrderStatus status, string message)
    {
        var history = new OrderStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            OrderId = orderId,
            Status = status,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.OrderStatusHistories.AddAsync(history);
    }
}
