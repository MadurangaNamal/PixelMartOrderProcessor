using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

[Table("order_status_history")]
public class OrderStatusHistory
{
    [Key]
    [Column("history_id")]
    public Guid HistoryId { get; set; }

    [Column("status")]
    public OrderStatus Status { get; set; }

    [Column("message")]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;
}
