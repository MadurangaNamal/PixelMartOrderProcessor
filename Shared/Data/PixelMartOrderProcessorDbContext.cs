using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace Shared.Data;

public class PixelMartOrderProcessorDbContext : DbContext
{
    public PixelMartOrderProcessorDbContext(DbContextOptions<PixelMartOrderProcessorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
    public DbSet<ProcessedMessage> ProcessedMessages { get; set; }
    public DbSet<WorkerHealthStatus> WorkerHealthStatuses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId);

            entity.HasIndex(e => e.IdempotencyKey)
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");

            entity.HasIndex(e => e.CustomerEmail);
            entity.HasIndex(e => e.OrderDate);
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.Property(e => e.PaymentStatus)
                .HasConversion<int>();

            entity.Property(e => e.InventoryStatus)
                .HasConversion<int>();

            entity.Property(e => e.EmailStatus)
                .HasConversion<int>();
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.OrderItemId);
            entity.HasIndex(e => e.OrderId);

            entity.Property(e => e.Price)
            .HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId);
            entity.HasIndex(e => e.OrderId);

            entity.Property(e => e.Status)
            .HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
            .WithMany()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedMessage>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.MessageId, e.WorkerType })
            .IsUnique();
        });

        modelBuilder.Entity<WorkerHealthStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WorkerName).IsUnique();
            entity.HasIndex(e => e.LastCheckTime);
        });

        base.OnModelCreating(modelBuilder);
    }
}
