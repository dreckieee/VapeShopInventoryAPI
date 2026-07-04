using Microsoft.EntityFrameworkCore;

public class VapeShopInventoryDbContext : DbContext
{
    public DbSet<Product> Products {get; private set;}
    public VapeShopInventoryDbContext (DbContextOptions<VapeShopInventoryDbContext> options) : base (options){}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();
    }
}