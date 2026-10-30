using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bakery.DAL.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("OrderStatusHistories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Comment).HasMaxLength(250);
        builder.HasIndex(x => new { x.OrderId, x.ChangedAt });
    }
}
