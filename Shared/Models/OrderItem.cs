using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

[Table("order_items")]
public class OrderItem
{
    [Key]
    [Column("order_item_id")]
    public Guid OrderItemId { get; set; }

    [Required]
    [Column("product_id")]
    [MaxLength(100)]
    public string ProductId { get; set; } = string.Empty;

    [Required]
    [Column("product_name")]
    [MaxLength(255)]
    public string ProductName { get; set; } = string.Empty;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("price")]
    [Precision(18, 2)]
    public decimal Price { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    // Navigation property
    [ForeignKey("OrderId")]
    public Order Order { get; set; } = null!;
}
