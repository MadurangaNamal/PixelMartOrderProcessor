using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

[Table("orders")]
public class Order
{
    [Key]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Column("idempotency_key")]
    [MaxLength(100)]
    public string? IdempotencyKey { get; set; }

    [Required]
    [Column("customer_email")]
    [MaxLength(255)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Column("total_amount")]
    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("status")]
    public OrderStatus Status { get; set; }

    [Column("payment_status")]
    public ProcessingStatus PaymentStatus { get; set; }

    [Column("inventory_status")]
    public ProcessingStatus InventoryStatus { get; set; }

    [Column("email_status")]
    public ProcessingStatus EmailStatus { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
