namespace Shared.Messages;

public class OrderPlacedMessage
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime Timestamp { get; set; }
}
