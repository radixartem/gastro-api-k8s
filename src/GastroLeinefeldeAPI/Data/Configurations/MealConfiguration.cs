using GastroLeinefeldeAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GastroLeinefeldeAPI.Data.Configurations;

public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Category)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(e => e.Price)
            .HasPrecision(10, 2);
        
        builder.Property(e => e.Status)
            .HasMaxLength(50);
        
        builder.Property(e => e.PreparationTime)
            .HasMaxLength(50);
        
        builder.Property(e => e.Hash)
            .HasMaxLength(64);

        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Name);
        builder.HasIndex(e => e.Date);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.ImportedAt);
        builder.HasIndex(e => new { e.Category, e.IsActive });
    }
}