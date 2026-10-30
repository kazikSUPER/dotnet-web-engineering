using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bakery.DAL.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", t => t.HasCheckConstraint("CK_Payments_Amount", "[Amount] > 0"));
        builder.HasKey(x => x.OrderId);
        builder.Property(x => x.TransactionNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PaymentMethod).IsRequired().HasMaxLength(30);
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Status).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.TransactionNumber).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();
    }
}
