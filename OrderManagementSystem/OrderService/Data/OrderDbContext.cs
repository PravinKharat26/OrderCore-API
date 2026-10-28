using Microsoft.EntityFrameworkCore;
using OrderService.Entities;

namespace OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.CustomerId);
            entity.HasIndex(o => o.Status);

            entity.Property(o => o.Status).HasMaxLength(30).IsRequired();
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2).IsRequired();
            entity.Property(o => o.CreatedAtUtc).IsRequired();

            entity.HasMany(o => o.OrderItems)
                  .WithOne(i => i.Order)
                  .HasForeignKey(i => i.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ProductNameSnapshot).HasMaxLength(150).IsRequired();
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2).IsRequired();
            entity.Property(i => i.Quantity).IsRequired();
            entity.Property(i => i.LineTotal).HasPrecision(18, 2).IsRequired();
        });
    }
}
