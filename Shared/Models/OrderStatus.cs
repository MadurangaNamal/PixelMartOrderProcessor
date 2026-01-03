namespace Shared.Models;

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    PaymentCompleted = 2,
    InventoryUpdated = 3,
    EmailSent = 4,
    Completed = 5,
    Failed = 6
}
