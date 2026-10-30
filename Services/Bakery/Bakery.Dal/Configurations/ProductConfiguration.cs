using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bakery.DAL.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_Price", "[Price] > 0");
            t.HasCheckConstraint("CK_Products_Stock", "[Stock] >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.Name);
        builder.Ignore(x => x.IsAvailable);
        builder.HasMany(x => x.OrderItems).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Detail).WithOne(x => x.Product).HasForeignKey<ProductDetail>(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Images).WithOne(x => x.Product).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
