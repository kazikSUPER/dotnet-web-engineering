using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bakery.DAL.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", t =>
        {
            t.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_OrderItems_UnitPrice", "[UnitPrice] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
        builder.Ignore(x => x.TotalPrice);
    }
}
