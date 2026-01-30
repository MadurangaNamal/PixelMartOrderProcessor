using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

[Table("processed_messages")]
public class ProcessedMessage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("message_id")]
    [MaxLength(100)]
    public string MessageId { get; set; } = string.Empty;

    [Required]
    [Column("order_id")]
    public Guid OrderId { get; set; }

    [Required]
    [Column("worker_type")]
    [MaxLength(50)]
    public string WorkerType { get; set; } = string.Empty;

    [Column("processed_at")]
    public DateTime ProcessedAt { get; set; }
}
