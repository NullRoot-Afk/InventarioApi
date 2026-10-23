using InventarioApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventarioApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Nombre)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(p => p.CodigoSKU)
                  .IsRequired()
                  .HasMaxLength(50);

            // SKU único
            entity.HasIndex(p => p.CodigoSKU).IsUnique();

            entity.Property(p => p.Precio).HasPrecision(18, 2);
        });
    }
}
