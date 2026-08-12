using GastroLeinefeldeAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GastroLeinefeldeAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Meal> Meals { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Meal>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(500);
            
            entity.Property(e => e.Price)
                .HasPrecision(10, 2);
            
            entity.Property(e => e.Status)
                .HasMaxLength(50);
            
            entity.Property(e => e.PreparationTime)
                .HasMaxLength(50);
            
            entity.Property(e => e.Hash)
                .HasMaxLength(64);

            // Indizes für bessere Performance
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ImportedAt);
            entity.HasIndex(e => new { e.Category, e.IsActive });
        });
    }
}