using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bakery.DAL.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", t =>
        {
            t.HasCheckConstraint("CK_Orders_Status", "[Status] IN ('Pending','Paid','Baking','Ready','Delivered','Cancelled')");
            t.HasCheckConstraint("CK_Orders_TotalAmount", "[TotalAmount] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderNumber).IsRequired().HasMaxLength(36);
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Notes).HasMaxLength(250);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasMany(x => x.Items).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Payment).WithOne(x => x.Order).HasForeignKey<Payment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.StatusHistory).WithOne(x => x.Order).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
