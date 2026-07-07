using Microsoft.EntityFrameworkCore;

public class VapeShopInventoryDbContext : DbContext
{
    public DbSet<Product> Products {get; private set;}
    public DbSet<Expense> Expenses {get; private set;}
    public DbSet<Sale> Sales {get; private set;}
    public DbSet<SaleItem> SaleItems {get; private set;}
    public VapeShopInventoryDbContext (DbContextOptions<VapeShopInventoryDbContext> options) : base (options){}
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.Sku)
            .IsUnique();

        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.Sale)
            .WithMany(s => s.SaleItems)
            .HasForeignKey(si => si.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SaleItem>()
            .HasOne(si => si.Product)
            .WithMany()
            .HasForeignKey(si => si.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Sale>()
            .Navigation(s => s.SaleItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}