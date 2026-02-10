using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

[Table("worker_health_status")]
public class WorkerHealthStatus
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("worker_name")]
    [MaxLength(100)]
    public string WorkerName { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    [Column("last_check_time")]
    public DateTime LastCheckTime { get; set; }

    [Column("total_processed")]
    public int TotalProcessed { get; set; }

    [Column("total_errors")]
    public int TotalErrors { get; set; }

    [Column("error_rate")]
    public double ErrorRate { get; set; }

    [Column("details")]
    public string? Details { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
