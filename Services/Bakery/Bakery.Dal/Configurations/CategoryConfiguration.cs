using Bakery.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bakery.DAL.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(80);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasData(
            new { Id = 1, Name = "Хліб" },
            new { Id = 2, Name = "Випічка" },
            new { Id = 3, Name = "Десерти" });
    }
}
