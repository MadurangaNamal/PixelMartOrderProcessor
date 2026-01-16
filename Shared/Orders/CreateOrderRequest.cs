namespace Shared.Orders;

public class CreateOrderRequest
{
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = [];
}
